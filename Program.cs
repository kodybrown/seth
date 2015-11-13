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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Bricksoft.PowerCode;

namespace Bricksoft.DosToys.seth
{
	public class Program
	{
		private static int DEFAULT_ENVAR_INDENT = 16;
		private static bool DEFAULT_ENVAR_ALIGN = false;

		private static bool pausePerPage = false;
		private static bool pauseAtEnd = false;
		private static bool dontWrapOutput = false;
		private static int width = Console.WindowWidth;
		private static int envarIndentation = DEFAULT_ENVAR_INDENT;
		private static bool rightAligned = DEFAULT_ENVAR_ALIGN;

		private static bool showAll = true;
		private static bool showMachine = false;
		private static bool showProcess = false;
		private static bool showUser = false;
		private static bool? lowerKey = null;

		private static string filter = "";
		private static FilterBy filterBy = FilterBy.both;
		private static bool use_regex = false;

		public enum FilterBy
		{
			both,
			name,
			value
		}

		public static int Main( string[] arguments )
		{
			//Console.WindowWidth = 100;
			//Console.BufferWidth = 100;
			//Console.WindowHeight = Console.LargestWindowHeight - 1;
			//Console.BufferHeight = 1000;

			for (int i = 0; i < arguments.Length; i++) {
				string arg = arguments[i];
				bool isOpt = false;

				while (arg.StartsWith("/") || arg.StartsWith("-")) {
					arg = arg.Substring(1);
					isOpt = true;
				}

				if (isOpt) {
					if (arg == "?" || arg.Equals("h", StringComparison.CurrentCultureIgnoreCase)
							|| arg.Equals("help", StringComparison.CurrentCultureIgnoreCase)) {
						ShowUsage();
						return 0;

					} else if (arg.Equals("v", StringComparison.CurrentCultureIgnoreCase)
							|| arg.Equals("version", StringComparison.CurrentCultureIgnoreCase)) {
						ShowVersion(arg.Equals("version", StringComparison.CurrentCultureIgnoreCase));
						return 0;

					} else if (arg.Equals("p", StringComparison.CurrentCultureIgnoreCase)
							|| arg.Equals("pause", StringComparison.CurrentCultureIgnoreCase)
							|| arg.Equals("pause-per-page", StringComparison.CurrentCultureIgnoreCase)) {
						pausePerPage = true;
					} else if (arg.Equals("pp", StringComparison.CurrentCultureIgnoreCase)
							|| arg.Equals("pause-at-end", StringComparison.CurrentCultureIgnoreCase)) {
						pauseAtEnd = true;

					} else if (arg.Equals("no-wrap", StringComparison.CurrentCultureIgnoreCase)) {
						dontWrapOutput = true;
					} else if (arg.StartsWith("wrap", StringComparison.CurrentCultureIgnoreCase)
							|| arg.StartsWith("width", StringComparison.CurrentCultureIgnoreCase)) {
						int pos = arg.IndexOfAny(new char[] { '=', ':' });
						if (pos == -1) {
							Console.Error.WriteLine("invalid option: " + arg);
							return 1;
						}
						string tmp = arg.Substring(pos + 1).Trim();
						int tmpWrap = 0;
						if (!int.TryParse(tmp, out tmpWrap)) {
							Console.Error.WriteLine("invalid option: " + arg);
							return 1;
						}
						width = Math.Max(20, tmpWrap);

					} else if (arg.Equals("no-indent", StringComparison.CurrentCultureIgnoreCase)) {
						envarIndentation = 0;
					} else if (arg.StartsWith("indent", StringComparison.CurrentCultureIgnoreCase)) {
						int pos = arg.IndexOfAny(new char[] { '=', ':' });
						if (pos == -1) {
							Console.Error.WriteLine("invalid option: " + arg);
							return 1;
						}
						string tmp = arg.Substring(pos + 1).Trim();
						int tmpIndent = 0;
						if (!int.TryParse(tmp, out tmpIndent)) {
							Console.Error.WriteLine("invalid option: " + arg);
							return 1;
						}
						envarIndentation = Math.Max(0, tmpIndent);

					} else if (arg.StartsWith("align", StringComparison.CurrentCultureIgnoreCase)) {
						int pos = arg.IndexOfAny(new char[] { '=', ':' });
						if (pos == -1) {
							Console.Error.WriteLine("invalid option: " + arg);
							return 1;
						}
						string tmp = arg.Substring(pos + 1).Trim();
						if (tmp.StartsWith("l", StringComparison.CurrentCultureIgnoreCase)) {
							rightAligned = false;
						} else if (tmp.StartsWith("r", StringComparison.CurrentCultureIgnoreCase)) {
							rightAligned = true;
						} else {
							Console.Error.WriteLine("invalid option: " + arg);
							return 1;
						}

					} else if (arg.Equals("lower", StringComparison.CurrentCultureIgnoreCase)
							|| arg.Equals("lower-case", StringComparison.CurrentCultureIgnoreCase)) {
						lowerKey = true;
					} else if (arg.Equals("upper", StringComparison.CurrentCultureIgnoreCase)
							|| arg.Equals("upper-case", StringComparison.CurrentCultureIgnoreCase)) {
						lowerKey = false;

					} else if (arg.Equals("all", StringComparison.CurrentCultureIgnoreCase)) {
						showAll = true;
					} else if (arg.Equals("machine", StringComparison.CurrentCultureIgnoreCase)) {
						showMachine = true;
						showAll = false;
					} else if (arg.Equals("process", StringComparison.CurrentCultureIgnoreCase)) {
						showProcess = true;
						showAll = false;
					} else if (arg.Equals("user", StringComparison.CurrentCultureIgnoreCase)) {
						showUser = true;
						showAll = false;

					} else if (arg.Equals("name", StringComparison.CurrentCultureIgnoreCase)) {
						filterBy = FilterBy.name;
					} else if (arg.Equals("value", StringComparison.CurrentCultureIgnoreCase)) {
						filterBy = FilterBy.value;

					} else if (arg.Equals("regex", StringComparison.CurrentCultureIgnoreCase)) {
						use_regex = true;

					} else {
						Console.Error.WriteLine("unknown command: " + arg);
						return 2;
					}
				} else {
					filter = (filter + " " + arg).Trim();
				}
			}

			StringBuilder s = new StringBuilder();

			if (showAll) {
				// Show all..
				Text.WriteCenteredLine(" All Environment Variables ", '-');
				ShowVariables(Environment.GetEnvironmentVariables(), s);
			} else {
				if (showMachine) {
					Text.WriteCenteredLine(" Machine Environment Variables ", '-');
					ShowVariables(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine), s);
				}
				if (showProcess) {
					Text.WriteCenteredLine(" Process Environment Variables ", '-');
					ShowVariables(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process), s);
				}
				if (showUser) {
					Text.WriteCenteredLine(" User Environment Variables ", '-');
					ShowVariables(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User), s);
				}
			}

			// Write the results to the screen
			if (ConsoleEx.IsOutputRedirected || dontWrapOutput) {
				Console.Out.Write(s.ToString());
			} else {
				int count = 0;

				if (pausePerPage) {
					string[] lines = s.ToString().Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
					int h = Console.WindowHeight;

					for (int i = 0; i < lines.Length; i++) {
						if (count >= h - 1) {
							ConsoleEx.SimplePressAnyKey("Press any key to continue");
							count = 0;
						}
						Console.Out.WriteLine(lines[i].TrimEnd());
						count++;
					}
				} else {
					Console.Out.Write(s.ToString());
				}

				if (pauseAtEnd && (!pausePerPage || count > 0)) {
					ConsoleEx.SimplePressAnyKey();
				}
			}

			return 0;
		}

		private static void ShowVariables( IDictionary envars, StringBuilder s )
		{
			List<string> names = new List<string>();
			int w = 0;

			//const string wrappedLineSymbol = "└─»";

			foreach (DictionaryEntry de in envars) {
				names.Add(de.Key.ToString());
				w = Math.Max(w, de.Key.ToString().Length + 1);
			}

			names.Sort();

			// Set max width, just in case
			w = Math.Min(w, envarIndentation);
			string key, value,
				sep = " = ";

			for (int n = 0; n < names.Count; n++) {

				// PREPARE NAME & VALUE
				if (lowerKey.HasValue) {
					if (lowerKey.Value) {
						key = names[n].ToLowerInvariant();
					} else {
						key = names[n].ToUpperInvariant();
					}
				} else {
					key = names[n];
				}
				value = envars[names[n]].ToString();

				// FILTER
				if (filter != null && filter.Length > 0) {
					if (filter.StartsWith("regex:", StringComparison.InvariantCultureIgnoreCase)) {
						use_regex = true;
						filter = filter.Substring(6);
					}

					bool matches_name, matches_value;
					if (use_regex) {
						// `regex` filter
						matches_name = Regex.IsMatch(key, filter, RegexOptions.Singleline | RegexOptions.IgnoreCase);
						matches_value = Regex.IsMatch(value, filter, RegexOptions.Singleline | RegexOptions.IgnoreCase);
					} else {
						// `contains` filter
						matches_name = key.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) > -1;
						matches_value = value.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) > -1;
					}
					if ((filterBy == FilterBy.both && !matches_name && !matches_value)
							|| (filterBy == FilterBy.name && !matches_name)
							|| (filterBy == FilterBy.value && !matches_value)) {
						continue;
					}
				}

				bool containsPath = false;
				string[] ar = new string[] { };
				string dir = rightAligned ? "" : "-";
				int tempWidth = w;

				// This indents subsequent paths to match the envar name,
				// at whatever indentation it is at:
				// >seth --indent=0
				// PATH ╤ C:\Program Files (x86)\iis express\PHP\v5.4;
				//      ├ C:\Program Files (x86)\PHP\v5.6;
				//      └ C:\bin;
				// PSModulePath ╤ C:\Windows\system32\WindowsPowerShell\v1.0\Modules\;
				//              ├ C:\Program Files (x86)\Microsoft SQL Server\120\Tools\PowerShell\Modules\;
				//              └ C:\Program Files (x86)\Microsoft SDKs\Azure\PowerShell\ServiceManagement;
				tempWidth = Math.Max(w, key.Length);

				if (value.IndexOf('\\') > -1 && value.IndexOf(';') > -1) {
					ar = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					if (ar.Length > 1) {
						containsPath = true;
					} else {
						containsPath = false;
					}
				}

				if (containsPath && ar.Length > 0) {
					if (ConsoleEx.IsOutputRedirected || dontWrapOutput) {
						s.AppendLine(string.Format("{0," + dir + w + "}{1}{2};", key, sep, ar[0]));

						for (int i = 1; i < ar.Length; i++) {
							s.AppendFormat("{0," + dir + w + "}{0," + sep.Length + "}{1};", " ", ar[i])
							 .AppendLine();
						}
					} else {
						string tempSep = ""; //┬╤

						s.AppendLine(Text.Wrap(string.Format("{0," + dir + w + "}{1}{2}", key, " ╤ ", ar[0] + "; ")
							, new int[] { width }
							, new int[] { 0, tempWidth + sep.Length }));

						for (int i = 1; i < ar.Length; i++) {
							if (i == ar.Length - 1) {
								tempSep = " └ ";
							} else {
								tempSep = " ├ ";
							}

							s.AppendLine(Text.Wrap(string.Format("{0," + dir + tempWidth + "}{1}{2}", " ", tempSep, ar[i] + ";")
								, new int[] { width }
								, new int[] { 0, tempWidth + sep.Length }));
						}
					}
				} else {
					if (ConsoleEx.IsOutputRedirected || dontWrapOutput) {
						s.AppendLine(string.Format("{0," + dir + w + "}{1}{2}", key, sep, value));
					} else {
						s.AppendLine(Text.Wrap(string.Format("{0," + dir + w + "}{1}{2}", key, sep, value)
							, new int[] { width }
							, new int[] { 0, tempWidth + sep.Length }));
					}
				}
			}
		}

		private static void ShowVersion( bool fullVersion )
		{
			FileVersionInfo fi = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
			if (fullVersion) {
				Console.WriteLine("{0}.exe v{1}", fi.ProductName.ToLowerInvariant(), fi.FileVersion);
				Console.WriteLine(fi.LegalCopyright);
			} else {
				Console.WriteLine(fi.FileVersion);
			}
		}

		private static void ShowUsage()
		{
			ShowVersion(true);

			int indentation = 16 + 2;

			Console.WriteLine();
			Console.WriteLine(Text.Wrap("Displays the environment variables with various options.", width, 2));
			Console.WriteLine();
			Console.WriteLine("USAGE: seth [options] [filter]");
			Console.WriteLine();
			Console.WriteLine(Text.Wrap("/?, -h          show this help", width, 2, indentation));
			Console.WriteLine(Text.Wrap("-p, --pause     pauses after each screenful (applies -pp)", width, 2, indentation));
			Console.WriteLine(Text.Wrap("-pp             pauses at the end", width, 2, indentation));
			Console.WriteLine();
			Console.WriteLine(Text.Wrap("--no-wrap       outputs formatted output, but without wrapping envar values. this format is used when the output is being redirected.", width, 2, indentation));
			Console.WriteLine(Text.Wrap("--wrap=n        forces wrapping at n characters instead of the window width. enforces minimum value of 20.", width, 2, indentation));
			Console.WriteLine();
			Console.WriteLine(Text.Wrap("--align=[l|r]   aligns the envar name left or right. the default is " + (DEFAULT_ENVAR_ALIGN ? "right" : "left") + ".", width, 2, indentation));
			Console.WriteLine();
			Console.WriteLine(Text.Wrap("--indent=n      sets the envar name indentation. the default is " + DEFAULT_ENVAR_INDENT + " characters.", width, 2, indentation));
			Console.WriteLine(Text.Wrap("--no-indent     sets the envar name indentation to 0.", width, 2, indentation));
			Console.WriteLine();
			Console.WriteLine(Text.Wrap("--lower         lower-cases the envar names.", width, 2, indentation));
			Console.WriteLine(Text.Wrap("--upper         upper-cases the envar names.", width, 2, indentation));
			Console.WriteLine(Text.Wrap("                if --lower and --upper are not specified, the envar name is not modified.", width, 2, indentation));
			Console.WriteLine();
			Console.WriteLine(Text.Wrap("--machine       shows only the machine-level environment variables.", width, 2, indentation));
			Console.WriteLine(Text.Wrap("--process       shows only the process-level environment variables.", width, 2, indentation));
			Console.WriteLine(Text.Wrap("--user          shows only the user environment variables.", width, 2, indentation));
			Console.WriteLine(Text.Wrap("--all           shows all environment variables regardless of where it came from. this is the default behavior.", width, 2, indentation));
			Console.WriteLine();
			Console.WriteLine("Filter options:");
			Console.WriteLine();
			Console.WriteLine(Text.Wrap("--name          indicates to only filter by comparing against the envar name.", width, 2, indentation));
			Console.WriteLine(Text.Wrap("--value         indicates to only filter by comparing against the envar's value.", width, 2, indentation));
			Console.WriteLine(Text.Wrap("                the filter is compared against both the name and value by default.", width, 2, indentation));
			Console.WriteLine();
			Console.WriteLine(Text.Wrap("--regex         indicates that the [filter] is a regular expression. you can also prefix the filter with `regex:` as in `seth regex:AppData$`.", width, 2, indentation));
			Console.WriteLine();
			Console.WriteLine(Text.Wrap("REGEX NOTE: if you are including any of the special dos symbols in your regex (such as '^', '(', ')', '<', '>', etc.), you must wrap them in quotes, for instance `seth --regex \"^AppData\"`.", width, 2, indentation - 4));
			Console.WriteLine();
		}
	}
}