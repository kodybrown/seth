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

namespace Bricksoft.PowerCode
{
	/// <summary>
	/// Provides an easier way to get environment variables.
	/// </summary>
	public class EnvironmentVariables
	{
		private char[] disallowed = new char[] { '`', '~', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '+', '=', '{', '[', '}', '}', '|', '\\', ':', ';', '"', '\'', '<', '>', ',', '.', '?', '/' };

		public string Prefix
		{
			get { return _prefix + "_"; }
			set
			{
				if (value.IndexOfAny(disallowed) > -1) {
					throw new ArgumentException("Only alphanumeric characters and underscores (_) are allowed for environment variable names.");
				}
				_prefix = value != null && value.Trim().Length > 0 ? value.Trim() : "";
				if (_prefix.EndsWith("_")) {
					_prefix = _prefix.Substring(0, _prefix.Length - 1);
				}
			}
		}
		private string _prefix = "";

		public string Postfix
		{
			get { return _postfix + "_"; }
			set
			{
				if (value.IndexOfAny(disallowed) > -1) {
					throw new ArgumentException("Only alphanumeric characters and underscores (_) are allowed for environment variable names.");
				}
				_postfix = value != null && value.Trim().Length > 0 ? value.Trim() : "";
				if (_postfix.EndsWith("_")) {
					_postfix = _postfix.Substring(0, _postfix.Length - 1);
				}
			}
		}
		private string _postfix = "";

		public EnvironmentVariableTarget Target { get { return _target; } set { _target = value; } }
		private EnvironmentVariableTarget _target;


		public EnvironmentVariables()
		{
			this.Prefix = "_";
			this.Postfix = "";
			this.Target = EnvironmentVariableTarget.Process;
		}

		public EnvironmentVariables( string Prefix, string Postfix = "", EnvironmentVariableTarget Target = EnvironmentVariableTarget.Process )
		{
			this.Prefix = Prefix;
			this.Postfix = Postfix;
			this.Target = Target;
		}


		/// <summary>
		/// Returns a dictionary of all environment variables that begin with the current instance's prefix (and ends with this instance's postfix, if specified).
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// If prefix (and postfix) is empty, ALL environment variables are returned.
		/// </remarks>
		public Dictionary<string, string> GetAll( EnvironmentVariableTarget Target = EnvironmentVariableTarget.Process )
		{
			Dictionary<string, string> l = new Dictionary<string, string>();

			foreach (KeyValuePair<string, string> p in Environment.GetEnvironmentVariables(Target)) {
				if ((Prefix.Length == 0 && Postfix.Length == 0)
						|| (p.Key.StartsWith(Prefix, StringComparison.CurrentCultureIgnoreCase)
						 && p.Key.EndsWith(Postfix, StringComparison.CurrentCultureIgnoreCase))) {
					l.Add(p.Key, p.Value);
				}
			}

			return l;
		}

		/// <summary>
		/// Returns a list of all environment variable names that begin with prefix (and end with postfix, if specified).
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// If prefix (and postfix) is empty, ALL environment variable names are returned.
		/// </remarks>
		public List<string> GetKeys( EnvironmentVariableTarget Target = EnvironmentVariableTarget.Process )
		{
			List<string> l = new List<string>();

			foreach (KeyValuePair<string, string> p in Environment.GetEnvironmentVariables(Target)) {
				//if (prefix.Length == 0 || p.Key.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase)) {
				if ((Prefix.Length == 0 && Postfix.Length == 0)
						|| (p.Key.StartsWith(Prefix, StringComparison.CurrentCultureIgnoreCase)
							&& p.Key.EndsWith(Postfix, StringComparison.CurrentCultureIgnoreCase))) {
					l.Add(p.Key);
				}
			}

			return l;
		}

		public static bool ExistsGlobally( string Key ) { return ContainsGlobally(Key); }

		public static bool ContainsGlobally( string Key, EnvironmentVariableTarget Target = EnvironmentVariableTarget.Process )
		{
			if (Environment.GetEnvironmentVariable(Key, Target) != null) {
				return true;
			}
			return false;
		}

		public bool Exists( string Key ) { return Contains(Key); }

		/// <summary>
		/// Returns whether the specified environment variable exists.
		/// The key is automatically prefixed by this instance's prefix property.
		/// The target (scope) is specified by <paramref name="Target"/>.
		/// </summary>
		/// <param name="Key"></param>
		/// <param name="Target"></param>
		/// <returns></returns>
		public bool Contains( string Key, EnvironmentVariableTarget Target = EnvironmentVariableTarget.Process )
		{
			if (Environment.GetEnvironmentVariable(Prefix + Key + Postfix, Target) != null) {
				return true;
			}
			return false;
		}


		/// <summary>
		/// Returns the index of the first environment variable that exists.
		/// The target (scope) is the current process.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public int IndexOfAny( params string[] Keys )
		{
			return IndexOfAny(EnvironmentVariableTarget.Process, Keys);
		}

