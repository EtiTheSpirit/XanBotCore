using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XanBotCore.Exceptions;

namespace XanBotCore.Utility {

	/// <summary>
	/// Adds a GetOrDefault method to Dictionary
	/// </summary>
	public static class DictionaryExtension {

		/// <summary>
		/// Attempts to get the entry for the specified key within this dictionary. Returns <paramref name="defaultValue"/> if the key has not been populated.
		/// </summary>
		/// <typeparam name="TKey">The key value type for this <see cref="IDictionary{TKey, TValue}"/></typeparam>
		/// <typeparam name="TValue">The value type corresponding to the keys in this <see cref="IDictionary{TKey, TValue}"/></typeparam>
		/// <param name="dictionary">The target dictionary.</param>
		/// <param name="key">The key to search for.</param>
		/// <param name="defaultValue">The default valye to return.</param>
		public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) {
			if (!dictionary.TryGetValue(key, out TValue retn)) 
				retn = defaultValue;
			
			return retn;
		}

		/// <summary>
		/// A variant of <see cref="GetOrDefault{TKey, TValue}(IDictionary{TKey, TValue}, TKey, TValue)"/> that is designed to work specifically for json dictionaries where the value type is Object and a specific return value is desired.
		/// </summary>
		/// <typeparam name="TKey">The key value type for this <see cref="IDictionary{TKey, TValue}"/></typeparam>
		/// <typeparam name="TReturn">The desired return type.</typeparam>
		/// <param name="dictionary">The target dictionary.</param>
		/// <param name="key">The key to search for.</param>
		/// <param name="defaultValue">The default valye to return.</param>
		public static TReturn GetOrDefaultObject<TKey, TReturn>(this IDictionary<TKey, object> dictionary, TKey key, TReturn defaultValue) {
			if (!dictionary.TryGetValue(key, out object retn)) {
				retn = defaultValue;
			}

			if (typeof(TReturn).IsPrimitive) {
				// This may require use of a Parse method.
				try {
					MethodInfo parseMethod = typeof(TReturn).GetMethod("Parse", new Type[] { typeof(string) });
					if (parseMethod != null) {
						return (TReturn)parseMethod.Invoke(null, new object[] { retn.ToString() });
					}
				} catch { }
			}
			return (TReturn)retn;
		}

		/// <summary>
		/// Attempts to do a reverse-lookup on the specified <paramref name="value"/>, returning the key that corresponds to this value.
		/// </summary>
		/// <typeparam name="TKey">The key value type for this <see cref="IDictionary{TKey, TValue}"/></typeparam>
		/// <typeparam name="TValue">The value type corresponding to the keys in this <see cref="IDictionary{TKey, TValue}"/></typeparam>
		/// <param name="dictionary">The target dictionary.</param>
		/// <param name="value">The value to find the key of.</param>
		/// <exception cref="ValueNotFoundException"/>
		/// <returns></returns>
		public static TKey KeyOf<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TValue value) {
			if (!dictionary.Values.Contains(value)) throw new ValueNotFoundException("The specified value does not exist in this dictionary.");
			foreach (TKey key in dictionary.Keys) {
				TValue v = dictionary[key];
				if (v.Equals(value)) {
					return key;
				}
			}

			throw new ValueNotFoundException("The specified value does not exist in this dictionary.");
		}
	}
}
