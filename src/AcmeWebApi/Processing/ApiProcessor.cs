using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace WebApplication
{
	/// <summary>
	///		delegate
	/// </summary>
	/// <param name="serviceType"></param>
	/// <param name="remoteIpAddress"></param>
	/// <returns></returns>
	public delegate Task<AcmeRequest?> CreateAcmeRequestAsync(string serviceType, IPAddress remoteIpAddress);

	/// <summary>
	///		delegate
	/// </summary>
	/// <param name="result"></param>
	/// <returns></returns>
	public delegate Task SendResponseAsync(HttpResult result);

	/// <summary>
	///		Summary description for ApiProcessor.
	/// </summary>
	public class ApiProcessor
	{
		private readonly ILogger _logger;
		private readonly ApplicationSettings _applicationSettings;

		private readonly Random _random = new Random();


		#region Constructors

		/// <summary>
		///		Create and instance of ApiProcessor.
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="applicationSettings"></param>
		public ApiProcessor(
			ILogger<ApiProcessor> logger,
			ApplicationSettings applicationSettings
		)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
		}

		#endregion

		#region Public Static Properties

		/// <summary>Gets a value indicating that the application is ready for processing.</summary>
		public static bool IsReady { get; internal set; }

		#endregion

		#region Public Methods

		/// <summary>
		///		Process a request asynchronously.
		/// </summary>
		/// <param name="serviceType">The service type ("rest" or "stream").</param>
		/// <param name="remoteIpAddress">The <see cref="T:IPAddress"/> of the remote client.</param>
		/// <param name="createAcmeRequestAsync">The asynchronous method that creates a
		///		<see cref="T:AcmeRequest&lt;TMethod&gt;"/> object from a request.</param>
		/// <param name="sendResponseAsync">The asynchronous method that sends a response to the
		///		client.</param>
		/// <returns>
		///		A <see cref="T:ProcessResult"/> object.
		/// </returns>
		public async Task<ProcessResult> ProcessRequestAsync(
			string serviceType,
			IPAddress remoteIpAddress,
			CreateAcmeRequestAsync createAcmeRequestAsync,
			SendResponseAsync sendResponseAsync
		)
		{
			AcmeRequest? acmeRequest = null;
			HttpResult? httpResult = null;

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			try
			{
				acmeRequest = await createAcmeRequestAsync(serviceType, remoteIpAddress);

				if (acmeRequest == null)
				{
					return new ProcessResult(false);
				}

				httpResult = await ExecuteMethod(acmeRequest);
			}
			catch (Exception ex)
			{
				int? seqNum = null;
				AcmeAuthorization? authorization = null;
				string? rawMethod = null;
				AcmeStatusCode? statusCode = null;
				string? errorMessage = null;

				switch (ex)
				{
					case AcmeException acmeException:
					{
						seqNum = acmeException.SeqNum;
						authorization = acmeException.Authorization;
						rawMethod = acmeException.RawMethod;
						statusCode = acmeException.StatusCode;
						errorMessage = acmeException.GetMesages();
						break;
					}

					case BadHttpRequestException badHttpRequestException:
					{
						statusCode = AcmeStatusCode.BadRequest;
						errorMessage = badHttpRequestException.GetMesages();
						break;
					}
				}

				if (httpResult is AcmeResult acmeResult)
				{
					if (!seqNum.HasValue)
					{
						seqNum = acmeResult.SeqNum;
					}

					if (authorization == null)
					{
						authorization = acmeResult.Authorization;
					}

					if (rawMethod == null)
					{
						rawMethod = acmeResult.RawMethod;
					}

					if (!statusCode.HasValue)
					{
						statusCode = acmeResult.AcmeStatusCode;
					}

					if (errorMessage == null)
					{
						errorMessage = (acmeResult is ErrorResult acmeErrorResult ? acmeErrorResult.ErrorMessage : ex.GetMesages());
					}
				}

				if (!seqNum.HasValue)
				{
					seqNum = acmeRequest?.SeqNum;
				}

				if (authorization == null)
				{
					authorization = acmeRequest?.Authorization;
				}

				if (rawMethod == null)
				{
					rawMethod = acmeRequest?.RawMethod;
				}

				if (!statusCode.HasValue)
				{
					if (acmeRequest?.Authorization.StatusCode != AcmeStatusCode.OK)
					{
						statusCode = acmeRequest?.Authorization.StatusCode;
					}

					if (!statusCode.HasValue)
					{
						statusCode = AcmeStatusCode.InternalServerError;
					}
				}

				if (errorMessage == null)
				{
					errorMessage = ex.GetMesages();
				}

				httpResult = new ErrorResult(
					seqNum,
					authorization,
					rawMethod,
					new XmlAcmeResponseError(
						ex,
						errorMessage,
						statusCode.Value
					)
				);
			}

			try
			{
				await sendResponseAsync(httpResult);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failure sending response. [Client:{0}]", remoteIpAddress.ToString());
			}

			try
			{
				stopwatch.Stop();

				if (httpResult is ErrorResult errorResult)
				{
					switch (errorResult.AcmeStatusCode)
					{
						case AcmeStatusCode.InvalidDeviceId:
						case AcmeStatusCode.InvalidOem:
						case AcmeStatusCode.InvalidOemId:
						case AcmeStatusCode.InvalidProduct:
						case AcmeStatusCode.InvalidUniqueIdFormat:
						case AcmeStatusCode.LicenseExceeded:
						case AcmeStatusCode.LicenseExpired:
						case AcmeStatusCode.LicenseInUse:
						case AcmeStatusCode.LicenseNotSupported:
						case AcmeStatusCode.Unauthorized:
						{
							_logger.LogWarning(
								"[Client:{0}] [Oem:{1}] [Device:{2}] [Unique:{3}] {4}",
								remoteIpAddress.ToString(),
								errorResult.Authorization?.Credential?.OemId,
								errorResult.Authorization?.Credential?.DeviceId,
								errorResult.Authorization?.Credential?.UniqueId,
								errorResult.ErrorMessage
							);
							break;
						}

						case AcmeStatusCode.InternalServerError:
						default:
						{
							if (acmeRequest == null)
							{
								_logger.LogError(
									errorResult.Exception,
									"[Client:{0}] {1}",
									remoteIpAddress.ToString(),
									errorResult.ErrorMessage
								);
							}
							else
							{
								_logger.LogError(
									errorResult.Exception,
									"[Client:{0}] {1}:\n-------------------\n{2}\n-------------------",
									remoteIpAddress.ToString(),
									errorResult.ErrorMessage,
									acmeRequest.RawRequest
								);
							}
							break;
						}
					}
				}

				if (httpResult is AcmeResult acmeResult)
				{
					_logger.LogDebug("Executed [{0}] method in {1} milliseconds: {2} {3}",
						acmeResult.RawMethod,
						stopwatch.ElapsedMilliseconds,
						acmeResult.AcmeStatusCode.ToString("D"),
						acmeResult.AcmeStatusCode.GetDescription()
					);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failure writing queue message. [Client:{0}]", remoteIpAddress.ToString());
			}

			return httpResult;
		}

		#endregion

		#region Private Methods

		#region Execute Method

		private async Task<HttpResult> ExecuteMethod(AcmeRequest acmeRequest)
		{
			switch (acmeRequest.Method)
			{
				case AcmePostMethod.Auth:
				{
					ResponseResult<XmlAcmeResponseAuth> result = await AuthorizationAsync(
						acmeRequest.Authorization,
						acmeRequest.RemoteIpAddress
					);

					return AcmeResult.Create(
						_applicationSettings.Options!,
						acmeRequest,
						result
					);
				}

				case AcmePostMethod.BuildVersion:
				{
					ResponseResult<XmlAcmeResponseBuildVersion> result = await BuildVersionAsync(
						acmeRequest.Authorization
					);

					return AcmeResult.Create(
						_applicationSettings.Options!,
						acmeRequest,
						result
					);
				}

				case AcmePostMethod.UriInfo:
				{
					if (acmeRequest.Uri == null)
					{
						return new ErrorResult(
							acmeRequest.SeqNum,
							acmeRequest.Authorization,
							acmeRequest.RawMethod,
							new XmlAcmeResponseError(
								null,
								$"<{nameof(acmeRequest.Uri)}> is null.",
								AcmeStatusCode.MalformedXml
							)
						);
					}

					ResponseResult<XmlAcmeResponseUriInfo> result = await UriInfoAsync(
						acmeRequest.Authorization,
						acmeRequest.Uri
					);

					return AcmeResult.Create(
						_applicationSettings.Options!,
						acmeRequest,
						result
					);
				}

				default:
				{
					return new ErrorResult(
						acmeRequest.SeqNum,
						acmeRequest.Authorization,
						acmeRequest.RawMethod,
						new XmlAcmeResponseError(
							null,
							$"Unknown [{acmeRequest.RawMethod}] method.",
							AcmeStatusCode.InvalidMethod
						)
					);
				}
			}
		}

		#endregion

		#region Authorization

		/// <summary>
		///		Checks whether or not the specified <see cref="T:AcmeAuthorization"/> object is
		///		authorized for the specified noun and verb combination asynchronously.
		/// </summary>
		/// <param name="authorization"></param>
		/// <returns>
		///		<b>true</b> if the account is authorized; otherwise, <b>false</b>.  When <b>false</b>,
		///		the HttpResponse is appropriately completed.
		/// </returns>
		private AcmeStatusCode GetAuthorizationStatusCodeAsync(AcmeAuthorization? authorization)
		{
			if (authorization == null)
			{
				return AcmeStatusCode.OK;
			}

			if (authorization.StatusCode != AcmeStatusCode.OK)
			{
				return authorization.StatusCode;
			}

			// Fall-through.  If we can't find a reason to deny access, access must be granted.
			return AcmeStatusCode.OK;
		}

		#endregion

		#region ACME

		private async Task<ResponseResult<XmlAcmeResponseAuth>> AuthorizationAsync(
			AcmeAuthorization authorization,
			IPAddress remoteIpAddress
		)
		{
			await Task.Delay(_random.Next(3, 5));

			AcmeStatusCode authStatusCode = GetAuthorizationStatusCodeAsync(authorization);
			LoggingInfo loggingInfo = new LoggingInfo
			{
				Key = $"{authorization.Credential?.OemId}|{authorization.Credential?.DeviceId}|{authorization.Credential?.UniqueId}",
			};

			if (authStatusCode != AcmeStatusCode.OK)
			{
				return new ResponseResult<XmlAcmeResponseAuth>(authStatusCode, loggingInfo);
			}

			XmlAcmeResponseAuth xmlResponse = new XmlAcmeResponseAuth(
				authStatusCode,
				DateTime.UtcNow.AddDays(1).Date.ToString("MM-dd-yyyy 23:59:59")
			);

			return new ResponseResult<XmlAcmeResponseAuth>(
				xmlResponse,
				loggingInfo
			);
		}

		private async Task<ResponseResult<XmlAcmeResponseBuildVersion>> BuildVersionAsync(
			AcmeAuthorization authorization
		)
		{
			await Task.Delay(_random.Next(3, 5));

			AcmeStatusCode authStatusCode = GetAuthorizationStatusCodeAsync(authorization);
			LoggingInfo loggingInfo = new LoggingInfo
			{
				Key = null,
			};

			if (authStatusCode != AcmeStatusCode.OK)
			{
				return new ResponseResult<XmlAcmeResponseBuildVersion>(authStatusCode, loggingInfo);
			}

			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			XmlAcmeSubResponseAssemblyVersion[] assemblyVersions = assemblies
				.Where(t => t.FullName?.StartsWith("Acme") ?? false)
				.Select(t => new XmlAcmeSubResponseAssemblyVersion(t))
				.ToArray();

			return new ResponseResult<XmlAcmeResponseBuildVersion>(
				new XmlAcmeResponseBuildVersion(
					Environment.OSVersion.VersionString,
					Environment.Version.ToString(),
					(
						Environment.Version.MajorRevision != -1 && Environment.Version.MinorRevision != -1
						? $"{Environment.Version.MajorRevision}.{Environment.Version.MinorRevision}"
						: null
					),
					assemblyVersions
				),
				loggingInfo
			);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="authorization"></param>
		/// <param name="uri"></param>
		/// <returns></returns>
		private async Task<ResponseResult<XmlAcmeResponseUriInfo>> UriInfoAsync(
			AcmeAuthorization? authorization,
			string uri
		)
		{
			await Task.Delay(_random.Next(3, 5));

			AcmeStatusCode authStatusCode = GetAuthorizationStatusCodeAsync(authorization);
			LoggingInfo loggingInfo = new LoggingInfo
			{
				Key = uri,
			};

			if (authStatusCode != AcmeStatusCode.OK)
			{
				return new ResponseResult<XmlAcmeResponseUriInfo>(authStatusCode, loggingInfo);
			}

			XmlAcmeResponseUriInfo response = new XmlAcmeResponseUriInfo(
				uri,
				uri,
				uri,
				uri,
				new XmlAcmeSubResponseCatConf[]
				{
						new XmlAcmeSubResponseCatConf(3, 70),
						new XmlAcmeSubResponseCatConf(45, 23),
						new XmlAcmeSubResponseCatConf(78, 90),
				},
				40,
				false,
				false,
				null
			);
			loggingInfo.Cats = response.Categories?.Select(t => t.CategoryId).ToArray();
			loggingInfo.Bcri = response.Bcri;

			// Create a dummy return to prevent a Redis network call.
			return new ResponseResult<XmlAcmeResponseUriInfo>(
				response,
				loggingInfo
			);
		}

		#endregion

		#endregion
	}
}
