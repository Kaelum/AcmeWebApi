using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace WebApplication
{
	/// <summary>
	///		Summary description for AcmeCoreExtensions
	/// </summary>
	public static class Extensions
	{
		private const string TRUE_STRING = "true";
		private const string FALSE_STRING = "false";

		private const string ONE_STRING = "1";
		private const string ZERO_STRING = "0";


		#region Boolean ToString

		/// <summary>
		///		Returns either "true" or "false".
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>
		///		"true" when <b>true</b>, and "false" when <b>false</b>.
		/// </returns>
		public static string ToLowerString(this bool value)
		{
			return (
				value
				? TRUE_STRING
				: FALSE_STRING
			);
		}

		/// <summary>
		///		Returns either "1" or "0".
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>
		///		"1" when <b>true</b>, and "0" when <b>false</b>.
		/// </returns>
		public static string ToOneZeroString(this bool value)
		{
			return (
				value
				? ONE_STRING
				: ZERO_STRING
			);
		}

		#endregion

		#region Convert byte array to hex string

		/// <summary>
		///		Converts a byte array to a hexadecimal string.
		/// </summary>
		/// <param name="byteArray"></param>
		/// <returns></returns>
		[return: NotNullIfNotNull("byteArray")]
		public static string? ByteArrayToString(byte[]? byteArray)
		{
			if (byteArray == null)
			{
				return null;
			}

			return ByteArrayToString(byteArray, 0, byteArray.Length);
		}

		/// <summary>
		///		Converts a byte array to a hexadecimal string.
		/// </summary>
		/// <param name="byteArray"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		[return: NotNullIfNotNull("byteArray")]
		public static string? ByteArrayToString(byte[]? byteArray, int offset, int length)
		{
			if (byteArray == null)
			{
				return null;
			}

			if (offset < 0 || offset >= byteArray.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset));
			}

			if (length < 0 || offset + length > byteArray.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(length));
			}

			StringBuilder returnValue = new StringBuilder();

			for (int i = offset; i < length; i++)
			{
				byte digitPair = byteArray[i];
				returnValue.AppendFormat("{0:X2}", digitPair);
			}

			return returnValue.ToString();
		}

		/// <summary>
		///		Converts a byte array to a hexadecimal string.
		/// </summary>
		/// <param name="byteArray"></param>
		/// <returns></returns>
		[return: NotNullIfNotNull("byteArray")]
		public static string? HexEncode(this byte[] byteArray)
		{
			return ByteArrayToString(byteArray, 0, byteArray.Length);
		}

		/// <summary>
		///		Converts a byte array to a hexadecimal string.
		/// </summary>
		/// <param name="byteArray"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		[return: NotNullIfNotNull("byteArray")]
		public static string? HexEncode(this byte[] byteArray, int offset, int length)
		{
			return ByteArrayToString(byteArray, offset, length);
		}

		#endregion

		#region Convert hex string to byte array

		/// <summary>
		///		Converts a hexadecimal string to a byte array.
		/// </summary>
		/// <param name="hexString"></param>
		/// <returns></returns>
		[return: NotNullIfNotNull("hexString")]
		public static byte[]? HexDecode(this string? hexString)
		{
			return StringToByteArray(hexString);
		}

		/// <summary>
		///		Converts a hexadecimal string to a byte array.
		/// </summary>
		/// <param name="hexString"></param>
		/// <returns></returns>
		[return: NotNullIfNotNull("hexString")]
		public static byte[]? StringToByteArray(string? hexString)
		{
			if (hexString == null)
			{
				return null;
			}

			if ((hexString.Length & 1) != 0)
			{
				throw new ArgumentException("String must contain an even number of digits.", "hexString");
			}

			byte[] returnValue = new byte[hexString.Length / 2];

			for (int i = 0; i * 2 < hexString.Length; i++)
			{
				returnValue[i] = byte.Parse(hexString.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
			}

			return returnValue;
		}

		#endregion

		#region DescriptionAttribute

		/// <summary>
		///		Gets value of the <see cref="T:DescriptionAttribute"/> for the specified enumerator
		///		member.
		/// </summary>
		/// <param name="enumerator">The enumerator value.</param>
		/// <returns>
		///		The value of the <see cref="T:DescriptionAttribute"/> for the specified enumerator
		///		member if it exists; otherwise, the lowercase value of the member name.
		/// </returns>
		public static string GetDescription(this Enum enumerator)
		{
			return (
				enumerator.GetType().GetField(enumerator.ToString())?.GetCustomAttributes(typeof(DescriptionAttribute), true).SingleOrDefault() is DescriptionAttribute attribute
				? attribute.Description
				: enumerator.ToString().ToLowerInvariant()
			);
		}

		#endregion

		#region Exception GetMessages

		/// <summary>
		///		Returns the concatenated list of <see cref="P:Exception.Message"/> properties from
		///		the root exception through all of the inner exceptions.
		/// </summary>
		/// <param name="source">The exception.</param>
		/// <returns>
		///		Returns the concatenated list of <see cref="P:Exception.Message"/> properties from
		///		the root exception through all of the inner exceptions.
		/// </returns>
		public static string GetMesages(this Exception source)
		{
			Exception? e = source;
			StringBuilder messages = new StringBuilder(source.Message);

			while ((e = e.InnerException) != null)
			{
				messages.Append(" ---> ").Append(e.Message);
			}

			return messages.ToString();
		}

		#endregion

		#region GetProperty

		/// <summary>
		///		Gets the value of the specified property.
		/// </summary>
		/// <param name="obj">The object being extended.</param>
		/// <param name="propertyName">The name of the property.</param>
		/// <returns>
		///		The value of the property.
		/// </returns>
		public static object? GetProperty(this object obj, string propertyName)
		{
			return GetProperty(obj, propertyName, false, true);
		}

		/// <summary>
		///		Gets the value of the specified property.
		/// </summary>
		/// <param name="obj">The object being extended.</param>
		/// <param name="propertyName">The name of the property.</param>
		/// <param name="ignoreCase"><b>true</b> to ignore case, or <b>false</b> to regard case.</param>
		/// <returns>
		///		The value of the property.
		/// </returns>
		public static object? GetProperty(this object obj, string propertyName, bool ignoreCase)
		{
			return GetProperty(obj, propertyName, ignoreCase, true);
		}

		/// <summary>
		///		Gets the value of the specified property.
		/// </summary>
		/// <param name="obj">The object being extended.</param>
		/// <param name="propertyName">The name of the property.</param>
		/// <param name="ignoreCase"><b>true</b> to ignore case, or <b>false</b> to regard case.</param>
		/// <param name="throwIfNotFound"><b>true</b> to throw an exception if the property does not
		///		exist, or <b>false</b> to return null.</param>
		/// <returns>
		///		The value of the property.
		/// </returns>
		public static object? GetProperty(this object obj, string propertyName, bool ignoreCase, bool throwIfNotFound)
		{
			BindingFlags bindingFlags = (BindingFlags.Instance | BindingFlags.Public | (ignoreCase ? BindingFlags.IgnoreCase : 0));
			PropertyInfo? propertyInfo = obj.GetType().GetProperty(propertyName, bindingFlags);

			if (propertyInfo == null)
			{
				if (throwIfNotFound)
				{
					throw new InvalidOperationException(string.Format("The {0} type does not have public property: {1}", obj.GetType().FullName, propertyName));
				}

				return null;
			}

			return propertyInfo.GetValue(obj);
		}

		#endregion

		#region XmlEnumAttribute

		/// <summary>
		///		Gets value of the <see cref="T:XmlEnumAttribute"/> for the specified enumerator
		///		member.
		/// </summary>
		/// <param name="enumerator">The enumerator value.</param>
		/// <returns>
		///		The value of the <see cref="T:XmlEnumAttribute"/> for the specified enumerator
		///		member if it exists; otherwise, the lowercase value of the member name.
		/// </returns>
		public static string GetXmlEnum(this Enum enumerator)
		{
			return (
				enumerator.GetType().GetField(enumerator.ToString())?.GetCustomAttributes(typeof(XmlEnumAttribute), true).SingleOrDefault() is XmlEnumAttribute attribute
				? attribute.Name
				: enumerator.ToString().ToLowerInvariant()
			);
		}

		#endregion
	}
}
