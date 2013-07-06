//
// Copyright (C) 2013 Kody Brown (kody@bricksoft.com).
// 
// MIT License:
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;

namespace seth
{
	public class Program
	{
		private static int maxNameWidth = 30;

		private static bool pp = false;
		private static bool machineOnly = false;
		private static bool processOnly = false;
		private static bool userOnly = false;
		private static bool lowerKey = false;

		public static int Main( string[] arguments )
		{
			//Console.WindowWidth = 100;
			//Console.BufferWidth = 100;
			//Console.WindowHeight = Console.LargestWindowHeight - 1;
			//Console.BufferHeight = 1000;

			foreach (string a in arguments) {
				if (a.StartsWith("/") || a.StartsWith("-")) {
					string arg = a.Substring(1);
					if (arg.Equals("p", StringComparison.CurrentCultureIgnoreCase)) {
						//p = true;
					} else if (arg.Equals("pp", StringComparison.CurrentCultureIgnoreCase)) {
						pp = true;
					} else if (arg.StartsWith("m", StringComparison.CurrentCultureIgnoreCase)) {
						machineOnly = true;
					} else if (arg.StartsWith("p", StringComparison.CurrentCultureIgnoreCase)) {
						processOnly = true;
					} else if (arg.StartsWith("u", StringComparison.CurrentCultureIgnoreCase)) {
						userOnly = true;
					} else if (arg.StartsWith("a", StringComparison.CurrentCultureIgnoreCase)) {
						// do nothing..
					} else if (arg.StartsWith("l", StringComparison.CurrentCultureIgnoreCase)) {
						lowerKey = true;
					} else if (arg.Equals("?", StringComparison.CurrentCultureIgnoreCase)) {
						showUsage();
						return 0;
					}
				}
			}

			string pad = new string('-', maxNameWidth);

			if (machineOnly) {
				WriteCenteredLine('-', " Machine Environment Variables ");
				ShowVariables(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine));
			} else if (processOnly) {
				WriteCenteredLine('-', " Process Environment Variables ");
				ShowVariables(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process));
			} else if (userOnly) {
				WriteCenteredLine('-', " User Environment Variables ");
				ShowVariables(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User));
			} else {
				// Combine all..
				WriteCenteredLine('-', " All Environment Variables ");
				ShowVariables(Environment.GetEnvironmentVariables());
			}

			if (pp) {
				PressAnyKey();
			}

			return 0;
		}

		private static void ShowVariables( IDictionary envs )
		{
			List<string> names = new List<string>();
			int w = 0;

			foreach (DictionaryEntry de in envs) {
				names.Add(de.Key.ToString());
				w = Math.Max(w, de.Key.ToString().Length + 1);
			}

			names.Sort();

			// Set max width, just in case
			w = Math.Min(w, maxNameWidth);
			string key, value,
				sep = " = ";

			for (int n = 0; n < names.Count; n++) {
				if (lowerKey) {
					key = names[n].ToLower();
				} else {
					key = names[n];
				}
				value = envs[names[n]].ToString();
				if (value.IndexOf('\\') > -1 && value.IndexOf(';') > -1) {
					string[] ar = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

					Console.WriteLine(Text.Wrap(string.Format("{0," + w + "}{1}{2};", key, sep, ar[0]), Console.WindowWidth - w - sep.Length, 0, w + sep.Length));
					//WritePrefixedLine("└─ ", Text.Wrap(string.Format("{0," + w + "}{1}{2};", key, sep, ar[0]), Console.WindowWidth - w - sep.Length, 0, w + sep.Length + 3));
					//--WritePrefixedLine("└─ ", w + sep.Length, Text.Wrap(string.Format("{0," + w + "}{1}{2};", key, sep, ar[0]), Console.WindowWidth - w - sep.Length - 3, 0, 3));
					//---WritePrefixedLine("└──", w + "└──".Length, Text.Wrap(string.Format("{0," + w + "}{1}{2}", key, sep, ar[0]), Console.WindowWidth - w - sep.Length, w + sep.Length, 0));

					for (int i = 1; i < ar.Length; i++) {
						WritePrefixedLine("└──", w + "└──".Length, Text.Wrap(string.Format("{0};", ar[i]), Console.WindowWidth - w - sep.Length, w + sep.Length, 0));
						//--WritePrefixedLine("└─ ", w + sep.Length, Text.Wrap(string.Format("{0};", ar[i]), Console.WindowWidth - w - sep.Length - 3, 0, 3));
					}
				} else {
					//works! --Console.WriteLine("{0," + w + "}{1}{2}", key, sep, value);
					Console.WriteLine("{0," + w + "}{1}{2}", key, sep, Text.Wrap(value, Console.WindowWidth - w - sep.Length, 0, w + sep.Length));

					//Console.WriteLine(Text.Wrap(string.Format("{0," + w + "}{1}{2}", key, sep, value), Console.WindowWidth - 1, 0, w + sep.Length + 3));
					//--WritePrefixedLine("└─ ", w + sep.Length, Text.Wrap(string.Format("{0," + w + "}{1}{2}", key, sep, value), Console.WindowWidth - 1, 0, w + sep.Length + 3));
				}
			}
		}

		private static void WritePrefixedLine( string prefix, int indentation, string s )
		{
			string[] lines = s.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			string pad = new string(' ', indentation);

			for (int l = 0; l < lines.Length; l++) {
				if (l > 0) {
					Console.WriteLine(pad + prefix + lines[l]);
				} else {
					Console.WriteLine(lines[l]);
				}
			}
		}

		private static void showUsage()
		{
			Console.WriteLine("seth.exe");
			Console.WriteLine("displays the current environment variables.");
			Console.WriteLine();
			Console.WriteLine("USAGE: seth [options]");
			Console.WriteLine();
			Console.WriteLine("   -p   pauses at the end");
			Console.WriteLine("   -pp  pauses after each screenful (applies -p)");
			Console.WriteLine();
		}

		private static void WriteCenteredLine( char ch, string s )
		{
			int w;
			string pad;

			w = (Console.WindowWidth - s.Length) / 2 - 1;

			pad = new string(ch, w);

			Console.WriteLine();
			Console.WriteLine();
			Console.SetCursorPosition(0, Console.CursorTop - 2);
			Console.WriteLine(pad + s + pad);
		}

		private static void PressAnyKey()
		{
			WriteCenteredLine('-', " Press any key to exit ");

			Console.CursorVisible = false;
			Console.ReadKey(true);
			Console.SetCursorPosition(0, Console.CursorTop);

			Console.WriteLine(new string(' ', Console.WindowWidth - 1));
			Console.CursorVisible = true;
		}
	}

	public static class Text
	{
		internal static char[] lineBreaks = new char[] { ' ', '.', ',', '-', ':', ';', '>', '"', ']', '}', '!', '?', ')', '\\', '/' };

		/* ----- WrapText() ----- */

		public static string Wrap( string Text ) { return Wrap(Text, null, null); }

		public static string Wrap( string Text, int WrapWidth ) { return Wrap(Text, WrapWidth, 0); }

		public static string Wrap( string Text, int WrapWidth, params int[] Indentations ) { return Wrap(Text, new int[] { WrapWidth }, Indentations); }

		public static string Wrap( string Text, int[] WrapWidths, int[] Indentations )
		{
			List<string> ar;
			string temp;
			int brk;
			int w;
			int wrapIndex, idIndex;
			List<string> ids;

			if (WrapWidths == null || WrapWidths.Length == 0) {
				WrapWidths = new int[] { Console.WindowWidth };
			}
			if (Indentations == null || Indentations.Length == 0) {
				Indentations = new int[] { 0 };
			}

			brk = -1;
			ar = new List<string>(Text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None));
			wrapIndex = 0;
			idIndex = 0;
			ids = new List<string>(Indentations.Length);

			Func<int> wrapWidthIdx = delegate()
			{
				return Math.Min(wrapIndex, WrapWidths.Length - 1);
			};
			Func<int> identIdx = delegate()
			{
				return Math.Min(idIndex, Indentations.Length - 1);
			};

			foreach (int indent in Indentations) {
				ids.Add(new string(' ', indent));
			}

			for (int i = 0; i < ar.Count; i++) {
				w = WrapWidths[wrapWidthIdx()] - Indentations[identIdx()] - 1;
				if (ar[i].Length > w) {
					temp = ar[i];
					brk = temp.Substring(0, w).LastIndexOfAny(lineBreaks) + 1;

					ar[i] = ids[identIdx()] + temp.Substring(0, brk);
					wrapIndex++;
					idIndex++;
					i++;

					//temp = temp.Substring(brk);
					if (brk < temp.Length - 1 && temp[brk] == ' ') {
						temp = temp.Substring(brk + 1);
					} else {
						temp = temp.Substring(brk);
					}

					while (temp.Length > 0) {
						w = WrapWidths[wrapWidthIdx()] - Indentations[identIdx()] - 1;
						if (temp.Length > w) {
							brk = temp.Substring(0, w).LastIndexOfAny(lineBreaks) + 1;
							if (brk == 0) {
								// The string could not be broken up nicely, 
								// so just cut the line at the wrap width..
								brk = w;
							}
						} else {
							brk = temp.Length;
						}

						ar.Insert(i, ids[identIdx()] + temp.Substring(0, brk));
						wrapIndex++;
						idIndex++;
						i++;

						if (brk < temp.Length - 1 && temp[brk] == ' ') {
							temp = temp.Substring(brk + 1);
						} else {
							temp = temp.Substring(brk);
						}
					}
				} else {
					ar[i] = ids[identIdx()] + ar[i];
				}

				wrapIndex++;
				idIndex++;
			}

			//return string.Join(Environment.NewLine + padding, ar);
			return string.Join("\n", ar);
		}
	}
}
