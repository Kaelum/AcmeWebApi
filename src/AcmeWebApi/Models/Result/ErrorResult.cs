using System;

namespace WebApplication
{
	/// <summary>
	///		Summary description for ErrorResult.
	/// </summary>
	public class ErrorResult : AcmeResult
	{
		#region Constructors

		/// <summary>
		///		Create an instance of ErrorResult for error results only.
		/// </summary>
		/// <param name="seqNum"></param>
		/// <param name="authorization"></param>
		/// <param name="rawMethod"></param>
		/// <param name="errorResponse"></param>
		public ErrorResult(
			int? seqNum,
			AcmeAuthorization? authorization,
			string? rawMethod,
			XmlAcmeResponseError errorResponse
		) : base(
			seqNum,
			authorization,
			rawMethod,
			errorResponse
		)
		{
			if (
				(Authorization == null || Authorization.StatusCode == AcmeStatusCode.OK)
				&& AcmeStatusCode == AcmeStatusCode.OK
			)
			{
				throw new AcmeException(
					seqNum,
					AcmeStatusCode.InternalServerError,
					$"{nameof(ErrorResult)} must represent a non-{AcmeStatusCode.OK} status."
				);
			}

			Exception = errorResponse.Exception;
			ErrorMessage = errorResponse.ErrorMessage;
		}

		#endregion

		/// <summary>Gets or sets the exception, if one exists.</summary>
		public Exception? Exception { get; set; }

		/// <summary>Gets or sets the error message when <see cref="P:StatusCode"/> is not OK.</summary>
		public string ErrorMessage { get; set; }
	}
}
