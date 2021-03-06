﻿// RaspberryPi.GpioWeb
//
// C# / Mono programming for the Raspberry Pi
// Copyright (c) 2017 Paul Carver
//
// RaspberryPi.GpioWeb is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using GpioWeb.GpioCore;
using Raspberry.IO.GeneralPurpose;
using System;
using System.Threading;

namespace GpioWeb.PluginRgbSimple
{
	public class HandlerRgbSimpleAction : IActionHandler
	{
		private object _state = null;

		public void Action(ActionBase baseAction, CancellationToken cancelToken, dynamic config)
		{
			RgbSimpleAction action = (RgbSimpleAction)baseAction;

			// note that config is dynamic so we cast the pin values to integer
				_state = new { state = "setup" };
			var connection = new MemoryGpioConnectionDriver();
			var pins = new GpioOutputBinaryPin[]
			{
				connection.Out((ProcessorPin)config.pins[0]),
				connection.Out((ProcessorPin)config.pins[1]),
				connection.Out((ProcessorPin)config.pins[2])
			};

			_state = new { state = "preDelay" };
			if (cancelToken.WaitHandle.WaitOne(action.PreDelayMs))
			{
				return;
			}

			for (int loopCounter = 0; loopCounter < action.LoopCount; ++loopCounter)
			{
				for (int i = 0; i < pins.Length; ++i)
				{
					_state = new { state = $"startValue_{loopCounter}" };
					pins[i].Write(action.StartValues[i]);
				}

				// wait until possible cancel, but continue if cancelled to at least set end value
				_state = new { state = $"startDuration_{loopCounter}" };
				cancelToken.WaitHandle.WaitOne(action.StartDurationMs);

				for (int i = 0; i < pins.Length; ++i)
				{
					// only output if it changes
					if (action.EndValues[i] != action.StartValues[i])
					{
						_state = new { state = $"endValue_{loopCounter}" };
						pins[i].Write(action.EndValues[i]);
					}
				}

				_state = new { state = $"endDuration_{loopCounter}" };
				if (cancelToken.WaitHandle.WaitOne(action.EndDurationMs))
				{
					return;
				}
			}

			_state = new { state = "postDelay" };
			if (cancelToken.WaitHandle.WaitOne(action.PostDelayMs))
			{
				return;
			}
		}

		public object CurrentState
		{
			get
			{
				return _state;
			}
		}

		public Type[] SupportedActions { get; } = new Type[]
		{
			typeof(RgbSimpleAction),
		};
	}
}
