using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog;

namespace WebApplication
{
	/// <summary>
	///		Summary description for ApiStartup.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class ApiStartup
	{
		internal const int SLEEP_1_SECOND = 1000;
		internal const int SLEEP_100_MILLISECONDS = 100;
		internal const int SLEEP_10_MILLISECONDS = 10;

		internal const string APPSETTINGS_FILENAME = "AppSettings";

		private static Logger? _logger;
		private static readonly ServiceThreadList _serviceThreads = new ServiceThreadList();

		private IConfiguration _configuration;


		#region Constructors

		/// <summary>
		///		Create an instance of ApiStartup.
		/// </summary>
		/// <param name="configuration"></param>
		public ApiStartup(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		#endregion

		#region Main

		/// <summary>
		///		Main entry point.
		/// </summary>
		/// <param name="args"></param>
		[MTAThread]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
		public static int Execute(string[] args)
		{
			_logger = LogManager.GetCurrentClassLogger();

			_logger.Info("ApiStartup (START)");

			try
			{
				using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
				{
					ApiService apiService = new ApiService(
						args,
						cancellationTokenSource
					);

					_serviceThreads.Add(apiService);

					while (_serviceThreads.All(t => t.Key.IsAlive) && !_serviceThreads.All(t => t.Value.IsFullyOperational))
					{
						Thread.Sleep(SLEEP_100_MILLISECONDS);
					}

					_logger.Info("ACME API (RUNNING)");

					Console.WriteLine("Press <ESC> to exit...");

					bool quit = false;

					while (!quit && _serviceThreads.All(t => t.Key.IsAlive))
					{
						Thread.Sleep(SLEEP_1_SECOND);

						while (!quit && !Console.IsInputRedirected && Console.KeyAvailable)
						{
							quit = (Console.ReadKey(true).Key == ConsoleKey.Escape);
						}
					}

					try
					{
						cancellationTokenSource.Cancel();
					}
					catch (AggregateException ae)
					{
						ae.Handle(ex => ex is TaskCanceledException);
					}

					for (bool firstPass = true; _serviceThreads.Count != 0; firstPass = false)
					{
						foreach (KeyValuePair<Thread, IService> processorThread in _serviceThreads.ToArray())
						{
							if (firstPass && processorThread.Key.IsAlive)
							{
								_logger.Info("Thread.Join: {0}", (string.IsNullOrWhiteSpace(processorThread.Key.Name) ? processorThread.Key.ManagedThreadId.ToString() : processorThread.Key.Name));
							}

							if (!processorThread.Key.IsAlive || processorThread.Key.Join(SLEEP_10_MILLISECONDS))
							{
								//processorThread.Value.Dispose();

								_serviceThreads.Remove(processorThread.Key);
							}
						}

						Thread.Sleep(SLEEP_100_MILLISECONDS);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.Fatal(ex, "ApiStartup (FAILURE)");
				return 2;
			}

			_logger.Info("ApiStartup (END)");

			return 0;
		}

		#endregion

		#region Public Methods

		/// <summary>
		///		This method gets called by the runtime. Use this method to add services to the
		///		container.
		/// </summary>
		/// <remarks>
		///		This executes before Configure().
		/// </remarks>
		/// <param name="services"></param>
		public void ConfigureServices(IServiceCollection services)
		{
			try
			{
				//services
				//	.AddSingleton<AcmeSerialization>();
			}
			catch (Exception ex)
			{
				_logger?.Error(ex, null);
				throw;
			}
		}

		/// <summary>
		///		This method gets called by the runtime. Use this method to configure the HTTP
		///		request pipeline.
		/// </summary>
		/// <remarks>
		///		This executes after ConfigureServices().
		/// </remarks>
		/// <param name="logger"></param>
		/// <param name="app"></param>
		/// <param name="env"></param>
		public void Configure(ILogger<ApiStartup> logger, IApplicationBuilder app, IWebHostEnvironment env)
		{
			try
			{
				if (env.IsEnvironment("Local") || env.IsDevelopment())
				{
					app.UseDeveloperExceptionPage();
				}
				else if (env.IsEnvironment("QA"))
				{
					app.UseHsts();
				}
				else
				{
					// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
					app.UseHsts();
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, null);
				throw;
			}
		}

		#endregion

		#region Public Static Methods

		/// <summary>
		///		Add the specified service to the thread list.
		/// </summary>
		/// <param name="service"></param>
		public static void AddProcessor(IService service)
		{
			_serviceThreads.Add(service);
		}

		#endregion
	}
}
