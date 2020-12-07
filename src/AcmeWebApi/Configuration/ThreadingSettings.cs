using System;

namespace WebApplication
{
	/// <summary>
	///		Summary description for ThreadingSettings.
	/// </summary>
	public class ThreadingSettings
	{
		/// <summary></summary>
		public int MaxCompletionPortThreads { get; set; } = 32768;

		/// <summary></summary>
		public int MaxWorkerThreads { get; set; } = 32768;

		/// <summary></summary>
		public int MinCompletionPortThreads { get; set; } = 200;

		/// <summary></summary>
		public int MinWorkerThreads { get; set; } = 200;
	}
}
