using System;
using System.Xml;

namespace WebApplication
{
	/// <summary>
	///		Summary description for XmlAcmeResponseAuth.
	/// </summary>
	[Serializable]
	public class XmlAcmeResponseAuth : XmlAcmeResponse
	{
		#region Constructors

		/// <summary>
		///		Create an instance of XmlAcmeResponseAuth.
		/// </summary>
		/// <param name="statusCode"></param>
		/// <param name="expireDate"></param>
		public XmlAcmeResponseAuth(
			AcmeStatusCode statusCode,
			string expireDate
		)
			: base(statusCode)
		{
			ExpireDate = expireDate;
		}

		#endregion

		#region Serialized Properties

		/// <summary></summary>
		public string ExpireDate { get; set; }

		#endregion

		#region IRenderXml Implementation

		/// <summary>
		///		Render the current instance to the specified <see cref="T:XmlWriter"/>.
		/// </summary>
		/// <param name="writer">The <see cref="T:XmlWriter"/>.</param>
		public override void RenderXml(XmlWriter writer)
		{
			base.RenderStartResponse(writer);

			writer.WriteElementString("expiredate", ExpireDate);

			base.RenderEndResponse(writer);
		}

		#endregion
	}
}
