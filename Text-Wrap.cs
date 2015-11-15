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
using System.Collections.Generic;
using System.Text;

namespace Bricksoft.PowerCode
{
	/// <summary>
	/// Common methods for manipulating text.
	/// </summary>
	public partial class Text
	{
		/// <summary>
		/// Gets or sets the characters that lines should be wrapped on.
		/// </summary>
		public static List<char> LineBreaks { get; set; }

		/// <summary>
		/// Gets or set a list of overrides to prevent inserting a line between these and a following letter/word.
		/// </summary>
		public static List<string> LineBreakOverrides { get; set; }

		/// <summary>
		/// Gets or sets the line suffix. This is the string to show at the end of a line when it is wrapped.
		/// </summary>
		public static string LineWrapSuffix { get; set; }

		public static bool ShowLineSuffix { get; set; }

		static Text()
		{
			LineBreaks = new List<char>(new char[] { ' ', '.', ',', ':', ';', '>', '-', ']', '}', '!', '?', ')', '\\', '/' });
			LineBreakOverrides = new List<string>(new string[] { "--", " /", " `", "\"" });
			LineWrapSuffix = new string((char)26, 1);
			ShowLineSuffix = false;
		}

		#region TODO / FUTURE - MinimumWidth
		//public static int MinimumWidth { get; set; }

		//static Text()
		//{
		//	MinimumWidth = 1;
		//}
		#endregion

		/* ----- WrapText() ----- */

		/// <summary>
		/// Wraps the specified <paramref name="Text"/>.
		/// </summary>
		/// <param name="Text">The text to wrap.</param>
		/// <returns></returns>
		public static string Wrap( string Text )
		{
			return Wrap(Text, null, null);
		}

		/// <summary>
		/// Wraps the specified <paramref name="Text"/>.
		/// </summary>
		/// <param name="Text">The text to wrap.</param>
		/// <param name="WrapWidth">The maximum number of characters per line.</param>
		/// <returns></returns>
		public static string Wrap( string Text, int WrapWidth )
		{
			return Wrap(Text, new int[] { WrapWidth }, null);
		}

		/// <summary>
		/// Wraps the specified <paramref name="Text"/> at <paramref name="WrapWidth"/>, 
		/// while indenting each line by <paramref name="Indentations"/> spaces.
		/// </summary>
		/// <param name="Text">The text to wrap.</param>
		/// <param name="WrapWidth">The maximum number of characters per line.</param>
		/// <param name="Indentations">The number of spaces to prepend onto each line.</param>
		/// <returns></returns>
		public static string Wrap( string Text, int WrapWidth, params int[] Indentations )
		{
			return Wrap(Text, new int[] { WrapWidth }, Indentations);
		}

		/// <summary>
		/// Wraps the specified <paramref name="Text"/>.
		/// </summary>
		/// <param name="Text">The text to wrap.</param>
		/// <param name="WrapWidths"></param>
		/// <param name="Indentations"></param>
		/// <returns></returns>
		public static string Wrap( string Text, int[] WrapWidths, int[] Indentations )
		{
			List<string> ar;
			string temp, tmp;
			int brk, w, wrapIndex, idIndex;
			List<string> ids;
			bool showSuffix = false;
			string lineSuffix = "";
			int lineSuffixLen = 0;
			char[] lineBreaks = LineBreaks.ToArray();

			if (WrapWidths == null || WrapWidths.Length == 0) {
				WrapWidths = new int[] { Console.WindowWidth };
			} else {
				for (int i = 0; i < WrapWidths.Length; i++) {
					if (WrapWidths[i] == 0) {
						WrapWidths[i] = Console.WindowWidth;
					} else if (WrapWidths[i] < 0) {
						WrapWidths[i] = Console.WindowWidth - Math.Abs(WrapWidths[i]);
					}
					if (WrapWidths[i] < 1) {
						WrapWidths[i] = 1;
					}
				}
			}

			if (Indentations == null || Indentations.Length == 0) {
				Indentations = new int[] { 0 };
			} else {
				for (int i = 0; i < Indentations.Length; i++) {
					if (Indentations[i] < 0) {
						Indentations[i] = 0;
					}
				}
			}

			if (ShowLineSuffix && LineWrapSuffix != null && LineWrapSuffix.Length > 0) {
				showSuffix = true;
				if (LineWrapSuffix[0] != ' ') {
					lineSuffix = " " + LineWrapSuffix;
				} else {
					lineSuffix = LineWrapSuffix;
				}
				lineSuffixLen = LineWrapSuffix.Length;
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
				w = WrapWidths[wrapWidthIdx()] - Indentations[identIdx()];

				// Ensure the width isn't so small, that it causes an infinite loop..
				w = Math.Max(w, 1);

				if (ar[i].Length > w) {
					temp = ar[i];
					brk = temp.Substring(0, w).LastIndexOfAny(lineBreaks) + 1;

					brk = checkOverrides(temp, brk);
					if (showSuffix && brk > w - lineSuffixLen) {
						brk = temp.Substring(0, w - lineSuffixLen).LastIndexOfAny(lineBreaks) + 1;
					}

					tmp = ids[identIdx()] + temp.Substring(0, brk);

					if (brk < temp.Length - 1 && temp[brk] == ' ') {
						temp = temp.Substring(brk + 1);
					} else {
						temp = temp.Substring(brk);
					}

					if (showSuffix && temp.Length > 0) {
						tmp += (tmp[tmp.Length - 1] == ' ') ? lineSuffix.TrimStart() : lineSuffix;
					}
					ar[i] = tmp.TrimEnd();

					while (temp.Length > 0) {
						i++;
						wrapIndex++;
						idIndex++;

						w = WrapWidths[wrapWidthIdx()] - Indentations[identIdx()];

						// Ensure the width isn't so small, that it causes an infinite loop..
						w = Math.Max(w, 1);

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

						brk = checkOverrides(temp, brk);
						if (showSuffix && brk > w - lineSuffixLen) {
							brk = temp.Substring(0, w - lineSuffixLen).LastIndexOfAny(lineBreaks) + 1;
						}

						tmp = ids[identIdx()] + temp.Substring(0, brk);

						if (brk < temp.Length - 1 && temp[brk] == ' ') {
							temp = temp.Substring(brk + 1);
						} else {
							temp = temp.Substring(brk);
						}

						if (showSuffix && temp.Length > 0) {
							tmp += (tmp[tmp.Length - 1] == ' ') ? lineSuffix.TrimStart() : lineSuffix;
						}
						ar.Insert(i, tmp.TrimEnd());
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

		private static int checkOverrides( string temp, int brk )
		{
			if (temp.Length == brk) {
				return brk;
			}

			for (int l = 0; l < LineBreakOverrides.Count; l++) {
				string lb = LineBreakOverrides[l];
				int len = lb.Length;
				for (int x = len; x >= 0; x--) {
					if (temp.Length > brk - x && temp.Substring(brk - x, len) == lb) {
						brk = brk - x;
						break;
					}
				}
			}

			return brk;
		}

		public static string WriteCenteredLine( string content, char ch = ' ' )
		{
			StringBuilder s = new StringBuilder();
			int w;
			string pad;

			w = (Console.WindowWidth - content.Length) / 2 - 1;
			pad = new string(ch, w);

			//Console.WriteLine();
			//Console.WriteLine();
			//Console.SetCursorPosition(0, Console.CursorTop - 2);
			//Console.WriteLine(pad + content + pad);
			s.AppendLine(pad + content + pad);
			s.AppendLine();

			return s.ToString();
		}

	}
}
