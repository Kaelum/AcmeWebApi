using System;

namespace WebApplication
{
	/// <summary>
	///		Summary description for ProcessResult.
	/// </summary>
	public class ProcessResult
	{
		#region Constructors

		/// <summary>
		///		Create an instance of ProcessResult.
		/// </summary>
		/// <param name="processed"><b>true</b> if the request was processed, or <b>false</b> if not.</param>
		public ProcessResult(bool processed)
		{
			Processed = processed;
		}

		#endregion

		/// <summary>Gets a value that indicates whether the request was processed.</summary>
		public bool Processed { get; private set; }
	}
}
