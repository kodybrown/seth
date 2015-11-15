/*!
	Copyright (C) 2003-2013 Kody Brown (kody@bricksoft.com).
	
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
using System.IO;
using System.Reflection;
using System.Text;

namespace Bricksoft.PowerCode
{
	public class Config : IEnumerable<KeyValuePair<string, object>>
	{
		private Dictionary<string, object> data;
		private Dictionary<string, string> comments;

		private string file
		{
			get { return _file; }
			set
			{
				_file = value != null ? value.Trim() : "";
			}
		}
		private string _file;

		//public object this[string key]
		//{
		//	get
		//	{
		//		if (data.ContainsKey(key)) {
		//			return data[key];
		//		} else {
		//			return null;
		//		}
		//	}
		//	set
		//	{
		//		if (data.ContainsKey(key)) {
		//			data[key] = value;
		//		} else {
		//			data.add(key, value);
		//		}
		//	}
		//}

		/// <summary>
		/// Returns whether the config file exists on disk or not.
		/// </summary>
		public bool exists()
		{
			return file.Length > 0 && File.Exists(file);
		}

		/// <summary>
		/// Creates a new instance of the class.
		/// </summary>
		public Config()
		{
			data = new Dictionary<string, object>();
			comments = new Dictionary<string, string>();
			file = "";
		}

		/// <summary>
		/// Creates a new instance of the class and reads the specified file into memory immediately.
		/// </summary>
		public Config( string file )
			: this()
		{
			this.file = file;
			this.read();
		}


		#region IEnumerable<KeyValuePair<string,object>> Members

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			return data.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return data.GetEnumerator();
		}

		#endregion

		public void clear() { data.Clear(); }

		public bool read( bool useDefaultFileName, string extension = ".config" )
		{
			if (useDefaultFileName) {
				string assemblyLocation;

				assemblyLocation = Assembly.GetEntryAssembly().Location;
				this.file = Path.Combine(Path.GetDirectoryName(assemblyLocation), Path.GetFileNameWithoutExtension(assemblyLocation)) + extension;
				return read(this.file);
			} else {
				return read();
			}
		}

		public bool read() { return read(this.file); }

		public bool read( string file )
		{
			const string NEXTNAME = ">X<NEXT_NAME>X<";
			string[] ar;
			string l, value, name;
			short shortVal;
			int intVal;
			long longVal;
			ulong ulongVal;
			DateTime dtVal;

			if (file == null || (file = file.Trim()).Length == 0) {
				throw new ArgumentNullException("file");
			}

			data.Clear();
			comments.Clear();

			if (!File.Exists(file)) {
				return false;
			}

			using (StreamReader reader = File.OpenText(file)) {
				while (!reader.EndOfStream) {
					l = reader.ReadLine();
					if (l == null) {
						continue;
					}

					if (l.Trim().Length == 0 || !l.Contains("=") || l.TrimStart().StartsWith(";") || l.TrimStart().StartsWith("#")) {
						if (comments.ContainsKey(NEXTNAME)) {
							comments[NEXTNAME] += Environment.NewLine + l;
						} else {
							comments.Add(NEXTNAME, l);
						}
						continue;
					}

					ar = l.Split(new char[] { '=' }, 2);
					if (ar.Length != 2) {
						continue;
					}

					name = ar[0].Trim();
					value = ar[1];

					if (this.contains(name)) {
						throw new InvalidDataException("key is used more than once: " + name);
					}

					if (value.Equals("true", StringComparison.InvariantCultureIgnoreCase)) {
						this.attr(name, true);
					} else if (value.Equals("false", StringComparison.InvariantCultureIgnoreCase)) {
						this.attr(name, false);
					} else if (value.StartsWith("[\"") && value.EndsWith("\"]")) {
						// string[]
						this.attr(name, unescapeArray(value));
					} else if (short.TryParse(value, out shortVal)) {
						this.attr(name, shortVal);
					} else if (int.TryParse(value, out intVal)) {
						this.attr(name, intVal);
					} else if (long.TryParse(value, out longVal)) {
						this.attr(name, longVal);
					} else if (ulong.TryParse(value, out ulongVal)) {
						this.attr(name, ulongVal);
					} else if (DateTime.TryParse(value, out dtVal)) {
						this.attr(name, dtVal);
					} else {
						// string and everything else..
						this.attr(name, unescapeStrings(value));
					}

					if (comments.ContainsKey(NEXTNAME)) {
						string m = comments[NEXTNAME];
						comments.Remove(NEXTNAME);
						comments.Add(name, m);
					}
				}

				reader.Close();
			}

			return true;
		}

		public bool write() { return write(file); }

		public bool write( string file )
		{
			if (file == null || (file = file.Trim()).Length == 0) {
				throw new ArgumentNullException("file");
			}

			if (File.Exists(file)) {
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}

			using (StreamWriter w = File.CreateText(file)) {
				foreach (KeyValuePair<string, object> p in this.data) {
					if (comments.ContainsKey(p.Key)) {
						w.WriteLine(comments[p.Key]);
					}
					if (p.Value != null) {
						if (p.Value is bool) {
							w.WriteLine(p.Key + "=" + p.Value.ToString().ToLower());
						} else if (p.Value is DateTime) {
							w.WriteLine(p.Key + "=" + p.Value.ToString());
						} else if (p.Value is string) {
							w.WriteLine(p.Key + "=" + escapeStrings(p.Value.ToString()));
						} else if (p.Value is string[] || p.Value is List<string>) {
							w.WriteLine(p.Key + "=" + escapeArray(p.Value is string[] ? p.Value as string[] : (p.Value as List<string>).ToArray()));
						} else {
							w.WriteLine(p.Key + "=" + p.Value.ToString());
						}
					}
				}
				w.Close();
			}

			return true;
		}

		#region escape utils

		private string escapeArray( string[] values )
		{
			StringBuilder s = new StringBuilder();
			for (int i = 0; i < values.Length; i++) {
				if (s.Length > 0) {
					s.Append(",");
				}
				while (values[i].StartsWith("\"") && values[i].EndsWith("\"")) {
					values[i] = values[i].Substring(1, values[i].Length - 2);
				}
				s.Append("\"").Append(values[i].Replace("\"", "\\\"").Replace("\r", "@\\r").Replace("\n", "@\\n")).Append("\"");
			}
			return "[" + s.ToString() + "]";
		}

		private string[] unescapeArray( string value )
		{
			string[] ar;

			if (value != "[]" && !value.StartsWith("[\"") && !value.EndsWith("\"]")) {
				throw new ArgumentException("value");
			}

			value = value.Trim('[', ']');
			ar = value.Split(new string[] { "\",\"" }, StringSplitOptions.RemoveEmptyEntries);

			if (ar.Length > 1) {
				for (int i = 0; i < ar.Length; i++) {
					ar[i] = ar[i].Replace("\\\"", "\"").Replace("@\\r", "\r").Replace("@\\n", "\n");
					if (i == 0) {
						ar[i] = ar[i] + "\"";
					} else {
						ar[i] = "\"" + ar[i];
					}
				}
			}

			return ar;
		}

		private string escapeStrings( string value )
		{
			while (value.IndexOfAny(new char[] { '\r', '\n' }) > -1) {
				value = value.Replace("\r", "@\\r").Replace("\n", "@\\n");
			}
			return value;
		}

		private string unescapeStrings( string value )
		{
			while (value.IndexOf("@\\r") > -1 || value.IndexOf("@\\n") > -1) {
				value = value.Replace("@\\r", "\r").Replace("@\\n", "\n");
			}
			return value;
		}

		#endregion

		/// <summary>
		/// Returns whether the specified <paramref name="key"/> exists in the config.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool contains( string key ) { return this.contains(key, StringComparison.CurrentCultureIgnoreCase); }

		/// <summary>
		/// Returns whether the specified <paramref name="key"/> exists in the config.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="comparison"></param>
		/// <returns></returns>
		public bool contains( string key, StringComparison comparison )
		{
			foreach (KeyValuePair<string, object> entry in data) {
				if (entry.Key.Equals(key, comparison)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Removes the specified <paramref name="key"/> from the config and returns it.
		/// If the item was not found, null is returned.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public object remove( string key )
		{
			object value;

			if (data.ContainsKey(key)) {
				value = data[key];
				data.Remove(key);
				return value;
			}

			return null;
		}

		/// <summary>
		/// Gets the value of <paramref name="key"/> from the config.
		/// Returns it as type T.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public T get<T>( string key, T defaultValue )
		{
			if (key == null || key.Length == 0) {
				throw new InvalidOperationException("key is required");
			}

			if (data.ContainsKey(key)) {
				if (typeof(T) == typeof(bool) || typeof(T).IsSubclassOf(typeof(bool))
						|| typeof(T) == typeof(bool?) || typeof(T).IsSubclassOf(typeof(bool?))) {
					if ((object)data[key] == null) {
						if (typeof(T) == typeof(bool?) || typeof(T).IsSubclassOf(typeof(bool?))) {
							return (T)(object)null;
						}
					} else {
						string str = data[key].ToString();
						bool val = str.StartsWith("t", StringComparison.CurrentCultureIgnoreCase)
										|| str.StartsWith("y", StringComparison.CurrentCultureIgnoreCase);
						return (T)(object)(val);
					}

				} else if (typeof(T) == typeof(DateTime) || typeof(T).IsSubclassOf(typeof(DateTime))) {
					DateTime dt;
					if ((object)data[key] != null && DateTime.TryParse(data[key].ToString(), out dt)) {
						return (T)(object)dt;
					}
				} else if (typeof(T) == typeof(short) || typeof(T).IsSubclassOf(typeof(short))) {
					short i;
					if ((object)data[key] != null && short.TryParse(data[key].ToString(), out i)) {
						return (T)(object)i;
					}
				} else if (typeof(T) == typeof(int) || typeof(T).IsSubclassOf(typeof(int))) {
					int i;
					if ((object)data[key] != null && int.TryParse(data[key].ToString(), out i)) {
						return (T)(object)i;
					}
				} else if (typeof(T) == typeof(long) || typeof(T).IsSubclassOf(typeof(long))) {
					long i;
					if ((object)data[key] != null && long.TryParse(data[key].ToString(), out i)) {
						return (T)(object)i;
					}
				} else if (typeof(T) == typeof(ulong) || typeof(T).IsSubclassOf(typeof(ulong))) {
					ulong i;
					if ((object)data[key] != null && ulong.TryParse(data[key].ToString(), out i)) {
						return (T)(object)i;
					}
				} else if (typeof(T) == typeof(string) || typeof(T).IsSubclassOf(typeof(string))) {
					// string
					if ((object)data[key] != null) {
						return (T)(object)(data[key]).ToString();
					}
				} else if (typeof(T) == typeof(string[]) || typeof(T).IsSubclassOf(typeof(string[]))) {
					// string[]
					if ((object)data[key] != null) {
						// string array data is ALWAYS saved to the file as a string[] (even List<string>)..
						return (T)(object)data[key];
					}
				} else if (typeof(T) == typeof(List<string>) || typeof(T).IsSubclassOf(typeof(List<string>))) {
					// List<string>
					if ((object)data[key] != null) {
						// string array data is ALWAYS saved to the file as a string[] (even List<string>)..
						return (T)(object)new List<string>((string[])data[key]);
					}
				} else {
					throw new InvalidOperationException("unknown or unsupported data type was requested");
				}
			}

			return defaultValue;
		}

		/// <summary>
		/// Gets the value of <paramref name="key"/> from the config.
		/// Returns it as type T.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
		public T attr<T>( string key )
		{
			return get<T>(key, default(T));
		}

		/// <summary>
		/// Sets the value of <paramref name="key"/> in the config to the <paramref name="value"/> specified.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public T attr<T>( string key, T value )
		{
			return set<T>(key, value);
		}

		/// <summary>
		/// Sets the value of <paramref name="key"/> in the config to the <paramref name="value"/> specified.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public T set<T>( string key, T value )
		{
			if (key == null || key.Length == 0) {
				throw new InvalidOperationException("key is required");
			}

			if (data.ContainsKey(key)) {
				data[key] = value;
			} else {
				data.Add(key, value);
			}

			return value;
		}

		/// <summary>
		/// Returns the comment associated with the specified key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns>the comment associated with the specified key or null if there is no comment.</returns>
		public string comment( string key )
		{
			if (key == null || key.Length == 0) {
				throw new InvalidOperationException("key is required");
			}

			if (comments.ContainsKey(key)) {
				return comments[key];
			}

			return null;
		}

		/// <summary>
		/// Adds a comment for the specified key.
		/// If a comment already exists, it is overwritten.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void comment( string key, string value )
		{
			if (key == null || key.Length == 0) {
				throw new InvalidOperationException("key is required");
			}

			if (comments.ContainsKey(key)) {
				if (value == null) {
					comments.Remove(key);
				} else {
					comments[key] = value;
				}
			} else if (value != null) {
				comments.Add(key, value);
			}
		}
	}
}
