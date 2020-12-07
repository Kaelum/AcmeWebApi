using System;

namespace WebApplication
{
	/// <summary>
	///		Summary description for LoggingInfo.
	/// </summary>
	public class LoggingInfo
	{
		/// <summary></summary>
		public int? Bcri { get; set; }

		/// <summary>Gets or sets and array of categories.</summary>
		public int[]? Cats { get; set; }

		/// <summary>Gets or sets and array of the items to log. (Optional)</summary>
		public LoggingInfo[]? Items { get; set; }

		/// <summary>Gets or sets the elapsed time in milliseconds.</summary>
		public long ElapsedMilliseconds { get; set; }

		/// <summary></summary>
		public string? Key { get; set; }
	}
}
