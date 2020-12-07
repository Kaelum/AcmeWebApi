using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog;
using NLog.Extensions.Logging;

namespace WebApplication
{
	/// <summary>
	///		Summary description for WebApiProcessor.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class ApiService : IService
	{
		private static ApplicationSettings _appSettings = new ApplicationSettings();

		private readonly string[] _args;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly CancellationToken _cancellationToken;


		#region Constructors

		/// <summary>
		///		Create an instance of WebApiProcessor.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="cancellationTokenSource"></param>
		internal ApiService(
			string[] args,
			CancellationTokenSource cancellationTokenSource
		)
		{
			_args = args;
			_cancellationTokenSource = cancellationTokenSource;

			_cancellationToken = _cancellationTokenSource.Token;

			Logger = LogManager.GetCurrentClassLogger();
		}

		#endregion

		#region IService Implementation

		/// <summary>Gets a value indicating that initialization is complete.</summary>
		public bool IsFullyOperational { get; private set; } = false;

		/// <summary>
		///		Execute the processor.
		/// </summary>
		public void Execute()
		{
			Thread thread = Thread.CurrentThread;

			try
			{
				Logger.Info($"PROCESSOR-START ({nameof(ApiService)}): {(string.IsNullOrWhiteSpace(thread.Name) ? thread.ManagedThreadId.ToString() : thread.Name)}");

				IHost webApiHost = CreateHostBuilder(_args).Build();
				LogSettings(webApiHost);

				Task webApiHostTask = webApiHost.RunAsync(_cancellationToken);

				List<Task> tasks = new List<Task>
				{
					webApiHostTask,
				};

				IsFullyOperational = true;

				if (Task.WaitAny(tasks.ToArray(), _cancellationToken) != -1)
				{
					if (!webApiHostTask.IsCompleted)
					{
						tasks.Add(webApiHost.StopAsync(_cancellationToken));
					}
				}

				Task.WaitAll(tasks.ToArray());
			}
			catch (OperationCanceledException)
			{
				// Perform any cleanup, if needed.
			}
			catch (Exception ex)
			{
				Logger.Error(ex, $"PROCESSOR-EXCEPTION ({nameof(ApiService)}): {(string.IsNullOrWhiteSpace(thread.Name) ? thread.ManagedThreadId.ToString() : thread.Name)}");
			}
			finally
			{
				Logger.Info($"PROCESSOR-END ({nameof(ApiService)}): {(string.IsNullOrWhiteSpace(thread.Name) ? thread.ManagedThreadId.ToString() : thread.Name)}");
			}
		}

		#endregion

		#region Private Properties

		/// <summary>Gets the current <see cref="T:Logger"/>.</summary>
		private Logger Logger { get; }

		#endregion

		#region Public Static Properties

		/// <summary>Gets the <see cref="T:ApplicationSettings"/>.</summary>
		public static ApplicationSettings AppSettings => _appSettings;

		#endregion

		#region Public Static Methods

		/// <summary>
		///		Set the global AppSettings.
		/// </summary>
		/// <param name="appSettings"></param>
		/// <returns></returns>
		public static ApplicationSettings SetAppSettings(ApplicationSettings appSettings)
		{
			return (_appSettings = appSettings);
		}

		#endregion

		#region Private Methods

		/// <summary>
		///		Create host builder.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		private IHostBuilder CreateHostBuilder(string[] args)
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			FileInfo fileInfo = new FileInfo(executingAssembly.Location);

			// Taken from Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(string[] args)
			// and customized for our purposes to increase performance.

			HostBuilder hostBuilder = new HostBuilder();
			hostBuilder.UseContentRoot(fileInfo.DirectoryName);
			hostBuilder.ConfigureHostConfiguration((config) =>
			{
				config.AddEnvironmentVariables("DOTNET_");

				if (args != null)
				{
					config.AddCommandLine(args);
				}

				IConfigurationRoot configurationRoot = config.Build();

				if (configurationRoot.GetValue<string>("ENVIRONMENT") == null)
				{
					throw new InvalidOperationException("The environment variables DOTNET_ENVIRONMENT and ASPDOTNET_ENVIRONMENT must be defined.");
				}
			});
			hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
			{
				IHostEnvironment hostingEnvironment = hostingContext.HostingEnvironment;

				config.AddXmlFile($"{ApiStartup.APPSETTINGS_FILENAME}.config", false, true);

				if (
					(hostingEnvironment.IsEnvironment("Local") || hostingEnvironment.IsDevelopment())
					&& !string.IsNullOrEmpty(hostingEnvironment.ApplicationName)
				)
				{
					Assembly assembly = Assembly.Load(new AssemblyName(hostingEnvironment.ApplicationName));

					if (assembly != null)
					{
						config.AddUserSecrets(assembly, true);
					}
				}

				config.AddEnvironmentVariables("ASPDOTNET_");

				if (args != null)
				{
					config.AddCommandLine(args);
				}

				IConfigurationRoot configurationRoot = config.Build();

				if (configurationRoot.GetValue<string>("ENVIRONMENT") == null)
				{
					throw new InvalidOperationException("The environment variables DOTNET_ENVIRONMENT and ASPDOTNET_ENVIRONMENT must be defined.");
				}
			});
			hostBuilder.ConfigureLogging((hostingContext, logging) =>
			{
				try
				{
					bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

					// This defines the minimum log level, so it must be configured correctly.
					logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));

					bool isContainer = hostingContext.Configuration.GetValue<bool>("RUNNING_IN_CONTAINER", false);

					logging.AddNLog();
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
				}
			});
			hostBuilder.UseDefaultServiceProvider((context, options) =>
			{
				try
				{
					bool validateOnBuild = options.ValidateScopes = (context.HostingEnvironment.IsEnvironment("Local") || context.HostingEnvironment.IsDevelopment());
					options.ValidateOnBuild = validateOnBuild;
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
				}
			});
			hostBuilder.ConfigureServices((hostingContext, services) =>
			{
				try
				{
					IConfiguration configuration = hostingContext.Configuration;

					SetAppSettings(configuration.Get<ApplicationSettings>());

					List<ValidationResult> validationResults = new List<ValidationResult>();

					if (!ValidatorRecursive.TryValidateObject(AppSettings, validationResults, true))
					{
						StringBuilder sb = new StringBuilder();

						foreach (ValidationResult validationResult in validationResults)
						{
							sb.AppendLine($"- {validationResult.ErrorMessage}");
						}

						throw new InvalidOperationException($"Invalid configuration.{Environment.NewLine}{sb.ToString()}");
					}

					ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
					ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);

					if (AppSettings.Threading!.MaxWorkerThreads < AppSettings.Threading.MinWorkerThreads)// Threading is required; therefore, it can't be null.
					{
						throw new InvalidOperationException("MaxWorkerThreads must be greater or equal to MinWorkerThreads.");
					}

					if (AppSettings.Threading.MaxCompletionPortThreads < AppSettings.Threading.MinCompletionPortThreads)
					{
						throw new InvalidOperationException("MaxCompletionPortThreads must be greater or equal to MinCompletionPortThreads.");
					}

					if (minWorkerThreads > AppSettings.Threading.MaxWorkerThreads || minCompletionPortThreads > AppSettings.Threading.MaxCompletionPortThreads)
					{
						if (!ThreadPool.SetMinThreads(AppSettings.Threading.MinWorkerThreads, AppSettings.Threading.MinCompletionPortThreads))
						{
							Logger.Warn("Unable to set the minimum number of threads available in the thread pool.");
						}

						if (!ThreadPool.SetMaxThreads(AppSettings.Threading.MaxWorkerThreads, AppSettings.Threading.MaxCompletionPortThreads))
						{
							throw new InvalidOperationException("Unable to set the maximum number of threads available in the thread pool.");
						}
					}
					else
					{
						if (!ThreadPool.SetMaxThreads(AppSettings.Threading.MaxWorkerThreads, AppSettings.Threading.MaxCompletionPortThreads))
						{
							throw new InvalidOperationException("Unable to set the maximum number of threads available in the thread pool.");
						}

						if (!ThreadPool.SetMinThreads(AppSettings.Threading.MinWorkerThreads, AppSettings.Threading.MinCompletionPortThreads))
						{
							Logger.Warn("Unable to set the minimum number of threads available in the thread pool.");
						}
					}

					services
						.AddOptions()
						.AddSingleton(_cancellationTokenSource)
						.AddSingleton(AppSettings);
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
				}
			});
			hostBuilder.ConfigureWebHost((webHostBuilder) =>
			{
				try
				{
					webHostBuilder.UseKestrel((builderContext, options) =>
					{
						KestrelConfigurationLoader kestrelConfig = options.Configure(builderContext.Configuration.GetSection("Kestrel"));

						foreach (string endpointName in AppSettings.Tcp!.KestrelTcpEndpoints.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()))
						{
							if (string.IsNullOrWhiteSpace(endpointName))
							{
								continue;
							}

							kestrelConfig.Endpoint(
								endpointName,
								endPoint =>
								{
									endPoint.ListenOptions.UseConnectionHandler<TcpHandler>();

									if (AppSettings.Options!.EnableConnectionLogging)
									{
										endPoint.ListenOptions.UseConnectionLogging();
									}
								}
							);
						}
					});
					webHostBuilder.UseStartup<ApiStartup>();
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
				}
			});

			return hostBuilder;
		}

		private void LogSettings(IHost host)
		{
			Logger.Info(string.Empty);
			Logger.Info($"Application: {AppSettings.ApplicationName}");
			Logger.Info($"Environment: {AppSettings.Environment}");

			ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
			ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
			ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);

			Logger.Info(string.Empty);
			Logger.Info($"ThreadPool:");
			Logger.Info($"  · Worker         : {minWorkerThreads,7:#,0} (min), {maxWorkerThreads,7:#,0} (max), {workerThreads,7:#,0} (available)");
			Logger.Info($"  · Completion Port: {minCompletionPortThreads,7:#,0} (min), {maxCompletionPortThreads,7:#,0} (max), {completionPortThreads,7:#,0} (available)");

			Logger.Info(string.Empty);
			Logger.Info("Garbage Collection:");
			Logger.Info($"  · GC Mode            : {(GCSettings.IsServerGC ? "Server" : "Workstation")}");
			Logger.Info($"  · LOH Compaction Mode: {GCSettings.LargeObjectHeapCompactionMode.ToString("G")}");
			Logger.Info($"  · Latency Mode       : {GCSettings.LatencyMode.ToString("G")}");

			Logger.Info(string.Empty);
			Logger.Info($"Application Options:");
			Logger.Info($"  · EnableConnectionLogging: {AppSettings.Options!.EnableConnectionLogging}");
			Logger.Info($"  · KestrelTcpEndpoints    : {AppSettings.Tcp!.KestrelTcpEndpoints}");

			Logger.Info(string.Empty);
		}

		#endregion
	}
}
