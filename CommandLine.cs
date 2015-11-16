/*!
	Copyright (C) 2003-2015 Kody Brown (@wasatchwizard).
	
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
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace Bricksoft.PowerCode
{
	/// <summary>
	/// Provides a simple way to get the command-line arguments.
	/// <remarks>
	/// See CommandLineArguments.cs.txt for details on how to use this class.
	/// </remarks>
	/// </summary>
	public class CommandLine
	{
		private OrderedDictionary data = new OrderedDictionary();


		/// <summary>
		/// Represents an un-named command-line argument.
		/// Unnamed items in the collection have their index appended, matching the order entered on the command-line.
		/// Unnamed items are one-based.
		/// <remarks>These arguments do not begin with a - nor /, and do not have a named item preceding them.</remarks>
		/// </summary>
		public const string UnnamedItem = "UnnamedItem";

		/// <summary>
		/// Provides the StringComparison when looking for command-line arguments.
		/// 
		/// By default, this value is set based on the operating system:
		/// If the operating system's path separator character is a '\', 
		/// then StringComparison.CurrentCultureIgnoreCase is used.
		/// For all other operating systems, StringComparison.CurrentCulture is used.
		/// 
		/// To change, set this property before ParseCommandLineProperties() is called. A good place to 
		/// set it is in the main class's constructor (the class that inherits ConsoleApplication).
		/// </summary>
		public StringComparison StringComparison { get; set; }

		public bool IgnoreCase { get { return (StringComparison == StringComparison.CurrentCultureIgnoreCase) || (StringComparison == StringComparison.InvariantCultureIgnoreCase) || (StringComparison == StringComparison.OrdinalIgnoreCase); } set { StringComparison = value ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture; } }

		public bool IsEmpty { get { return data.Count == 0; } }

		private object Caller { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public string[] OriginalCmdLine { get { return _originalCmdLine; } }
		private string[] _originalCmdLine;

		/// <summary>
		/// Gets the first un-named item if it exists, otherwise 
		/// returns an empty string.
		/// </summary>
		public string UnnamedItem1 { get { return GetValue("", "UnnamedItem1"); } }

		/// <summary>
		/// 
		/// </summary>
		public List<string> UnnamedItems
		{
			get
			{
				List<string> l;
				IDictionaryEnumerator enumerator;

				l = new List<string>();
				enumerator = data.GetEnumerator();

				while (enumerator.MoveNext()) {
					if (enumerator.Key.ToString().StartsWith(UnnamedItem, StringComparison.InvariantCulture)) {
						l.Add(enumerator.Value as string);
					}
				}

				return l;
			}
		}

		// **** Indexers ---------------------------------------------------------------------------

		public object this[int Index]
		{
			get { return data[Index]; }
			set { data[Index] = value; }
		}

		public object this[string Key]
		{
			get
			{
				int index = GetIndexOfArgument(Key);
				if (index == -1)
					throw new ArgumentException("Key");
				return data[index];
			}
			set
			{
				int index = GetIndexOfArgument(Key);
				if (index == -1)
					throw new ArgumentException("Key");
				data[index] = value;
			}
		}

		//public object this[string Key, StringComparison? StrComparison = null]
		//{
		//	get
		//	{
		//		int index = GetIndexOfArgument(Key, StrComparison);
		//		if (index == -1)
		//			throw new ArgumentException("key");
		//		return data[index];
		//	}
		//	set
		//	{
		//		int index = GetIndexOfArgument(Key, StrComparison);
		//		if (index == -1) 
		//			throw new ArgumentException("key");
		//		data[index] = value;
		//	}
		//}

		/// <summary>
		/// Gets the count of command-line arguments.
		/// </summary>
		public int Count { get { return data.Count; } }

		#region -- Constructor(s) --

		/// <summary>
		/// Creates a new instance of the class.
		/// </summary>
		/// <param name="arguments"></param>
		public CommandLine( string[] arguments )
		{
			StringComparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;

			_originalCmdLine = arguments;
			ParseCommandLine(arguments);
		}

		#endregion

		// **** Parsers ---------------------------------------------------------------------------

		/// <summary>
		/// 
		/// </summary>
		/// <param name="arguments"></param>
		private void ParseCommandLine( string[] arguments )
		{
			char[] anyOf = new char[] { '=', ':' };
			int pos = -1;

			#region Supported arguments:

			// the / indicates a single parameter
			// the - and -- indicate a parameter with a trailing value and are interchangeable..
			// 
			// /name                    ( name = true )
			// /"name"                  ( name = true )
			// /"name one"              ( name one = true )
			// "/name one"              ( name = true )
			// 
			// /name=value              ( name=value = true )
			// /name:value              ( name = value )
			// /name="value here"       ( name = value here )
			// 
			// -name value              ( name = value )
			// -name "-value"           ( name = -value )
			// -name -value             ( name = -value )
			// -"name 4" "value"        ( name 4 = value )
			// "-name 4" "value"        ( name 4 = value )
			// "-name 4" "value one"    ( name 4 = value one )
			// 
			// -name1 -name2            ( name1 = name2 )
			// 
			// -name=value              ( name = value )
			// -"name"=value            ( name = value )
			// -name="value"            ( name = value )
			// -"name"="value"          ( name = value )
			// -"name=value"            ( name = value )
			// "-name=value"            ( name = value )
			// 
			// -name="value one"        ( name = value one )
			// -"name=value one"        ( name = value one )
			// "-name=value one"        ( name = value one )
			// 
			// 
			// 
			// /name "value"            ( name = true ) and ( value = true )  <-- notice the /
			// -name "value"            ( name = value )  <-- notice the -
			// 
			// 
			// -"name 1"                
			// 

			#endregion

			string arg;
			string name;
			string value;
			bool needsValue;
			int unnamedItemCount;

			name = string.Empty;
			value = string.Empty;
			needsValue = false;
			unnamedItemCount = 0;

			if (arguments == null || arguments.Length == 0) {
				return;
			}

			for (int i = 0; i < arguments.Length; i++) {
				arg = arguments[i];

				if (needsValue && name != null && name.Length > 0) {

					// Get the value for a NameValueArg argument.
					value = arg.Trim();
					while (value.StartsWith("\"") && value.EndsWith("\"")) {
						value = value.Substring(1, value.Length - 2);
					}

					Add(name, value);
					needsValue = false;

				} else if (arg.StartsWith("-")) {

					// NameValueOptional | NameValueRequired
					name = arg.Trim();
					while (name.StartsWith("-") || (name.StartsWith("\"") && name.EndsWith("\""))) {
						name = name.TrimStart('-');
						if (name.StartsWith("\"") && name.EndsWith("\"")) {
							name = name.Substring(1, name.Length - 2);
						}
					}

					pos = name.IndexOfAny(anyOf);
					if (pos > -1) {
						value = name.Substring(pos + 1);
						if (value.StartsWith("\"") && value.EndsWith("\"")) {
							value = value.Substring(1, value.Length - 2);
						}
						name = name.Substring(0, pos);
						Add(name, value);
						needsValue = false;
					} else {
						needsValue = true;
					}

				} else if (arg.StartsWith("/")) {

					// NameOnly
					name = arg.Trim();
					while (name.StartsWith("/") || (name.StartsWith("\"") && name.EndsWith("\""))) {
						name = name.TrimStart('/');
						if (name.StartsWith("\"") && name.EndsWith("\"")) {
							name = name.Substring(1, name.Length - 2);
						}
					}

					pos = name.IndexOfAny(anyOf);
					if (pos > -1) {
						value = name.Substring(pos + 1);
						if (value.StartsWith("\"") && value.EndsWith("\"")) {
							value = value.Substring(1, value.Length - 2);
						}
						name = name.Substring(0, pos);
						Add(name, value);
					} else {
						Add(name, null);
					}
					needsValue = false;

				} else {

					// UnnamedItem
					Add(UnnamedItem + (++unnamedItemCount), arg);

				}

			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="arguments"></param>
		/// <returns></returns>
		public static CommandLine Parse( string[] arguments ) { return new CommandLine(arguments); }


		/// <summary>
		/// Adds an argument with the specified <paramref name="Key"/> and <paramref name="Value"/> 
		/// into the collection with the lowest available index.
		/// <remarks>If an existing item exists in the collection, it will be overwritten.</remarks>
		/// </summary>
		/// <param name="Key">The key of the entry to add.</param>
		/// <param name="Value">The value of the entry to add. This value can be null.</param>
		public void Add( string Key, object Value )
		{
			data.Add(Key, Value);
		}

		/// <summary>
		/// Removes the entry with the specified key from the System.Collections.Specialized.OrderedDictionary collection.
		/// </summary>
		/// <param name="Key">The key of the entry to Remove.</param>
		/// <exception cref="System.NotSupportedException">The System.Collections.Specialized.OrderedDictionary collection is read-only.</exception>
		/// <exception cref="System.ArgumentNullException">key is null.</exception>
		public void Remove( string Key ) { Remove(Key); }

		public void Remove( string Key, StringComparison StringComparison ) { Remove(Key, StringComparison); }

		/// <summary>
		/// Removes the entry with the specified key from the System.Collections.Specialized.OrderedDictionary collection.
		/// </summary>
		/// <param name="Keys">The keys of the entries to Remove.</param>
		/// <exception cref="System.NotSupportedException">The System.Collections.Specialized.OrderedDictionary collection is read-only.</exception>
		/// <exception cref="System.ArgumentNullException">key is null.</exception>
		public void Remove( params string[] Keys )
		{
			foreach (string p in Keys) {
				data.Remove(p);
			}
		}

		public void RenameKey( string Key, string NewKey )
		{
			if (Contains(Key) && !Contains(NewKey)) {
				data.Add(NewKey, data[Key]);
				data.Remove(Key);
			}
		}

		/// <summary>
		/// Returns whether any of the items in <paramref name="Keys"/> exists on the command-line.
		/// </summary>
		/// <param name="Keys"></param>
		/// <returns></returns>
		public bool Contains( params string[] Keys )
		{
			return Contains(new List<string>(Keys));
		}

		public bool Contains( List<string> Keys )
		{
			IDictionaryEnumerator enumerator;
			string Key;

			if (Keys == null || Keys.Count == 0) {
				throw new ArgumentNullException("Keys");
			}

			enumerator = data.GetEnumerator();

			while (enumerator.MoveNext()) {
				Key = (string)enumerator.Key;
				foreach (string k in Keys) {
					if (Key.Equals(k, StringComparison)) {
						return true;
					}
				}
			}

			return false;
		}

		public int IndexOf( params string[] Keys )
		{
			return IndexOf(new List<string>(Keys));
		}

		public int IndexOf( List<string> Keys )
		{
			IDictionaryEnumerator enumerator;
			string Key;
			int index;

			if (Keys == null || Keys.Count == 0) {
				throw new ArgumentNullException("Keys");
			}

			enumerator = data.GetEnumerator();
			index = -1;

			while (enumerator.MoveNext()) {
				Key = (string)enumerator.Key;
				index++;
				foreach (string k in Keys) {
					if (Key.Equals(k, StringComparison)) {
						return index;
					}
				}
			}

			return -1;
		}

		/// <summary>
		/// Returns whether all items in <paramref name="Keys"/> exist on the command-line.
		/// </summary>
		/// <param name="Keys">A collection of named items to search the command-line for.</param>
		/// <returns></returns>
		public bool ContainsAllOf( params string[] Keys )
		{
			if (Keys == null) {
				throw new ArgumentNullException("Keys");
			}

			foreach (string arg in Keys) {
				if (arg == null) {
					throw new ArgumentNullException("Keys", "An element in keys is null");
				}
				if (!Contains(arg)) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns whether <paramref name="Index"/> is an un-named argument.
		/// </summary>
		/// <param name="Index">The index of the argument to check whether it is an un-named argument.</param>
		/// <returns></returns>
		public bool IsUnnamedItem( int Index )
		{
			int i;

			if (data.Keys.Count <= Index) {
				return false;
				//throw new IndexOutOfRangeException("index cannot exceed collection count.");
			}

			i = 0;

			foreach (string key in data.Keys) {
				if (i++ == Index) {
					if (key.StartsWith(UnnamedItem, StringComparison.InvariantCulture)) {
						return true;
					} else {
						return false;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns the numeric index of the <paramref name="Key"/> specified.
		/// Performs comparison ignoring the case.
		/// </summary>
		/// <param name="Key"></param>
		/// <returns></returns>
		public int GetIndexOfArgument( string Key, StringComparison? StrComparison = null )
		{
			IDictionaryEnumerator enumerator;
			string name;
			int index;
			bool foundIt;

			if (StrComparison == null) {
				StrComparison = StringComparison;
			}

			enumerator = data.GetEnumerator();
			index = 0;
			foundIt = false;

			while (enumerator.MoveNext()) {
				name = (string)enumerator.Key;
				//value = enumerator.Value as string;
				if (name.Equals(Key, StrComparison.Value)) {
					foundIt = true;
					break;
				}
				index++;
			}

			if (foundIt) {
				return index;
			} else {
				return -1;
			}
		}

		public string GetRemainingString( string DefaultValue, params string[] Keys )
		{
			foreach (string key in Keys) {
				if (HasValue(key)) {
					StringBuilder val = new StringBuilder();
					val.Append((string)this[key]).Append(' ');
					for (int i = GetIndexOfArgument(key) + 1; i < data.Count; i++) {
						val.Append((string)this[i]).Append(' ');
					}
					return val.ToString().Trim();
				}
			}
			return DefaultValue;
		}

		/// <summary>
		/// Returns the value of the command-line argument as a string, found at position <paramref name="Index"/>,
		/// INCLUDING everything on the command-line that followed, otherwise returns an empty string.
		/// </summary>
		/// <param name="Index"></param>
		/// <returns></returns>
		public string GetRemainingString( int Index ) { return GetRemainingString(Index, string.Empty); }

		/// <summary>
		/// Returns the value of the command-line argument as a string, found at position <paramref name="Index"/>,
		/// INCLUDING everything on the command-line that followed, otherwise returns <paramref name="DefaultValue"/>.
		/// </summary>
		/// <param name="Index"></param>
		/// <param name="DefaultValue"></param>
		/// <returns></returns>
		public string GetRemainingString( int Index, string DefaultValue )
		{
			StringBuilder val;

			if (this[Index] != null) {
				val = new StringBuilder();
				val.Append((string)this[Index]).Append(' ');
				for (int i = Index + 1; i < data.Count; i++) {
					val.Append((string)this[i]).Append(' ');
				}
				return val.ToString().Trim();
			}

			return DefaultValue;
		}

		/// <summary>
		/// Finds the first command-line argument found in <paramref name="Key"/> and returns
		/// everything AFTER it on the command-line that followed, otherwise returns <paramref name="DefaultValue"/>.
		/// </summary>
		/// <param name="Key"></param>
		/// <param name="DefaultValue"></param>
		/// <returns></returns>
		public string GetEverythingAfter( string Key, string DefaultValue )
		{
			if (Contains(Key) && this[Key] != null) {
				StringBuilder val = new StringBuilder();
				//val.Append((string)this[Key]).Append(' ');
				for (int i = GetIndexOfArgument(Key) + 1; i < data.Count; i++) {
					val.Append((string)this[i]).Append(' ');
				}
				return val.ToString().Trim();
			}
			return DefaultValue;
		}

		public bool Exists( string Key )
		{
			if (Key == null || Key.Length == 0) {
				throw new ArgumentNullException("Key");
			}

			if (Contains(Key)) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns whether the argument(s) contain a non-null and non-empty value.
		/// If no arguments are found it returns false.
		/// Performs comparison ignoring case.
		/// </summary>
		/// <param name="Key"></param>
		/// <returns></returns>
		public bool HasValue( string Key )
		{
			if (Key == null || Key.Length == 0) {
				throw new ArgumentNullException("Key");
			}

			if (Contains(Key) && GetValue("", Key).Length > 0) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns a collection of values loaded from the specified file.
		/// One item in collection for each line in file.
		/// Lines starting with a semi-colon (;) are ignored.
		/// </summary>
		/// <param name="FileName"></param>
		/// <param name="Values"></param>
		/// <returns></returns>
		static public bool ReadValuesFromFile( string FileName, out List<string> Values )
		{
			if (FileName == null || FileName.Length == 0) {
				throw new ArgumentNullException("fileName");
			}
			if (!File.Exists(FileName)) {
				Values = null;
				return false;
			}

			Values = new List<string>();

			try {
				foreach (string line in File.ReadAllLines(FileName)) {
					// Ignore comments in file, just in case!
					if (line.StartsWith(";")) {
						continue;
					}
					Values.Add(line);
				}
			} catch (Exception ex) {
				//values = string.Empty;
				Values.Clear();
				Values.Add(ex.Message);
				return false;
			}

			return true;
		}

		// **** GetValue() --------------------------------------------------

		public T GetValue<T>( T DefaultValue, params string[] Keys )
		{
			return GetValue(DefaultValue, new List<string>(Keys));
		}

		public T GetValue<T>( T DefaultValue, List<string> Keys )
		{
			if (Keys == null || Keys.Count == 0) {
				throw new ArgumentNullException("Keys is required");
			}

			foreach (string key in Keys) {
				if (Contains(key)) {
					if (typeof(T) == typeof(bool) || typeof(T).IsSubclassOf(typeof(bool))) {
						if ((object)this[key] != null) {
							return (T)(object)(this[key].ToString().StartsWith("t", StringComparison.CurrentCultureIgnoreCase));
						} else {
							return (T)(object)true;
						}
					} else if (typeof(T) == typeof(DateTime) || typeof(T).IsSubclassOf(typeof(DateTime))) {
						DateTime dt;
						if ((object)this[key] != null && DateTime.TryParse(this[key].ToString(), out dt)) {
							return (T)(object)dt;
						}
					} else if (typeof(T) == typeof(short) || typeof(T).IsSubclassOf(typeof(short))) {
						short i;
						if ((object)this[key] != null && short.TryParse(this[key].ToString(), out i)) {
							return (T)(object)i;
						}
					} else if (typeof(T) == typeof(int) || typeof(T).IsSubclassOf(typeof(int))) {
						int i;
						if ((object)this[key] != null && int.TryParse(this[key].ToString(), out i)) {
							return (T)(object)i;
						}
					} else if (typeof(T) == typeof(long) || typeof(T).IsSubclassOf(typeof(long))) {
						long i;
						if ((object)this[key] != null && long.TryParse(this[key].ToString(), out i)) {
							return (T)(object)i;
						}
					} else if (typeof(T) == typeof(ulong) || typeof(T).IsSubclassOf(typeof(ulong))) {
						ulong i;
						if ((object)this[key] != null && ulong.TryParse(this[key].ToString(), out i)) {
							return (T)(object)i;
						}
					} else if (typeof(T) == typeof(string) || typeof(T).IsSubclassOf(typeof(string))) {
						// string
						if ((object)this[key] != null) {
							return (T)(object)(this[key]).ToString();
						}
					} else if (typeof(T) == typeof(string[]) || typeof(T).IsSubclassOf(typeof(string[]))) {
						// string[]
						if ((object)this[key] != null) {
							// string array data is ALWAYS saved to the file as a string[] (even List<string>)..
							return (T)(object)this[key];
						}
					} else if (typeof(T) == typeof(List<string>) || typeof(T).IsSubclassOf(typeof(List<string>))) {
						// List<string>
						if ((object)this[key] != null) {
							// string array data is ALWAYS saved to the file as a string[] (even List<string>)..
							return (T)(object)new List<string>((string[])this[key]);
						}
					} else {
						throw new InvalidOperationException("unknown or unsupported data type was requested");
					}
				}
			}

			return DefaultValue; //default(T);
		}

		// **** ToString() --------------------------------------------------

		/// <summary>
		/// Output all arguments as it would be entered on the command-line.
		/// </summary>
		/// <returns></returns>
		public string[] ToArray()
		{
			List<string> result;
			IDictionaryEnumerator enumerator;
			string name;
			string value;
			int pos;

			result = new List<string>();
			enumerator = data.GetEnumerator();

			while (enumerator.MoveNext()) {
				name = (string)enumerator.Key;
				value = enumerator.Value as string;

				if (name.StartsWith(UnnamedItem, StringComparison.InvariantCulture)) {

					// UnnamedItem
					pos = value.IndexOf(' ');
					if (pos > -1) {
						result.Add("\"" + name + "\"");
					} else {
						result.Add(name);
					}

				} else if (value == null) {

					// StandAloneArg (/arg)
					pos = name.IndexOf(' ');
					if (pos > -1) {
						result.Add("/\"" + name + "\"");
					} else {
						result.Add("/" + name);
					}

				} else {

					// NameValueArg (-name value)
					pos = name.IndexOf(' ');
					if (pos > -1) {
						result.Add("-\"" + name + "\"");
					} else {
						result.Add("-" + name);
					}

					pos = value.IndexOf(' ');
					if (pos > -1) {
						result.Add("\"" + name + "\"");
					} else {
						result.Add(name);
					}

				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Output all arguments as it would be entered on the command-line.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder result;
			IDictionaryEnumerator enumerator;
			string name;
			string value;
			int pos;

			result = new StringBuilder();
			enumerator = data.GetEnumerator();

			while (enumerator.MoveNext()) {
				name = (string)enumerator.Key;
				value = enumerator.Value as string;

				if (result.Length > 0) {
					result.Append(' ');
				}

				if (name.StartsWith(UnnamedItem, StringComparison.InvariantCulture)) {

					// UnnamedItem
					pos = value.IndexOf(' ');
					if (pos > -1) {
						result.Append('"').Append(value).Append('"');
					} else {
						result.Append(value);
					}

				} else if (value == null) {

					// StandAloneArg (/arg)
					result.Append('/');
					pos = name.IndexOf(' ');
					if (pos > -1) {
						result.Append('"').Append(name).Append('"');
					} else {
						result.Append(name);
					}

				} else {

					// NameValueArg (-name value)
					result.Append('-');
					pos = name.IndexOf(' ');
					if (pos > -1) {
						result.Append('"').Append(name).Append('"');
					} else {
						result.Append(name);
					}
					result.Append(' ');
					pos = value.IndexOf(' ');
					if (pos > -1) {
						result.Append('"').Append(value).Append('"');
					} else {
						result.Append(value);
					}

				}
			}

			return result.ToString();
		}
	}

	public class CommandLineArg
	{
		public const int DEFAULT_INDEX = (int.MaxValue / 2);
		public const int DEFAULT_GROUP = (int.MaxValue / 2);

		public int SortIndex { get { return _sortIndex; } set { _sortIndex = value; } }
		private int _sortIndex = DEFAULT_INDEX;

		public int Group { get { return _group; } set { _group = value; } }
		private int _group = DEFAULT_GROUP;

		public string Name { get { return _name ?? (_name = string.Empty); } set { _name = value ?? string.Empty; } }
		private string _name = string.Empty;

		/// <summary>Gets or sets the summary description displayed in the usage.</summary>
		public string Description { get { return _description ?? (_description = string.Empty); } set { _description = value ?? string.Empty; } }
		private string _description = string.Empty;

		public string AdditionalNotes { get { return _additionalNotes ?? (_additionalNotes = string.Empty); } set { _additionalNotes = value ?? string.Empty; } }
		private string _additionalNotes = string.Empty;

		/// <summary>Gets or sets the additional help content that is displayed when you call `--help cmd`, where cmd is the current CommandLineArg.</summary>
		public string HelpContent { get { return _helpContent ?? (_helpContent = string.Empty); } set { _helpContent = value ?? string.Empty; } }
		private string _helpContent = string.Empty;

		/// <summary>Gets or sets the error text when the command-line argument was not provided and was set to <seealso cref="Required"/>.</summary>
		/// <remarks>If not specified the <seealso name="AppDescription"/> is used in the error message.</remarks>
		public string MissingText { get { return _missingText ?? (_missingText = string.Empty); } set { _missingText = value ?? string.Empty; } }
		private string _missingText = string.Empty;

		/// <summary>Gets or sets whether the current command-line argument or flag is required.</summary>
		public bool Required { get { return _required; } set { _required = value; } }
		private bool _required = false;

		/// <summary>Gets or sets the keys to check for on the command-line. These keys are displayed in the usage.</summary>
		public List<string> Keys { get { return _keys ?? (_keys = new List<string>()); } set { _keys = value ?? new List<string>(); } }
		private List<string> _keys = new List<string>();

		public void AddKeys( params string[] Keys )
		{
			foreach (string k in Keys) {
				//if (k.StartsWith("/") || k.StartsWith("-")) {
				//	throw new ArgumentException("The items in the Keys property must not be prefixed by - or /. The correct prefix will be displayed in usage based on the Options property.");
				//}
				this.Keys.Add(k);
			}
		}

		/// <summary>Gets or sets additional keys to check for on the command-line, but that aren't displayed in the usage.</summary>
		public List<string> ExtraKeys { get { return _extraKeys ?? (_extraKeys = new List<string>()); } set { _extraKeys = value ?? new List<string>(); } }
		private List<string> _extraKeys = new List<string>();

		public void AddExtraKeys( params string[] Keys )
		{
			foreach (string k in Keys) {
				//if (k.StartsWith("/") || k.StartsWith("-")) {
				//	throw new ArgumentException("The items in the Keys property must not be prefixed by - or /. The correct prefix will be displayed in usage based on the Options property.");
				//}
				this.ExtraKeys.Add(k);
			}
		}

		/// <summary>Gets all keys (both Keys and ExtraKeys).</summary>
		public List<string> AllKeys
		{
			get
			{
				List<string> allKeys = new List<string>();
				allKeys.AddRange(this.Keys);
				allKeys.AddRange(this.ExtraKeys);
				return allKeys;
			}
		}

		public string DefaultKey
		{
			get
			{
				if (_defaultKey != null && _defaultKey.Length > 0) {
					return _defaultKey;
				} else if (Keys.Count > 0) {
					return Keys[0];
				} else {
					//throw new InvalidOperationException("At least one key is required.");
					// Nope! ... think UnnamedItem's
					return "";
				}
			}
			set { _defaultKey = (value != null) ? value.Trim() : ""; }
		}
		private string _defaultKey = "";

		/// <summary>Gets the key that was found on the command-line.</summary>
		public string KeyFound { get; set; }

		public DisplayMode DisplayMode { get { return _displayMode; } set { _displayMode = value; } }
		private DisplayMode _displayMode = DisplayMode.Always;

		public CommandLineArgumentOptions Options { get { return _options; } set { _options = value; } }
		private CommandLineArgumentOptions _options = CommandLineArgumentOptions.NameValueOptional;

		public Type InteractiveClass { get { return _interactiveClass; } set { _interactiveClass = value; } }
		private Type _interactiveClass = null;

		/// <summary>
		/// The name of the value 'cmd'.
		/// usage: blah
		///   -arg 'cmd'
		/// </summary>
		public string ExpressionLabel { get { return _expressionLabel ?? (_expressionLabel = string.Empty); } set { _expressionLabel = value ?? string.Empty; } }
		private string _expressionLabel = string.Empty;

		public string ExpressionDescription { get { return _expressionDescriptiom ?? (_expressionDescriptiom = string.Empty); } set { _expressionDescriptiom = value ?? string.Empty; } }
		private string _expressionDescriptiom= string.Empty;

		/// <summary>
		/// The allowed options for the current CommandLineArg..
		/// This property contains a collection of name and description values for each allowed option.
		/// 
		/// For example, when creating your property:
		/// 
		///		_mode.Name = "mode";
		///		_mode.Description = "Sets the mode of the application.";
		///		_mode.Keys.Add("");
		///     // If set, ExpressionsLabel will replace the 'Allowed values for '-mode' includes:' line in the usage() output.
		///     _mode.ExpressionsLabel = "";
		///     
		///		_mode.ExpressionsAllowed.Add("debug", "Displays additional debugging details while running.");
		///     _mode.ExpressionsAllowed.Add("quiet", "Prevents displaying any output except errors. Will not prompt for anything from the user.");
		///     ...
		/// 
		/// Will output the following in usage():
		/// 
		///     USAGE: 
		///       -mode 'option'   Sets the mode of the application. (flags: -mode, -m)
		///                        Allowed values for '-mode' includes: 
		///                         'debug'   Displays additional debugging details while running.
		///                         'quiet'   Prevents displaying any output except errors. Will not prompt for anything from the user.
		///                         ...
		///       
		/// </summary>
		public Dictionary<string, string> ExpressionsAllowed { get { return _expressionsAllowed ?? (_expressionsAllowed = new Dictionary<string, string>()); } set { _expressionsAllowed = value ?? new Dictionary<string, string>(); } }
		private Dictionary<string, string> _expressionsAllowed = new Dictionary<string, string>();

		/// <summary>Gets or sets whether the current command line argument is enabled.</summary>
		public bool Enabled { get { return _enabled; } set { _enabled = value; } }
		private bool _enabled = true;

		/// <summary>
		/// Gets whether the argument is present on the command-line, in an environment variable, or in a config value.
		/// <seealso cref="Exists"/> tells you whether or not the argument exists on the command-line, envar, or config,
		/// where <seealso cref="hasValue"/> tells you that it exists and has a (non-empty) value.
		/// </summary>
		public bool Exists { get { return IsArgument || IsEnvironmentVariable || IsConfigItem; } }

		/// <summary>
		/// Gets whether the option has been set.
		/// <seealso cref="Exists"/> tells you whether or not the argument exists on the command-line, envar, or config,
		/// where <seealso cref="hasValue"/> tells you that it exists and has a (non-empty) value.
		/// </summary>
		public bool HasValue { get { return _hasValue; } protected internal set { _hasValue = value; } }
		private bool _hasValue = false;

		/// <summary>Gets whether the argument was read from the command-line.</summary>
		public bool IsArgument { get { return _isArgument; } set { _isArgument = value; } }
		private bool _isArgument = false;

		public EventHandler Handler { get; set; }

		/// <summary>Gets or sets whether the value is the default value (and not found in the command-line arguments, nor in the environment variables).</summary>
		public bool IsDefault { get { return _isDefault; } set { _isDefault = value; } }
		private bool _isDefault = false;

		/// <summary>Gets or sets whether the value can be read and written to the config (settings) file.</summary>
		public bool AllowConfig { get { return _allowConfig; } set { _allowConfig = value; } }
		private bool _allowConfig = false;

		/// <summary>Gets or sets whether the value was read from config (the settings file).</summary>
		public bool IsConfigItem { get { return _isConfigItem; } set { _isConfigItem = value; } }
		private bool _isConfigItem = false;

		/// <summary>
		/// Gets or sets whether the value can be read from the environment variables.
		/// A value specified on the command-line will ALWAYS take precedence over the environment variable.
		/// </summary>
		public bool AllowEnvironmentVariable { get { return _allowEnvironmentVariable; } set { _allowEnvironmentVariable = value; } }
		private bool _allowEnvironmentVariable = true;

		/// <summary>Gets or sets whether the value was read from the environment variable.</summary>
		public bool IsEnvironmentVariable { get { return _isEnvironmentVariable; } set { _isEnvironmentVariable = value; } }
		private bool _isEnvironmentVariable = false;
	}

	[Flags]
	public enum PreCheckResult
	{
		NotSet = 0,
		Enabled = (1 << 0),
		Validate = (1 << 1),
		LoadValue = (1 << 1),
		Okay = Enabled | Validate | LoadValue
	}

	public static class PreCheckResultExtensions
	{
		public static bool enabled( this PreCheckResult presult ) { return (presult & PreCheckResult.Enabled) == PreCheckResult.Enabled; }
		public static bool validate( this PreCheckResult presult ) { return (presult & PreCheckResult.Validate) == PreCheckResult.Validate; }
		public static bool load( this PreCheckResult presult ) { return (presult & PreCheckResult.LoadValue) == PreCheckResult.LoadValue; }
	}

	public class CommandLineArg<T> : CommandLineArg
	{
		public delegate PreCheckResult PreCheckEventHandler( CommandLine CmdLine, CommandLineArg<T> arg );

#pragma warning disable 67
		/// <summary>Provides a method to prepare the command-line argument's settings based on the current runtime.</summary>
		public event PreCheckEventHandler PreCheck;
#pragma warning restore 67

		public PreCheckResult OnPreCheck( CommandLine cmdLine, CommandLineArg<T> arg )
		{
			if (PreCheck != null) {
				return PreCheck(cmdLine, arg);
			}
			return PreCheckResult.Okay;
		}

		public delegate Result ValidateEventHandler( CommandLine CmdLine, CommandLineArg<T> arg );

#pragma warning disable 67
		/// <summary>Provides a method to ensure the validity of the command-line argument.</summary>
		public event ValidateEventHandler Validate;
#pragma warning restore 67

		public Result OnValidate( CommandLine cmdLine, CommandLineArg<T> arg )
		{
			if (Validate != null) {
				return Validate(cmdLine, arg);
			}
			return new Result(Result.Okay, "");
		}

		//public delegate Result CommandLineActionEventHandler( CommandLine CmdLine, CommandLineArg<T> arg );
		//#pragma warning disable 67
		///// <summary>Provides an event that gets fired when the command-line argument is present..</summary>
		//public event CommandLineActionEventHandler CommandLineAction;
		//#pragma warning restore 67
		//public Result OnCommandLineAction( CommandLine cmdLine, CommandLineArg<T> arg )
		//{
		//	if (CommandLineAction != null) {
		//		return CommandLineAction(cmdLine, arg);
		//	}
		//	return new Result(Result.Okay, "");
		//}

		/// <summary>Gets or sets the value of this command-line argument.</summary>
		public T Value { get { return _value != null ? _value : (_value = default(T)); } set { _value = value != null ? value : default(T); } }
		private T _value = default(T);

		/// <summary>Gets or sets the default value used if the Value was not set.</summary>
		public T Default { get { return _default != null ? _default : (_default = default(T)); } set { _default = value != null ? value : default(T); } }
		private T _default = default(T);

		#region Operator Overloads

		// relational operators

		public static bool operator ==( CommandLineArg<T> obj1, CommandLineArg<T> obj2 )
		{
			// If both are null, or both are same instance, return true.
			if (System.Object.ReferenceEquals(obj1, obj2)) {
				return true;
			}

			// If one is null, but not both, return false.
			// The ^ is an exclusive-or.
			if ((object)obj1 == null ^ (object)obj2 == null) {
				return false;
			}

			return obj1.Equals(obj2);
		}

		public static bool operator !=( CommandLineArg<T> obj1, CommandLineArg<T> obj2 ) { return !(obj1 == obj2); }

		//public static bool operator <( CommandLineArg<short> val1, CommandLineArg<short> val2 ) { return val1.Value < val2.Value; }
		//public static bool operator <( CommandLineArg<int> val1, CommandLineArg<int> val2 ) { return val1.Value < val2.Value; }
		//public static bool operator <( CommandLineArg<long> val1, CommandLineArg<long> val2 ) { return val1.Value < val2.Value; }

		//public static bool operator <=( CommandLineArg<short> val1, CommandLineArg<short> val2 ) { return val1.Value <= val2.Value; }
		//public static bool operator <=( CommandLineArg<int> val1, CommandLineArg<int> val2 ) { return val1.Value <= val2.Value; }
		//public static bool operator <=( CommandLineArg<long> val1, CommandLineArg<long> val2 ) { return val1.Value <= val2.Value; }

		//public static bool operator >( CommandLineArg<short> val1, CommandLineArg<short> val2 ) { return val1.Value > val2.Value; }
		//public static bool operator >( CommandLineArg<int> val1, CommandLineArg<int> val2 ) { return val1.Value > val2.Value; }
		//public static bool operator >( CommandLineArg<long> val1, CommandLineArg<long> val2 ) { return val1.Value > val2.Value; }

		//public static bool operator >=( CommandLineArg<short> val1, CommandLineArg<short> val2 ) { return val1.Value >= val2.Value; }
		//public static bool operator >=( CommandLineArg<int> val1, CommandLineArg<int> val2 ) { return val1.Value >= val2.Value; }
		//public static bool operator >=( CommandLineArg<long> val1, CommandLineArg<long> val2 ) { return val1.Value >= val2.Value; }

		// assignment and cast operators

		//public static explicit operator CommandLineArg<T>( T Value ) { return new CommandLineArg<T>(Value); }

		//public static implicit operator string( CommandLineArg<T> value )
		//{
		//	//if (value.Value is string && typeof(T) == typeof(string)) {
		//	return Convert.ToString(value.Value);
		//	//}
		//	//throw new InvalidCastException();
		//}
		//public static implicit operator bool( CommandLineArg<T> value )
		//{
		//	if (value.Value is bool && typeof(T) == typeof(bool)) {
		//		if (value.Value != null) {
		//			return Convert.ToBoolean(value.Value);
		//		} else {
		//			return false;
		//		}
		//	}
		//	throw new InvalidCastException();
		//}
		//public static implicit operator DateTime( CommandLineArg<T> value )
		//{
		//	if (value.Value is DateTime && typeof(T) == typeof(DateTime)) {
		//		return Convert.ToDateTime(value.Value);
		//	}
		//	throw new InvalidCastException();
		//}
		//public static explicit operator Int16( CommandLineArg<T> value )
		//{
		//	if (value.Value is Int16 && typeof(T) == typeof(Int16)) {
		//		return Convert.ToInt16(value.Value);
		//	}
		//	throw new InvalidCastException();
		//}
		//public static implicit operator Int32( CommandLineArg<T> value )
		//{
		//	if (value.Value is Int32 && typeof(T) == typeof(Int32)) {
		//		return Convert.ToInt32(value.Value);
		//	}
		//	throw new InvalidCastException();
		//}
		//public static implicit operator Int64( CommandLineArg<T> value )
		//{
		//	if (value.Value is Int64 && typeof(T) == typeof(Int64)) {
		//		return Convert.ToInt64(value.Value);
		//	}
		//	throw new InvalidCastException();
		//}
		//public static implicit operator UInt16( CommandLineArg<T> value )
		//{
		//	if (value.Value is UInt16 && typeof(T) == typeof(UInt16)) {
		//		return Convert.ToUInt16(value.Value);
		//	}
		//	throw new InvalidCastException();
		//}
		//public static implicit operator UInt32( CommandLineArg<T> value )
		//{
		//	if (value.Value is UInt32 && typeof(T) == typeof(UInt32)) {
		//		return Convert.ToUInt32(value.Value);
		//	}
		//	throw new InvalidCastException();
		//}
		//public static implicit operator UInt64( CommandLineArg<T> value )
		//{
		//	if (value.Value is UInt64 && typeof(T) == typeof(UInt64)) {
		//		return Convert.ToUInt64(value.Value);
		//	}
		//	throw new InvalidCastException();
		//}
		//public static explicit operator int[]( CommandLineArg<int[]> value )
		//{
		//   if (value.Value is int[]) {
		//      return value.Value;
		//   }
		//   throw new InvalidCastException();
		//}

		#endregion

		public CommandLineArg() : this(default(T)) { }

		public CommandLineArg( T DefaultValue ) { Default = DefaultValue; }

		public override int GetHashCode() { return Value.GetHashCode(); }

		public override bool Equals( object value )
		{
			CommandLineArg<T> tmp;

			// If parameter is null return false.
			if (value == null) {
				return false;
			}

			// If parameter cannot be cast to CommandLineArg<T>, return false.
			tmp = value as CommandLineArg<T>;
			if ((System.Object)tmp == null) {
				return false;
			}

			return base.Equals(tmp);
		}

		public bool Equals( CommandLineArg<T> value )
		{
			// TODO 
			return true;
		}

		public override string ToString() { return Value.ToString(); }
	}

	/// <summary>
	/// Indicates what to expect and what is allowed for the command-line argument's values.
	/// </summary>
	public enum CommandLineArgumentOptions
	{
		///// <summary></summary>
		//NotSet = 0,
		/// <summary>There will not be any value specified. (No value allowed.)</summary>
		NameOnly = (1 << 0),
		/// <summary>There may or may not be a value specified. (A value is optional.)</summary>
		NameValueOptional = (1 << 1),
		/// <summary>There will be a value specified. (A value is required.)</summary>
		NameValueRequired = (1 << 2),
		/// <summary>There may be multiple values specified. (At least one value is required.)</summary>
		NameRemainingValues = (1 << 3),
		/// <summary>There is no name. The value is the first argument without a prefix. (The name is optional.)</summary>
		UnnamedItem = (1 << 4),
		/// <summary>There is no name. The value is the first argument without a prefix. (The name is required.)</summary>
		UnnamedItemRequired = (1 << 5),
		/// <summary></summary>
		NameOnlyInteractive = (1 << 6)
	}

	/// <summary>
	/// Indicates when (if ever) the command-line argument should be displayed in the usage content.
	/// </summary>
	public enum DisplayMode
	{
		/// <summary>Always displays the command-line argument.</summary>
		Always,
		/// <summary>Displays the command-line argument only when `help arg` is specified or the /hidden flag was specified.</summary>
		Hidden,
		/// <summary>Will never display the command-line argument, as though it doesn't even exist.</summary>
		Never
	}

	/// <summary>
	/// 
	/// </summary>
	public class Result
	{
		public const int Okay = 0;
		public const int Success = 0;
		public const int Error = 1;

		public int Code { get; set; }
		public string Message { get; set; }

		public Result SetCode( int Code )
		{
			this.Code = Code;
			return this;
		}

		public Result SetMessage( string Message )
		{
			this.Message = Message;
			return this;
		}

		public Result() : this(0, string.Empty) { }

		public Result( int Code ) : this(Code, string.Empty) { }

		public Result( int Code, string Message )
		{
			this.Code = Code;
			this.Message = Message;
		}
	}
}
