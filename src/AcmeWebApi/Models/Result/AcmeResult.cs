using System;
using System.Net;
using System.Net.Mime;

namespace WebApplication
{
	/// <summary>
	///		Summary description for AcmeResult.
	/// </summary>
	public class AcmeResult : HttpResult
	{
		#region Constructors

		/// <summary>
		///		Create an instance of AcmeResult.
		/// </summary>
		/// <param name="acmeRequest"></param>
		/// <param name="response"></param>
		public AcmeResult(
			AcmeRequest acmeRequest,
			byte[] response
		) : base(
			HttpStatusCode.OK,
			MediaTypeNames.Application.Xml,
			response
		)
		{
			SeqNum = acmeRequest.SeqNum;
			Authorization = acmeRequest.Authorization;
			RawMethod = acmeRequest.RawMethod;
			AcmeStatusCode = AcmeStatusCode.OK;
		}

		/// <summary>
		///		Create an instance of AcmeResult.
		/// </summary>
		/// <param name="acmeRequest"></param>
		/// <param name="acmeResponse"></param>
		public AcmeResult(
			AcmeRequest acmeRequest,
			XmlAcmeResponse acmeResponse
		) : base(
			acmeRequest.SeqNum,
			(acmeResponse.StatusCode == AcmeStatusCode.InternalServerError ? HttpStatusCode.InternalServerError : HttpStatusCode.OK),
			acmeResponse
		)
		{
			SeqNum = acmeRequest.SeqNum;
			Authorization = acmeRequest.Authorization;
			RawMethod = acmeRequest.RawMethod;
			AcmeStatusCode = acmeResponse.StatusCode;
		}

		/// <summary>
		///		Create an instance of AcmeResult.
		/// </summary>
		/// <param name="seqNum"></param>
		/// <param name="authorization"></param>
		/// <param name="rawMethod"></param>
		/// <param name="acmeResponse"></param>
		protected AcmeResult(
			int? seqNum,
			AcmeAuthorization? authorization,
			string? rawMethod,
			XmlAcmeResponse acmeResponse
		) : base(
			seqNum,
			(acmeResponse.StatusCode == AcmeStatusCode.InternalServerError ? HttpStatusCode.InternalServerError : HttpStatusCode.OK),
			acmeResponse
		)
		{
			SeqNum = seqNum;
			Authorization = authorization;
			RawMethod = rawMethod;
			AcmeStatusCode = acmeResponse.StatusCode;
		}

		#endregion

		/// <summary>Gets or sets the authorization.</summary>
		public AcmeAuthorization? Authorization { get; private set; }

		/// <summary>Gets or sets the ACME status code.</summary>
		public AcmeStatusCode AcmeStatusCode { get; private set; }

		/// <summary>Gets or sets the raw method.</summary>
		public string? RawMethod { get; private set; }

		/// <summary>Gets or sets the sequence number.</summary>
		public int? SeqNum { get; private set; }

		#region Static Create

		/// <summary>
		///		Create an instance of <see cref="T:AcmeResult"/>.  If
		/// </summary>
		/// <param name="applicationOptions"></param>
		/// <param name="acmeRequest"></param>
		/// <param name="responseResult"></param>
		/// <returns></returns>
		public static AcmeResult Create<TXmlResponse>(
			ApplicationOptions applicationOptions,
			AcmeRequest acmeRequest,
			ResponseResult<TXmlResponse> responseResult
		)
			where TXmlResponse : XmlAcmeResponse
		{
			if (responseResult.ErrorResponse != null)
			{
				return new ErrorResult(
					acmeRequest.SeqNum,
					acmeRequest.Authorization,
					acmeRequest.RawMethod,
					responseResult.ErrorResponse
				);
			}

			if (responseResult.Response == null)
			{
				return new ErrorResult(
					acmeRequest.SeqNum,
					acmeRequest.Authorization,
					acmeRequest.RawMethod,
					new XmlAcmeResponseError(
						null,
						"Response is null.",
						AcmeStatusCode.InvalidStateCondition
					)
				);
			}

			if (responseResult.Response.StatusCode == AcmeStatusCode.OK)
			{
				return new AcmeResult(
					acmeRequest,
					responseResult.Response
				);
			}

			return new ErrorResult(
				acmeRequest.SeqNum,
				acmeRequest.Authorization,
				acmeRequest.RawMethod,
				new XmlAcmeResponseError(
					null,
					responseResult.Response.StatusMessage,
					responseResult.Response.StatusCode
				)
			);
		}

		#endregion
	}
}
