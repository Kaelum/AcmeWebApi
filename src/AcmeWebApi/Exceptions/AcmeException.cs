using System;

namespace WebApplication
{
	/// <summary>
	///		Base class for all ACME custom exceptions.
	/// </summary>
	public class AcmeException : Exception
	{
		#region Constructors

		/// <summary>
		///		Create an instance of AcmeException.
		/// </summary>
		/// <param name="seqNum"></param>
		/// <param name="statusCode"></param>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public AcmeException(
			int? seqNum,
			AcmeStatusCode statusCode,
			string? message = null,
			Exception? innerException = null
		) : this(
			seqNum,
			null,
			null,
			statusCode,
			message,
			innerException
		)
		{ }

		/// <summary>
		///		Create an instance of AcmeException.
		/// </summary>
		/// <param name="seqNum"></param>
		/// <param name="authorization"></param>
		/// <param name="rawMethod"></param>
		/// <param name="statusCode"></param>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public AcmeException(
			int? seqNum,
			AcmeAuthorization? authorization,
			string? rawMethod,
			AcmeStatusCode statusCode,
			string? message = null,
			Exception? innerException = null
		) : base(
			message,
			innerException
		)
		{
			SeqNum = seqNum;
			Authorization = authorization;
			RawMethod = rawMethod;
			StatusCode = statusCode;
		}

		#endregion

		#region Public Properties

		/// <summary></summary>
		public AcmeAuthorization? Authorization { get; set; }

		/// <summary></summary>
		public string? RawMethod { get; set; }

		/// <summary></summary>
		public int? SeqNum { get; set; }

		/// <summary></summary>
		public AcmeStatusCode StatusCode { get; set; }

		#endregion
	}
}
