/*!
	Copyright (C) 2003-2015 Kody Brown (kody@bricksoft.com).
	
	MIT License:
	
	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to
	deal in the Software without restriction, including without limitation the
	rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
	sell copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:
	
	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.
	
	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
	FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
	DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Threading;

namespace Bricksoft.PowerCode
{
	public partial class ConsoleEx
	{
		static int wrapWidth = Console.WindowWidth;
		static string errorSpacer = new string('=', wrapWidth);

		public static ConsoleKey Error( string message, string description = "", Exception ex = null, bool promptToContinue = true, bool showDivider = true, int indentation = 6 )
		{
			//string ind = new string(' ', indentation);

			if (showDivider) {
				Console.WriteLine(errorSpacer);
			}

			Console.WriteLine(message);

			if (description != null && description.Length > 0) {
				Console.WriteLine(Text.Wrap(string.Format("\nError description:\n{0}", description), Console.WindowWidth, indentation));
			}

			if (ex != null) {
				Console.WriteLine(Text.Wrap(string.Format("\nException details:\n{0}\nStack trace:\n{1}", ex.Message, ex.StackTrace), Console.WindowWidth, indentation));
			}

			if (showDivider && !promptToContinue) {
				Console.WriteLine(errorSpacer);
			}

			if (promptToContinue) {
				return ConsoleEx.PressAnyKey(centered: true, padChar: '=', animated: true).Key;
			} else {
				return ConsoleKey.Escape;
			}
		}

		public static ConsoleKeyInfo PressAnyKey( string prompt = " Press any key to exit ", bool interceptKey = true, bool clearAfter = true, bool centered = true, char padChar = '-', bool animated = false, ConsoleColor animatedColor = ConsoleColor.White, ConsoleColor promptColor = ConsoleColor.Gray, ConsoleColor padColor = ConsoleColor.DarkGray )
		{
			int top, top2, winWidth, left;
			bool curVis;
			string empty;
			ConsoleKeyInfo k;
			Thread th = null;

			top = Console.CursorTop;
			left = Console.CursorLeft;
			curVis = Console.CursorVisible;
			winWidth = Console.WindowWidth;
			empty = new string(' ', winWidth);

			Console.CursorVisible = false;

			if (centered) {
				if (animated && !IsOutputRedirected) {
					//Console.SetCursorPosition(pad.Length + prompt.Length, top);
					th = new Thread(new ParameterizedThreadStart(promptThread));
					th.Start(new { animated = true, prompt = prompt, left = left, centered = true, padChar = padChar, animatedColor = animatedColor, promptColor = promptColor, padColor = padColor });
				} else {
					promptThread(new { animated = false, prompt = prompt, left = left, centered = true, padChar = padChar, animatedColor = animatedColor, promptColor = promptColor, padColor = padColor });
				}
			} else {
				if (animated && !IsOutputRedirected) {
					//Console.SetCursorPosition(prompt.Length, top);
					th = new Thread(new ParameterizedThreadStart(promptThread));
					th.Start(new { animated = true, prompt = prompt, left = left, centered = false, padChar = padChar, animatedColor = animatedColor, promptColor = promptColor, padColor = padColor });
				} else {
					promptThread(new { animated = false, prompt = prompt, left = left, centered = false, padChar = padChar, animatedColor = animatedColor, promptColor = promptColor, padColor = padColor });
				}
			}

			k = Console.ReadKey(interceptKey);

			if (animated && !IsOutputRedirected) {
				if (th != null) {
					if (th.IsAlive) {
						th.Abort();
					}
					th.Join(1000);
					th = null;
				}
			}

			if (clearAfter) {
				top2 = Console.CursorTop;
				for (int t = top; t <= top2; t++) {
					if (t == top) {
						Console.SetCursorPosition(left, t);
						Console.Write(new string(' ', winWidth - left));
					} else {
						Console.SetCursorPosition(0, t);
						Console.Write(empty);
					}
				}

				Console.SetCursorPosition(left, top);
			}

			Console.CursorVisible = curVis;

			return k;
		}

		private static void promptThread( object obj )
		{
			dynamic data = obj;

			ConsoleColor backupColor = Console.ForegroundColor;
			bool bit = false;
			int top = Console.CursorTop;
			int w;
			string pad, pad2;

			try {

				while (true) {
					if (data.centered) {
						w = (Console.WindowWidth - data.prompt.Length) / 2;
						pad = new string(data.padChar, w);
						if (pad.Length + data.prompt.Length + pad.Length == Console.WindowWidth - 1) {
							pad2 = new string(data.padChar, w + 1);
						} else {
							pad2 = pad;
						}

						Console.SetCursorPosition(data.left, top);

						Console.ForegroundColor = data.padColor;
						Console.Write(pad);

						if (bit) {
							Console.ForegroundColor = data.animatedColor;
						} else {
							Console.ForegroundColor = data.promptColor;
						}
						Console.Write(data.prompt);

						Console.ForegroundColor = data.padColor;
						Console.Write(pad2);
					} else {
						if (bit) {
							Console.ForegroundColor = data.animatedColor;
						} else {
							Console.ForegroundColor = data.promptColor;
						}
						Console.CursorLeft = data.left;
						Console.Write(data.prompt);
					}

					if (!data.animated) {
						break;
					}
					bit = !bit;
					Thread.Sleep(bit ? 400 : 750);
				}

			} catch (ThreadAbortException) {
				// do nothing..
			} finally {
				//Console.CursorTop = top;
				Console.ForegroundColor = backupColor;
			}
		}

		public static void SimplePressAnyKey( string prompt = "Press any key to exit" )
		{
			Console.CursorVisible = false;

			Console.Out.Write(Text.WriteCenteredLine(" " + prompt + " ", '-').TrimEnd());
			Console.ReadKey(true);

			ConsoleEx.ClearLine();
			Console.CursorVisible = true;
		}

		public static void ClearLine( int startCol = 0 )
		{
			Console.CursorVisible = false;

			Console.SetCursorPosition(startCol, Console.CursorTop);
			Console.Write(new string(' ', Console.WindowWidth));

			Console.SetCursorPosition(startCol, Console.CursorTop - 1);
			Console.CursorVisible = true;
		}
	}
}
