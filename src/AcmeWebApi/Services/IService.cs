using System;

namespace WebApplication
{
	/// <summary>
	///		Summary description for IService.
	/// </summary>
	public interface IService
	{
		/// <summary>Gets a value indicating that the processor is fully operational.</summary>
		bool IsFullyOperational { get; }


		/// <summary>
		///		Execute the processor.
		/// </summary>
		void Execute();
	}
}
