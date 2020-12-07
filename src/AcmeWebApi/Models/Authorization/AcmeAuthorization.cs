using System;
using System.Collections.Generic;

namespace WebApplication
{
	/// <summary>
	///		Summary description for AcmeAuthorization.
	/// </summary>
	[Serializable]
	public class AcmeAuthorization : IComparable<AcmeAuthorization>, IComparer<AcmeAuthorization>
	{
		private readonly SortedSet<AcmeCredential> _authorizedCredentials = new SortedSet<AcmeCredential>()
	{
		new AcmeCredential("oem", "device", "001"),
		new AcmeCredential("oem", "device", "002"),
		//new AcmeCredential("", "", ""),
	};

		#region Constructors

		/// <summary>
		///		Create an instance of AcmeAuthorization.
		/// </summary>
		/// <param name="oemId">The OEM identifier.</param>
		/// <param name="deviceId">The device identifier.</param>
		/// <param name="uniqueId">The unique identifier.</param>
		public AcmeAuthorization(
			string? oemId,
			string? deviceId,
			string? uniqueId
		)
		{
			Credential = new AcmeCredential(oemId, deviceId, uniqueId);

			if (Credential.OemId == string.Empty)
			{
				StatusCode = AcmeStatusCode.InvalidOemId;
			}
			else if (Credential.DeviceId == string.Empty)
			{
				StatusCode = AcmeStatusCode.InvalidDeviceId;
			}
			else if (Credential.UniqueId == string.Empty)
			{
				StatusCode = AcmeStatusCode.Unauthorized;
			}
			else
			{
				StatusCode = AcmeStatusCode.Undefined;
			}

			if (StatusCode != AcmeStatusCode.Undefined)
			{
				AuthorizationFailed = true;
				return;
			}

			AuthorizationFailed = !IsAuthorized();
		}

		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating that a failure occurred during authorization.</summary>
		public bool AuthorizationFailed { get; private set; }

		/// <summary>Gets the credential.</summary>
		public AcmeCredential Credential { get; private set; }

		/// <summary>Gets the ACME status code.</summary>
		public AcmeStatusCode StatusCode { get; private set; }

		#endregion

		#region IComparable<AcmeAuthorization> Implementation

		/// <summary>
		///		Compares the current instance with another object of the same type and returns an
		///		integer that indicates whether the current instance precedes, follows, or occurs in
		///		the same position in the sort order as the other object.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(AcmeAuthorization? other)
			=> DefaultComparer(this, other);

		#endregion

		#region IComparer<AcmeAuthorization> Implementation

		/// <summary>
		///		Compares two objects and returns a value indicating whether one is less than, equal
		///		to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns></returns>
		public int Compare(AcmeAuthorization? x, AcmeAuthorization? y)
			=> DefaultComparer(x, y);

		#endregion

		#region Operators

		/// <summary>
		///		Define the is greater than operator.
		/// </summary>
		/// <param name="operand1"></param>
		/// <param name="operand2"></param>
		/// <returns></returns>
		public static bool operator >(AcmeAuthorization? operand1, AcmeAuthorization? operand2)
			=> DefaultComparer(operand1, operand2) == 1;

		/// <summary>
		///		Define the is less than operator.
		/// </summary>
		/// <param name="operand1"></param>
		/// <param name="operand2"></param>
		/// <returns></returns>
		public static bool operator <(AcmeAuthorization? operand1, AcmeAuthorization? operand2)
			=> DefaultComparer(operand1, operand2) == -1;

		/// <summary>
		///		Define the is greater than or equal to operator.
		/// </summary>
		/// <param name="operand1"></param>
		/// <param name="operand2"></param>
		/// <returns></returns>
		public static bool operator >=(AcmeAuthorization? operand1, AcmeAuthorization? operand2)
			=> DefaultComparer(operand1, operand2) >= 0;

		/// <summary>
		///		Define the is less than or equal to operator.
		/// </summary>
		/// <param name="operand1"></param>
		/// <param name="operand2"></param>
		/// <returns></returns>
		public static bool operator <=(AcmeAuthorization? operand1, AcmeAuthorization? operand2)
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
		public static int DefaultComparer(AcmeAuthorization? x, AcmeAuthorization? y)
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

			return AcmeCredential.DefaultComparer(x.Credential, y.Credential);
		}

		#endregion

		#region Private Methods

		/// <summary>
		///		Validate the authorization objects.
		/// </summary>
		/// <returns>
		///		<b>true</b> if the authorized; otherwise, <b>false</b>.
		/// </returns>
		private bool IsAuthorized()
		{
			if (AuthorizationFailed)
			{
				StatusCode = AcmeStatusCode.Unauthorized;
				return false;
			}

			if (_authorizedCredentials.Contains(Credential))
			{
				StatusCode = AcmeStatusCode.OK;
				return true;
			}

			StatusCode = AcmeStatusCode.Unauthorized;
			return false;
		}

		#endregion
	}
}
