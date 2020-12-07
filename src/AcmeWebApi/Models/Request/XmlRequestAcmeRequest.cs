using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace WebApplication
{
	/// <summary>
	///		Summary description for XmlRequestAcmeRequest.
	/// </summary>
	[XmlRoot("request")]
	[Serializable]
	public class XmlRequestAcmeRequest
	{
		/// <summary>
		///		Gets a static instance of an <see cref="T:XmlSerializer"/> for
		///		<see cref="T:XmlRequestAcmeRequest"/>.
		///	</summary>
		public static readonly XmlSerializer XmlSerializer = new XmlSerializer(typeof(XmlRequestAcmeRequest));


		#region Serialized Properties

		/// <summary>Gets or sets the array of keys.</summary>
		[XmlElement("key")]
		public string[]? Keys { get; set; }

		/// <summary>Gets or sets the maximum number of items to return.</summary>
		[XmlElement("limit")]
		public string? Limit { get; set; }

		/// <summary>Gets or sets the operation method.</summary>
		[Required]
		[XmlElement("method")]
		public string? Method { get; set; }

		/// <summary>Gets or sets the OEM identifier.</summary>
		[Required]
		[XmlElement("oemid")]
		public string? OemId { get; set; }

		/// <summary>Gets for sets the product (a.k.a. device) identifier.</summary>
		[Required]
		[XmlElement("productid")]
		public string? ProductId { get; set; }

		/// <summary>Gets or sets the array of URIs.</summary>
		[XmlElement("uri")]
		public string? Uri { get; set; }

		/// <summary>Gets or sets the unique identifier.</summary>
		[Required]
		[XmlElement("uid")]
		public string? UniqueId { get; set; }

		#endregion
	}
}
