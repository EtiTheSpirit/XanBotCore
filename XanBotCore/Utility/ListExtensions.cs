using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XanBotCore.Utility {

	/// <summary>
	/// Offers an extension to <see cref="IEnumerable{T}"/> that sharply enhances the speed of the Skip method.
	/// </summary>
	public static class ListExtensions {
		/// <summary>
		/// Returns true if the contents of this <see cref="IEnumerable{T}"/> are identical to the contents of <paramref name="other"/> via using the Contains method. Consider using SequenceEquals instead if you do not need to test via Contains and can instead test via the objects' default equality methods.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static bool ContentEquals<T>(this IEnumerable<T> source, IEnumerable<T> other) {
			if (other == null) throw new ArgumentNullException("other");
			if (source.Count() != other.Count()) return false;

			using (IEnumerator<T> srcEnumerator = source.GetEnumerator()) {
				for (int i = 0; i < source.Count(); i++) {
					srcEnumerator.MoveNext();
					if (!other.Contains(srcEnumerator.Current)) return false;
				}
			}
			
			return true;
		}

		/// <summary>
		/// Returns whether or not the two specified <see cref="IEnumerable{T}"/> instances have identical contents even if they are not necessarily the same reference.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static bool IsEqualTo<T>(this IEnumerable<T> source, IEnumerable<T> other) {
			if (other == null) throw new ArgumentNullException("other");
			if (ReferenceEquals(source, other)) return true;
			if (source.Count() != other.Count()) return false;

			T[] sourceArray = source.ToArray();
			T[] otherArray = other.ToArray();

			for (int idx = 0; idx < sourceArray.Length; idx++) {
				if (!sourceArray[idx].Equals(otherArray[idx])) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns a random entry from the specified <see cref="IEnumerable{T}"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <returns></returns>
		public static T RandomIn<T>(this IEnumerable<T> source) {
			return source.ElementAt(RNG.Next(source.Count()));
		}
		private static readonly Random RNG = new Random();
	}
}
