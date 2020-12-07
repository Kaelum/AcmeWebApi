using System;
using System.Collections.Generic;
using System.Threading;

namespace WebApplication
{
	/// <summary>
	///		Defines a method that implements a compare of two <see cref="T:Thread"/> objects by
	///		their <see cref="P:Thread.ManagedThreadId"/> properties.
	/// </summary>
	public class CompareThreadByManagedThreadId : IComparer<Thread>, IEqualityComparer<Thread>
	{
		/// <summary>
		///		Compares two <see cref="T:Thread"/> objects and returns a value indicating whether
		///		one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first <see cref="T:Thread"/> to compare.</param>
		/// <param name="y">The second <see cref="T:Thread"/> to compare.</param>
		/// <returns></returns>
		public int Compare(Thread? x, Thread? y)
		{
			if (x == null && y == null)
			{
				return 0;
			}
			else if (x == null)
			{
				return int.MinValue;
			}
			else if (y == null)
			{
				return int.MaxValue;
			}

			return x.ManagedThreadId.CompareTo(y.ManagedThreadId);
		}

		/// <summary>
		///		Determines whether the specified <see cref="T:Thread"/> objects are equal.
		/// </summary>
		/// <param name="x">The first <see cref="T:Thread"/> to compare.</param>
		/// <param name="y">The second <see cref="T:Thread"/> to compare.</param>
		/// <returns>
		///		<b>true</b> if the specified <see cref="T:Thread"/> objects are equal; otherwise,
		///		<b>false</b>.
		/// </returns>
		public bool Equals(Thread? x, Thread? y)
		{
			return (
				(x == null && y == null)
				|| (x != null && y != null && x.ManagedThreadId == y.ManagedThreadId)
			);
		}

		/// <summary>
		///		Returns a hash code for the specified <see cref="T:Thread"/>.
		/// </summary>
		/// <param name="obj">The <see cref="T:Thread"/> for which a hash code is to be returned.</param>
		/// <returns>
		///		A hash code for the specified <see cref="T:Thread"/>.
		/// </returns>
		public int GetHashCode(Thread obj)
		{
			return obj.GetHashCode();
		}
	}
}
