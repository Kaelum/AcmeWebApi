using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication
{
	/// <summary>
	///		Summary description for ApplicationOptions.
	/// </summary>
	public class ApplicationOptions
	{
		/// <summary>Gets or sets a value indicating to enable Kestrel's connection logging.</summary>
		[Required]
		public bool EnableConnectionLogging { get; set; } = false;
	}
}