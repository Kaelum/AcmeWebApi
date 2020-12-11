using System;
using System.Net;

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

		#endregion

		/// <summary>Gets or sets the HTTP Content-Type.</summary>
		public string ContentType { get; private set; }

		/// <summary>Gets or sets the HTTP status code.</summary>
		public HttpStatusCode HttpStatusCode { get; private set; }

		/// <summary>Gets or sets the response.</summary>
		public byte[] Response { get; private set; }
	}
}
