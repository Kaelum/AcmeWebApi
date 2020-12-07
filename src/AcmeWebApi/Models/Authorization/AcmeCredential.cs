using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace WebApplication
{
	/// <summary>
	///		Summary description for AcmeCredential.
	/// </summary>
	public class AcmeCredential : IComparable<AcmeCredential>, IComparer<AcmeCredential>
	{
		#region Constructors

		/// <summary>
		///		Creates and instance of AcmeCredential.
		/// </summary>
		/// <param name="oemId"></param>
		/// <param name="deviceId"></param>
		/// <param name="uniqueId"></param>
		public AcmeCredential(
			string? oemId,
			string? deviceId,
			string? uniqueId
		)
		{
			OemId = oemId ?? string.Empty;
			DeviceId = deviceId ?? string.Empty;
			UniqueId = uniqueId ?? string.Empty;
		}

		#endregion

		#region Public Properties

		/// <summary>Gets the device identifier.</summary>
		public string DeviceId { get; private set; }

		/// <summary>Gets the OEM identifier.</summary>
		public string OemId { get; private set; }

		/// <summary>Gets the unique identifier.</summary>
		public string UniqueId { get; private set; }

		#endregion

		#region IComparable<AcmeCredential> Implementation

		/// <summary>
		///		Compares the current instance with another object of the same type and returns an
		///		integer that indicates whether the current instance precedes, follows, or occurs in
		///		the same position in the sort order as the other object.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(AcmeCredential? other)
			=> DefaultComparer(this, other);

		#endregion

		#region IComparer<AcmeCredential> Implementation

		/// <summary>
		///		Compares two objects and returns a value indicating whether one is less than, equal
		///		to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns></returns>
		public int Compare(AcmeCredential? x, AcmeCredential? y)
			=> DefaultComparer(x, y);

		#endregion

		#region Operators

		/// <summary>
		///		Define the is greater than operator.
		/// </summary>
		/// <param name="operand1"></param>
		/// <param name="operand2"></param>
		/// <returns></returns>
		public static bool operator >(AcmeCredential? operand1, AcmeCredential? operand2)
			=> DefaultComparer(operand1, operand2) == 1;

		/// <summary>
		///		Define the is less than operator.
		/// </summary>
		/// <param name="operand1"></param>
		/// <param name="operand2"></param>
		/// <returns></returns>
		public static bool operator <(AcmeCredential? operand1, AcmeCredential? operand2)
			=> DefaultComparer(operand1, operand2) == -1;

		/// <summary>
		///		Define the is greater than or equal to operator.
		/// </summary>
		/// <param name="operand1"></param>
		/// <param name="operand2"></param>
		/// <returns></returns>
		public static bool operator >=(AcmeCredential? operand1, AcmeCredential? operand2)
			=> DefaultComparer(operand1, operand2) >= 0;

		/// <summary>
		///		Define the is less than or equal to operator.
		/// </summary>
		/// <param name="operand1"></param>
		/// <param name="operand2"></param>
		/// <returns></returns>
		public static bool operator <=(AcmeCredential? operand1, AcmeCredential? operand2)
			=> DefaultComparer(operand1, operand2) <= 0;

		#endregion

		#region DefaultComparer

		/// <summary>
		///		Compares two objects and returns a value indicating whether one is less than, equal
		///		to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns></returns>
		public static int DefaultComparer(AcmeCredential? x, AcmeCredential? y)
		{
			if (x == null & y == null)
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

			int result = string.Compare(x.OemId, y.OemId, true);

			if (result != 0) { return result; }

			result = string.Compare(x.DeviceId, y.DeviceId, true);

			if (result != 0) { return result; }

			return string.Compare(x.UniqueId, y.UniqueId, true);
		}

		#endregion
	}
}
