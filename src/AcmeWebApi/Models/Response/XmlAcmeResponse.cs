using System;
using System.Xml;

namespace WebApplication
{
	/// <summary>
	///		Summary description for XmlAcmeResponse.
	/// </summary>
	[Serializable]
	public abstract class XmlAcmeResponse : IRenderXml
	{
		#region Constructors

		/// <summary>
		///		Create an instance of XmlAcmeResponse.
		/// </summary>
		/// <param name="statusCode"></param>
		public XmlAcmeResponse(AcmeStatusCode statusCode = AcmeStatusCode.OK)
		{
			StatusCode = statusCode;
		}

		#endregion

		#region Non-serialized Properties

		/// <summary>Gets or sets the status code.  Serialized by the <see cref="P:Status"/> property.</summary>
		public AcmeStatusCode StatusCode { get; set; }

		#endregion

		#region Serialized Properties

		/// <summary></summary>
		public int Status
		{
			get { return (int)StatusCode; }
			set { StatusCode = (AcmeStatusCode)value; }
		}

		/// <summary></summary>
		public string StatusMessage
		{
			get { return StatusCode.GetDescription(); }
			set { }
		}

		#endregion

		#region Public Methods

		/// <summary>
		///		RenderEndResponse
		/// </summary>
		/// <param name="writer"></param>
		protected void RenderEndResponse(XmlWriter writer)
		{
			writer.WriteEndElement(); // response
		}

		/// <summary>
		///		RenderStartResponse
		/// </summary>
		/// <param name="writer"></param>
		protected void RenderStartResponse(XmlWriter writer)
		{
			writer.WriteStartElement(null, "response", null);

			writer.WriteElementString("status", Status.ToString());
			writer.WriteElementString("statusmsg", StatusMessage);
		}

		#endregion

		#region Public Abstract Methods

		/// <summary>
		///		Render the current instance to the specified <see cref="T:XmlWriter"/>.
		/// </summary>
		/// <param name="writer">The <see cref="T:XmlWriter"/>.</param>
		public abstract void RenderXml(XmlWriter writer);

		#endregion
	}
}
