using System;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Xml;

namespace WebApplication
{
	/// <summary>
	///		Summary description for HttpResult.
	/// </summary>
	public class HttpResult : ProcessResult
	{
		#region Constructors

		/// <summary>
		///		Create an instance of HttpResult.
		/// </summary>
		/// <param name="httpStatusCode"></param>
		/// <param name="contentType"></param>
		/// <param name="response"></param>
		public HttpResult(
			HttpStatusCode httpStatusCode,
			string contentType,
			byte[] response
		) : base(true)
		{
			HttpStatusCode = httpStatusCode;
			ContentType = contentType;
			Response = response;
		}

		/// <summary>
		///		Create an instance of HttpResult.
		/// </summary>
		/// <param name="seqNum"></param>
		/// <param name="httpStatusCode"></param>
		/// <param name="acmeResponse"></param>
		protected HttpResult(
			int? seqNum,
			HttpStatusCode httpStatusCode,
			XmlAcmeResponse acmeResponse
		) : base(true)
		{
			HttpStatusCode = httpStatusCode;
			ContentType = MediaTypeNames.Application.Xml;
			Response = SerializeResponse(
				seqNum,
				acmeResponse
			);
		}

		#endregion

		/// <summary>Gets or sets the HTTP Content-Type.</summary>
		public string ContentType { get; private set; }

		/// <summary>Gets or sets the HTTP status code.</summary>
		public HttpStatusCode HttpStatusCode { get; private set; }

		/// <summary>Gets or sets the response.</summary>
		public byte[] Response { get; private set; }

		#region Protected Methods

		/// <summary>
		///		Serialize the specified response.
		/// </summary>
		/// <param name="seqNum"></param>
		/// <param name="acmeResponse"></param>
		/// <returns></returns>
		protected byte[] SerializeResponse(
			int? seqNum,
			IRenderXml acmeResponse
		)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			using (XmlWriter xmlWriter = AcmeSerialization.CreateAcmeXmlWriter(memoryStream))
			{
				AcmeSerialization.InitializeAcmeXmlWriter(xmlWriter, seqNum);
				AcmeSerialization.SerializeAcmeResponse(xmlWriter, acmeResponse);
				AcmeSerialization.FinishAcmeXmlWriter(xmlWriter);

				memoryStream.Seek(0L, SeekOrigin.Begin);

				return memoryStream.ToArray();
			}
		}

		#endregion
	}
}
