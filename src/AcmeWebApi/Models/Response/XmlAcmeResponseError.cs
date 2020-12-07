using System;
using System.Xml;

namespace WebApplication
{
	/// <summary>
	///		Summary description for XmlAcmeResponseError.
	/// </summary>
	public class XmlAcmeResponseError : XmlAcmeResponse
	{
		#region Constructors

		/// <summary>
		///		Create an instance of XmlAcmeResponseError.
		/// </summary>
		/// <param name="e"></param>
		public XmlAcmeResponseError(AcmeException e)
			: base(e.StatusCode)
		{
			Exception = e;
			ErrorMessage = e.Message;
		}

		/// <summary>
		///		Create an instance of XmlAcmeResponseError.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="statusCode">The status code.</param>
		public XmlAcmeResponseError(Exception? e, AcmeStatusCode statusCode)
			: base(statusCode)
		{
			Exception = e;
			ErrorMessage = e?.Message ?? statusCode.GetDescription();
		}

		/// <summary>
		///		Create an instance of XmlAcmeResponseError.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="errorMessage">The error message.</param>
		/// <param name="statusCode">The status code.</param>
		public XmlAcmeResponseError(Exception? e, string errorMessage, AcmeStatusCode statusCode)
			: base(statusCode)
		{
			Exception = e;
			ErrorMessage = errorMessage;
		}

		#endregion

		#region Non-serialized Properties

		/// <summary></summary>
		public string ErrorMessage { get; set; }

		/// <summary></summary>
		public Exception? Exception { get; set; }

		#endregion

		#region IRenderXml Implementation

		/// <summary>
		///		Render the current instance to the specified <see cref="T:XmlWriter"/>.
		/// </summary>
		/// <param name="writer">The <see cref="T:XmlWriter"/>.</param>
		public override void RenderXml(XmlWriter writer)
		{
			base.RenderStartResponse(writer);

			base.RenderEndResponse(writer);
		}

		#endregion
	}
}