		/// <summary>
		/// Returns the index of the first environment variable that exists.
		/// The target (scope) is specified by <paramref name="Target"/>.
		/// </summary>
		/// <param name="Target"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public int IndexOfAny( EnvironmentVariableTarget Target, params string[] Keys )
		{
			for (int i = 0; i < Keys.Length; i++) {
				if (Environment.GetEnvironmentVariable(Prefix + Keys[i] + Postfix, Target) != null) {
					return i;
				}
			}
			return -1;
		}


		/// <summary>
		/// Gets the value of <paramref name="key"/> from the environment variables.
		/// The prefix and postfix values are applied.
		/// Returns it as type T.
		/// The target (scope) is specified by <paramref name="target"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <param name="separator"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public T attr<T>( string key, T defaultValue = default(T), string separator = "||", EnvironmentVariableTarget target = EnvironmentVariableTarget.Process )
		{
			if (key == null || key.Length == 0) {
				throw new InvalidOperationException("key is required");
			}

			if (Environment.GetEnvironmentVariable(Prefix + key + Postfix, target) != null) {
				return GetAttrValue<T>(Environment.GetEnvironmentVariable(Prefix + key + Postfix, target), defaultValue, separator);
			}

			return defaultValue;
		}

		/// <summary>
		/// Gets the value of <paramref name="key"/> from the environment variables.
		/// The prefix and postfix values are ignored.
		/// Returns it as type T.
		/// The target (scope) is specified by <paramref name="target"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <param name="separator"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public T global<T>( string key, T defaultValue = default(T), string separator = "||", EnvironmentVariableTarget target = EnvironmentVariableTarget.Process )
		{
			if (key == null || key.Length == 0) {
				throw new InvalidOperationException("key is required");
			}

			if (Environment.GetEnvironmentVariable(key, target) != null) {
				return GetAttrValue<T>(Environment.GetEnvironmentVariable(key, target), defaultValue, separator);
			}

			return defaultValue;
		}

		public bool GetBoolean( bool defaultValue, params string[] keys )
		{
			return global(keys[0], defaultValue);
		}

		public int GetInt32( int defaultValue, params string[] keys )
		{
			return global(keys[0], defaultValue);
		}

		public DateTime GetDateTime( DateTime defaultValue, params string[] keys )
		{
			return global(keys[0], defaultValue);
		}

		public string GetString( string defaultValue, params string[] keys )
		{
			return global(keys[0], defaultValue);
		}

		private T GetAttrValue<T>( string keydata, T defaultValue = default(T), string separator = "||" )
		{
			if (typeof(T) == typeof(bool) || typeof(T).IsSubclassOf(typeof(bool))) {
				if ((object)keydata != null) {
					return (T)(object)(keydata.StartsWith("t", StringComparison.CurrentCultureIgnoreCase));
				}
			} else if (typeof(T) == typeof(DateTime) || typeof(T).IsSubclassOf(typeof(DateTime))) {
				DateTime dt;
				if ((object)keydata != null && DateTime.TryParse(keydata, out dt)) {
					return (T)(object)dt;
				}
			} else if (typeof(T) == typeof(short) || typeof(T).IsSubclassOf(typeof(short))) {
				short i;
				if ((object)keydata != null && short.TryParse(keydata, out i)) {
					return (T)(object)i;
				}
			} else if (typeof(T) == typeof(int) || typeof(T).IsSubclassOf(typeof(int))) {
				int i;
				if ((object)keydata != null && int.TryParse(keydata, out i)) {
					return (T)(object)i;
				}
			} else if (typeof(T) == typeof(long) || typeof(T).IsSubclassOf(typeof(long))) {
				long i;
				if ((object)keydata != null && long.TryParse(keydata, out i)) {
					return (T)(object)i;
				}
			} else if (typeof(T) == typeof(ulong) || typeof(T).IsSubclassOf(typeof(ulong))) {
				ulong i;
				if ((object)keydata != null && ulong.TryParse(keydata, out i)) {
					return (T)(object)i;
				}
			} else if (typeof(T) == typeof(string) || typeof(T).IsSubclassOf(typeof(string))) {
				// string
				if ((object)keydata != null) {
					return (T)(object)(keydata).ToString();
				}
			} else if (typeof(T) == typeof(string[]) || typeof(T).IsSubclassOf(typeof(string[]))) {
				// string[]
				if ((object)keydata != null) {
					// string array data SHOULD always be saved to the environment as a string||string||string..
					return (T)(object)keydata.Split(new string[] { separator }, StringSplitOptions.None);
				}
			} else if (typeof(T) == typeof(List<string>) || typeof(T).IsSubclassOf(typeof(List<string>))) {
				// List<string>
				if ((object)keydata != null) {
					// string array data SHOULD always be saved to the environment as a string||string||string..
					return (T)(object)new List<string>(keydata.Split(new string[] { separator }, StringSplitOptions.None));
				}
			} else {
				throw new InvalidOperationException("unknown or unsupported data type was requested");
			}

			return defaultValue;
		}
	}
}
