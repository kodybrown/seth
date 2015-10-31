//
// Copyright (C) 2013-2015 Kody Brown (kody@bricksoft.com).
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
using System.Runtime.InteropServices;

namespace Bricksoft.PowerCode
{
	public static class console
	{
		#region Console Redirection

		public static bool IsOutputRedirected
		{
			get { return FileType.Char != GetFileType(GetStdHandle(StdHandle.Stdout)); }
		}

		public static bool IsInputRedirected
		{
			get { return FileType.Char != GetFileType(GetStdHandle(StdHandle.Stdin)); }
		}

		public static bool IsErrorRedirected
		{
			get { return FileType.Char != GetFileType(GetStdHandle(StdHandle.Stderr)); }
		}

		// P/Invoke:
		private enum FileType { Unknown, Disk, Char, Pipe };
		private enum StdHandle { Stdin = -10, Stdout = -11, Stderr = -12 };

		[DllImport("kernel32.dll")]
		private static extern FileType GetFileType( IntPtr hdl );

		[DllImport("kernel32.dll")]
		private static extern IntPtr GetStdHandle( StdHandle std );

		#endregion

		#region Console Candy

		public static void PressAnyKey( string prompt = "Press any key to exit" )
		{
			Console.CursorVisible = false;

			Console.Out.Write(Text.WriteCenteredLine(" " + prompt + " ", '-').TrimEnd());
			Console.ReadKey(true);

			console.ClearLine();
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

		#endregion
	}

}
