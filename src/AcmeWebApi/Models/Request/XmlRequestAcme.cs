using System;
using System.Xml;
using System.Xml.Serialization;

namespace WebApplication
{
	/// <summary>
	///		Summary description for XmlRequestAcme.
	/// </summary>
	[XmlRoot("acme")]
	[Serializable]
	public class XmlRequestAcme
	{
		/// <summary>
		///		Gets a static instance of <see cref="T:XmlSerializer"/> for
		///		<see cref="T:XmlRequestAcme"/>.
		///	</summary>
		public static readonly XmlSerializer XmlSerializer = new XmlSerializer(typeof(XmlRequestAcme));


		#region Elements

		/// <summary></summary>
		[XmlElement("encrypt-type")]
		public string? EncryptType { get; set; }

		/// <summary></summary>
		[XmlElement("request")]
		public XmlRequestAcmeRequest? Request { get; set; }

		/// <summary></summary>
		[XmlElement("seqnum")]
		public string? SeqNum { get; set; }

		#endregion

		#region Out-of-band Element

		/// <summary></summary>
		[XmlText]
		public string? OutOfBandString { get; set; }

		#endregion
	}
}
