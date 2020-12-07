using System;
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;
using System.Threading;

using NLog;

namespace WebApplication
{
	/// <summary>
	///		Summary description for Program.
	/// </summary>
	public class Program
	{
		private static Logger? _logger;


		/// <summary>
		///		Main entry point.
		/// </summary>
		/// <param name="args"></param>
		[MTAThread]
		//[LoaderOptimization(LoaderOptimization.MultiDomain)]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
		public static int Main(string[] args)
		{
			_logger = LogManager.GetCurrentClassLogger();

			_logger.Info("Application (START)");

			AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
			AppDomain.CurrentDomain.ProcessExit += ProcessExitHandler;

			try
			{
				GC.GetTotalMemory(false);

				ApiStartup.Execute(args);
			}
			catch (Exception ex)
			{
				_logger.Fatal(ex, "Application (FAILURE)");
				return 2;
			}

			//throw new InvalidOperationException("Throwing an unhandled exception.");

			_logger.Info("Application (END)");

			return 0;
		}

		#region Public Static Methods

		private static void ProcessExitHandler(object? sender, EventArgs e)
		{
			_logger?.Info("Application (ProcessExit)");
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private static void UnhandledExceptionHandler(object? sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = (Exception)e.ExceptionObject;

			_logger?.Error(ex, ex.Message);

			if (e.IsTerminating)
			{
				_logger?.Fatal("Application (APPCRASH)");

				// Sleep for 3 seconds to ensure that any logging is complete before crashing.
				Thread.Sleep(3000);
			}
		}

		#endregion
	}
}