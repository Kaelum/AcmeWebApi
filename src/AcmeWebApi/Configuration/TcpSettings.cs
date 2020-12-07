using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication
{
	/// <summary>
	///		Summary description for TcpSettings.
	/// </summary>
	public class TcpSettings
	{
		/// <summary>Gets or sets a semicolon separated list of TCP endpoints. (Default: "tcp-stream")</summary>
		[Required]
		public string KestrelTcpEndpoints { get; set; } = "tcp-stream";

		/// <summary>Gets or sets the TCP timeout in seconds. (Default: 60)</summary>
		[Required]
		public int Timeout { get; set; } = 60;
	}
}
