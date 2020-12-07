using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication
{
	/// <summary>
	///		Summary description for ApplicationSettings.
	/// </summary>
	public class ApplicationSettings
	{
		/// <summary></summary>
		[Required]
		public string? ApplicationName { get; set; }

		/// <summary></summary>
		[Required]
		public string? Environment { get; set; }

		/// <summary></summary>
		[Required]
		public ApplicationOptions? Options { get; set; }

		/// <summary></summary>
		[Required]
		public TcpSettings? Tcp { get; set; }

		/// <summary></summary>
		[Required]
		public ThreadingSettings? Threading { get; set; }
	}
}
