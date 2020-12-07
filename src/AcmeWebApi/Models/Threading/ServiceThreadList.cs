using System;
using System.Collections.Generic;
using System.Threading;

namespace WebApplication
{
	/// <summary>
	///		Summary description for ServiceThreadList.
	/// </summary>
	public class ServiceThreadList : SortedList<Thread, IService>
	{
		/// <summary>
		///		Create and instance of ServiceThreadList.
		/// </summary>
		public ServiceThreadList()
			: base(new CompareThreadByManagedThreadId()) { }

		/// <summary>
		///		Add the specified <see cref="T:IService"/> object to the service thread list.
		/// </summary>
		/// <param name="service"></param>
		public void Add(IService service)
		{
			if (service == null)
			{
				throw new ArgumentNullException(nameof(service));
			}

			Thread thread = new Thread(new ThreadStart(service.Execute))
			{
				//Name = "ServiceMethod Processing",
			};

			thread.Start();

			base.Add(thread, service);
		}
	}
}
