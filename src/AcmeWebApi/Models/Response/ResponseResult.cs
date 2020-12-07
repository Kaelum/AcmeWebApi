using System;

namespace WebApplication
{
	/// <summary>
	///		Summary description for ResponseResult.
	/// </summary>
	[Serializable]
	public class ResponseResult<TXmlResponse>
		where TXmlResponse : XmlAcmeResponse
	{
		#region Constructors

		/// <summary>
		///		Create and instance of ResponseResult.
		/// </summary>
		/// <param name="authStatusCode"></param>
		/// <param name="loggingInfo"></param>
		public ResponseResult(
			AcmeStatusCode authStatusCode,
			LoggingInfo loggingInfo
		)
		{
			AuthStatusCode = authStatusCode;
			Logging = loggingInfo;

			if (authStatusCode != AcmeStatusCode.OK)
			{
				ErrorResponse = new XmlAcmeResponseError(
					null,
					authStatusCode
				);
			}
		}

		/// <summary>
		///		Create and instance of ResponseResult.
		/// </summary>
		/// <param name="response"></param>
		/// <param name="loggingInfo"></param>
		public ResponseResult(
			TXmlResponse response,
			LoggingInfo loggingInfo
		)
		{
			AuthStatusCode = AcmeStatusCode.OK;
			Response = response;
			Logging = loggingInfo;
		}

		/// <summary>
		///		Create and instance of ResponseResult.
		/// </summary>
		/// <param name="errorResponse"></param>
		/// <param name="loggingInfo"></param>
		public ResponseResult(
			XmlAcmeResponseError? errorResponse,
			LoggingInfo loggingInfo
		)
		{
			AuthStatusCode = AcmeStatusCode.OK;
			ErrorResponse = errorResponse;
			Logging = loggingInfo;
		}

		#endregion

		/// <summary></summary>
		public AcmeStatusCode AuthStatusCode { get; set; }

		/// <summary></summary>
		public XmlAcmeResponseError? ErrorResponse { get; set; }

		/// <summary></summary>
		public LoggingInfo Logging { get; set; }

		/// <summary></summary>
		public TXmlResponse? Response { get; set; }
	}
}
