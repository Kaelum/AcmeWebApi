using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace WebApplication
{
	/// <summary>
	///		Summary description for TcpHandler.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class TcpHandler : ConnectionHandler
	{
		private const string PROXY_HEADER_BEGIN = "PROXY ";
		private const string PROXY_HEADER_END = "\r\n";
		private const string HTTP_REQUEST_HEADER_BEGIN_1 = "GET ";
		private const string HTTP_REQUEST_HEADER_BEGIN_2 = "POST ";
		private const string HTTP_REQUEST_HEADER_END = "HTTP/1.1\r\n";
		private const string ACME_REQUEST_BEGIN_1 = "<?Acme ";
		private const string ACME_REQUEST_BEGIN_2 = "<acme>";
		private const string ACME_REQUEST_END = "</acme>";

		private const int MAX_REQUEST_OVERHEAD_SIZE = 2048;
		private const int MAX_URI_SIZE = 2048;

		private const string IGNORE_EXCEPTION_MESSAGE_1 = "Writing is not allowed after writer was completed.";

		private const string HTTP_RESPONSE_FORMAT = "HTTP/1.1 {0} {1}\r\nContent-Type: {2}\r\nContent-Length: {3}\r\nServer: acme.com\r\nConnection: Keep-Alive\r\n\r\n";

		private static readonly byte[] _proxyHeaderBeginBytes = Encoding.UTF8.GetBytes(PROXY_HEADER_BEGIN);
		private static readonly byte[] _proxyHeaderEndBytes = Encoding.UTF8.GetBytes(PROXY_HEADER_END);
		private static readonly byte[] _httpRequestHeaderBegin1Bytes = Encoding.UTF8.GetBytes(HTTP_REQUEST_HEADER_BEGIN_1);
		private static readonly byte[] _httpRequestHeaderBegin2Bytes = Encoding.UTF8.GetBytes(HTTP_REQUEST_HEADER_BEGIN_2);
		private static readonly byte[] _httpRequestHeaderEndBytes = Encoding.UTF8.GetBytes(HTTP_REQUEST_HEADER_END);
		private static readonly byte[] _acmeRequestBegin1Bytes = Encoding.UTF8.GetBytes(ACME_REQUEST_BEGIN_1);
		private static readonly byte[] _acmeRequestBegin2Bytes = Encoding.UTF8.GetBytes(ACME_REQUEST_BEGIN_2);
		private static readonly byte[] _acmeRequestEndBytes = Encoding.UTF8.GetBytes(ACME_REQUEST_END);

		// |:?(?::[0-9A-F]{{0,4}}){{0,7}}(?::[0-9A-F]{{0,4}})
		private static readonly Regex _proxyHeaderRegex = new Regex($"(?:{PROXY_HEADER_BEGIN})(?<protocol>TCP4|TCP6) (?<clientIp>(?:[0-9]{{1,3}}\\.){{3}}[0-9]{{1,3}}|(?::)?(?:[0-9A-F]{{0,4}}:){{0,7}}(?:[0-9A-F]{{0,4}})) (?<proxyIp>(?:[0-9]{{1,3}}\\.){{3}}[0-9]{{1,3}}|(?::)?(?:[0-9A-F]{{0,4}}:){{0,7}}(?:[0-9A-F]{{0,4}})) (?<clientPort>[0-9]{{1,5}}) (?<proxyPort>[0-9]{{1,5}})$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

		private readonly int _maxRequestSize;
		private readonly int _connectionTimeout;

		private readonly ILogger<TcpHandler> _logger;
		private readonly ApplicationSettings _applicationSettings;

		private readonly AcmeSerialization _acmeSerialization;
		private readonly ApiProcessor _apiProcessor;

		private class ReadBufferResult
		{
			public ReadBufferResult(
				SequencePosition position,
				bool isProxyProtocolheader,
				bool isHttpRequest
			)
			{
				Position = position;
				IsProxyProtocolheader = isProxyProtocolheader;
				IsHttpRequest = isHttpRequest;
			}

			public bool IsHttpRequest { get; set; }

			public bool IsProxyProtocolheader { get; set; }

			public SequencePosition Position { get; set; }
		}

		private class RequestBuffer
		{
			public RequestBuffer(
				byte[] buffer,
				bool isHttpRequest
			)
			{
				Buffer = buffer;
				IsHttpRequest = isHttpRequest;
			}

			public bool IsHttpRequest { get; set; }

			public byte[] Buffer { get; set; }
		}


		#region Constructors

		/// <summary>
		///		Create an instance of TcpHandler.
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="applicationSettings"></param>
		/// <param name="acmeSerialization"></param>
		/// <param name="apiProcessor"></param>
		public TcpHandler(
			ILogger<TcpHandler> logger,
			ApplicationSettings applicationSettings,
			AcmeSerialization acmeSerialization,
			ApiProcessor apiProcessor
		)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));

			_maxRequestSize = MAX_REQUEST_OVERHEAD_SIZE + MAX_URI_SIZE;
			_connectionTimeout = _applicationSettings.Tcp!.Timeout * 1000;// Tcp is required; therefore, it can't be null.

			_acmeSerialization = acmeSerialization ?? throw new ArgumentNullException(nameof(acmeSerialization));
			_apiProcessor = apiProcessor ?? throw new ArgumentNullException(nameof(apiProcessor));
		}

		#endregion

		/// <summary>
		///		?
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public override async Task OnConnectedAsync(ConnectionContext connection)
		{
			_logger.LogInformation(connection.ConnectionId + " connected");

			CancellationToken closedToken = connection.ConnectionClosed;
			PipeReader reader = connection.Transport.Input;
			PipeWriter writer = connection.Transport.Output;
			IPEndPoint localIpEndPoint = (IPEndPoint)connection.LocalEndPoint;
			int localPort = localIpEndPoint.Port;
			int proxyPort = localPort;
			IPEndPoint remoteIpEndPoint = (IPEndPoint)connection.RemoteEndPoint;
			IPAddress remoteIpAddress = remoteIpEndPoint.Address;
			string serviceType = GetServiceType(localPort, proxyPort);


			try
			{
				ReadResult readResult;
				ReadBufferResult readBufferResult;
				long blockCounter = 0;

				bool beginProxyMatched = false;
				int beginProxyMatchIndex = 0;
				int endProxyMatchIndex = 0;

				bool isHttpRequest = false;
				bool beginHttpRequestMatched = false;
				int beginHttpRequestMatch1Index = 0;
				int beginHttpRequestMatch2Index = 0;
				int endHttpRequestMatchIndex = 0;

				bool beginRequestMatched = false;
				int beginRequestMatch1Index = 0;
				int beginRequestMatch2Index = 0;
				int endRequestMatchIndex = 0;

				ReadBufferResult? parseReadBuffer(MemoryStream requestStream, ReadOnlySequence<byte> buffer)
				{
					long bytesRead = 0;

					if (buffer.IsEmpty)
					{
						return null;
					}

					foreach (ReadOnlyMemory<byte> segment in buffer)
					{
						ReadOnlySpan<byte> span = segment.Span;

						foreach (byte currentByte in span)
						{
							bytesRead++;

							if (!beginProxyMatched && !beginRequestMatched)
							{
								if (blockCounter == 0)
								{
									if (currentByte == _proxyHeaderBeginBytes[beginProxyMatchIndex])
									{
										beginProxyMatchIndex++;

										if (beginProxyMatchIndex == _proxyHeaderBeginBytes.Length)
										{
											beginProxyMatched = true;

											requestStream.Write(_proxyHeaderBeginBytes);
											continue;
										}
									}
									else if (beginProxyMatchIndex != 0)
									{
										beginProxyMatchIndex = 0;
									}
								}

								if (!beginHttpRequestMatched)
								{
									if (currentByte == _httpRequestHeaderBegin1Bytes[beginHttpRequestMatch1Index])
									{
										beginHttpRequestMatch1Index++;

										if (beginHttpRequestMatch1Index == _httpRequestHeaderBegin1Bytes.Length)
										{
											beginHttpRequestMatched = true;

											//requestStream.Write(_httpRequestHeaderBegin1Bytes);
											continue;
										}
									}
									else if (beginHttpRequestMatch1Index != 0)
									{
										beginHttpRequestMatch1Index = 0;
									}

									if (currentByte == _httpRequestHeaderBegin2Bytes[beginHttpRequestMatch2Index])
									{
										beginHttpRequestMatch2Index++;

										if (beginHttpRequestMatch2Index == _httpRequestHeaderBegin2Bytes.Length)
										{
											beginHttpRequestMatched = true;

											//requestStream.Write(_httpRequestHeaderBegin2Bytes);
											continue;
										}
									}
									else if (beginHttpRequestMatch2Index != 0)
									{
										beginHttpRequestMatch2Index = 0;
									}
								}
								else
								{
									if (currentByte == _httpRequestHeaderEndBytes[endHttpRequestMatchIndex])
									{
										if (++endHttpRequestMatchIndex == _httpRequestHeaderEndBytes.Length)
										{
											beginHttpRequestMatched = false;
											beginHttpRequestMatch1Index = 0;
											beginHttpRequestMatch2Index = 0;

											isHttpRequest = true;
										}
									}
									else
									{
										endHttpRequestMatchIndex = 0;
									}
								}

								if (currentByte == _acmeRequestBegin1Bytes[beginRequestMatch1Index])
								{
									beginRequestMatch1Index++;

									if (beginRequestMatch1Index == _acmeRequestBegin1Bytes.Length)
									{
										beginRequestMatched = true;

										requestStream.Write(_acmeRequestBegin1Bytes);
										continue;
									}
								}
								else if (beginRequestMatch1Index != 0)
								{
									beginRequestMatch1Index = 0;
								}

								if (currentByte == _acmeRequestBegin2Bytes[beginRequestMatch2Index])
								{
									beginRequestMatch2Index++;

									if (beginRequestMatch2Index == _acmeRequestBegin2Bytes.Length)
									{
										beginRequestMatched = true;

										requestStream.Write(_acmeRequestBegin2Bytes);
										continue;
									}
								}
								else if (beginRequestMatch2Index != 0)
								{
									beginRequestMatch2Index = 0;
								}

								//if (isHttpRequest)
								//{
								//	requestStream.WriteByte(currentByte);
								//}

								continue;
							}

							requestStream.WriteByte(currentByte);

							if (beginProxyMatched)
							{
								if (currentByte == _proxyHeaderEndBytes[endProxyMatchIndex])
								{
									if (++endProxyMatchIndex == _proxyHeaderEndBytes.Length)
									{
										readBufferResult = new ReadBufferResult(buffer.GetPosition(bytesRead), true, false);

										beginProxyMatched = false;
										beginProxyMatchIndex = 0;
										endProxyMatchIndex = 0;

										beginHttpRequestMatch1Index = 0;
										beginHttpRequestMatch2Index = 0;
										endHttpRequestMatchIndex = 0;

										beginRequestMatched = false;
										beginRequestMatch1Index = 0;
										beginRequestMatch2Index = 0;
										endRequestMatchIndex = 0;

										blockCounter++;
										isHttpRequest = false;
										requestStream.SetLength(requestStream.Length - _proxyHeaderEndBytes.Length);// Remove end bytes.

										return readBufferResult;
									}
								}
								else
								{
									endProxyMatchIndex = 0;
									continue;
								}
							}
							else if (beginRequestMatched)
							{
								if (currentByte == _acmeRequestEndBytes[endRequestMatchIndex])
								{
									if (++endRequestMatchIndex == _acmeRequestEndBytes.Length)
									{
										readBufferResult = new ReadBufferResult(buffer.GetPosition(bytesRead), false, isHttpRequest);

										beginProxyMatched = false;
										beginProxyMatchIndex = 0;
										endProxyMatchIndex = 0;

										beginHttpRequestMatch1Index = 0;
										beginHttpRequestMatch2Index = 0;
										endHttpRequestMatchIndex = 0;

										beginRequestMatched = false;
										beginRequestMatch1Index = 0;
										beginRequestMatch2Index = 0;
										endRequestMatchIndex = 0;

										blockCounter++;
										isHttpRequest = false;

										return readBufferResult;
									}
								}
								else
								{
									endRequestMatchIndex = 0;
									continue;
								}
							}
							else
							{
								throw new InvalidOperationException("Invalid state.");
							}
						}
					}

					return null;
				}

				void processRequestBuffer(RequestBuffer requestBuffer)
				{
					Task<AcmeRequest?> createAcmeRequestAsync(string serviceType, IPAddress remoteIpAddress)
					{
						string xmlPayloadString = Encoding.UTF8.GetString(requestBuffer.Buffer);

						return Task.FromResult(_acmeSerialization.CreatePostRequest(
							serviceType,
							remoteIpAddress,
							xmlPayloadString
						));
					}

					async Task sendResponseAsync(HttpResult httpResult)
					{
						try
						{
							if (closedToken.IsCancellationRequested)
							{
								return;
							}

							await SendResponseAsync(writer, httpResult, requestBuffer.IsHttpRequest, closedToken);
						}
						catch (InvalidOperationException ex) when (IGNORE_EXCEPTION_MESSAGE_1.Equals(ex.Message, StringComparison.InvariantCultureIgnoreCase))
						{
#if DEBUG
							_logger.LogWarning("{0} (closed: {1})", ex.Message, closedToken.IsCancellationRequested);
#endif
							return;
						}
					}

					Task.Run(() => _apiProcessor.ProcessRequestAsync(
						serviceType,
						remoteIpAddress,
						createAcmeRequestAsync,
						sendResponseAsync
					)).Wait();
				}

				using (Timer timeoutTimer = new Timer((stateInfo) => { connection.Abort(new ConnectionAbortedException("Connection timeout.")); }, null, Timeout.Infinite, Timeout.Infinite))
				using (MemoryStream bufferStream = new MemoryStream(new byte[_maxRequestSize], true))
				{
					bufferStream.SetLength(0L);

					while (!closedToken.IsCancellationRequested)
					{
						try
						{
							timeoutTimer.Change(_connectionTimeout, Timeout.Infinite);

							try { readResult = await reader.ReadAsync(closedToken); }
							catch (ConnectionResetException) { break; }
							catch (ConnectionAbortedException) { break; }
							catch (SocketException) { break; }
							catch (OperationCanceledException) { break; }

							if (readResult.IsCompleted) { break; }

							//SequencePosition? newPosition
							ReadBufferResult? readPosition = parseReadBuffer(bufferStream, readResult.Buffer);
							reader.AdvanceTo(readPosition?.Position ?? readResult.Buffer.End);

							if (readPosition == null) { continue; }

							bufferStream.Seek(0L, SeekOrigin.Begin);

							RequestBuffer requestBuffer = new RequestBuffer(
								new byte[bufferStream.Length],
								readPosition.IsHttpRequest
							);
							int readCount = await bufferStream.ReadAsync(requestBuffer.Buffer);

							if (readCount != bufferStream.Length)
							{
								throw new InvalidOperationException($"Read count does not match buffer length. ({readCount} != {bufferStream.Length})");
							}

							if (readPosition.IsProxyProtocolheader)
							{
								string proxyHeader = Encoding.ASCII.GetString(requestBuffer.Buffer);
								Match match = _proxyHeaderRegex.Match(proxyHeader);

								if (!match.Success)
								{
									throw new InvalidOperationException($"Unknown proxy header format: [{proxyHeader}]");
								}

								string matchedProtocol = match.Groups["protocol"].Value;
								string matchedClientIp = match.Groups["clientIp"].Value;
								string matchedProxyIp = match.Groups["proxyIp"].Value;
								string matchedClientPort = match.Groups["clientPort"].Value;
								string matchedProxyPort = match.Groups["proxyPort"].Value;

								_logger.LogTrace("AWS proxy protocol client {0}:{1} and proxy {2}:{3}", matchedClientIp, matchedClientPort, matchedProxyIp, proxyPort);

								if (!IPAddress.TryParse(matchedClientIp, out remoteIpAddress))
								{
									_logger.LogError("Unable to parse client IP address [{0}].", matchedClientIp);
								}

								if (int.TryParse(matchedProxyPort, out proxyPort))
								{
									serviceType = GetServiceType(localPort, proxyPort);
								}
								else
								{
									_logger.LogError("Unable to parse proxy port [{0}].", matchedProxyPort);
								}
							}
							else
							{
								ThreadPool.QueueUserWorkItem(processRequestBuffer, requestBuffer, false);
							}

							bufferStream.Seek(0L, SeekOrigin.Begin);
							bufferStream.SetLength(0);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Failure processing PipeReader.");
						}
					}
				}
			}
			catch (Exception ex)
			{
				try
				{
					int? seqNum;
					AcmeAuthorization? authorization;
					string? rawMethod;
					AcmeStatusCode acmeStatusCode;

					if (ex is AcmeException acmeException)
					{
						seqNum = acmeException.SeqNum;
						authorization = acmeException.Authorization;
						rawMethod = acmeException.RawMethod;
						acmeStatusCode = acmeException.StatusCode;
					}
					else
					{
						seqNum = null;
						authorization = null;
						rawMethod = null;
						acmeStatusCode = AcmeStatusCode.InternalServerError;
					}

					ErrorResult errorResult = new ErrorResult(
						seqNum,
						authorization,
						rawMethod,
						new XmlAcmeResponseError(
							ex,
							acmeStatusCode
						)
					);

					_logger.LogError(ex, "{0}.  [Client:{1}]", ex.Message, remoteIpAddress.ToString());

					await SendResponseAsync(writer, errorResult, false, closedToken);
				}
				catch (InvalidOperationException ex2) when (IGNORE_EXCEPTION_MESSAGE_1.Equals(ex2.Message, StringComparison.InvariantCultureIgnoreCase))
				{
					// Do nothing, the connection is closed and nothing needs to be done.
#if DEBUG
					_logger.LogWarning("{0} (closed: {1})", ex.Message, closedToken.IsCancellationRequested);
#endif
				}
				catch (Exception ex2)
				{
					_logger.LogError(ex2, "Failure sending error response. [Client:{0}]", remoteIpAddress?.ToString());
				}
			}

			_logger.LogInformation(connection.ConnectionId + " disconnected");
		}

		#region Private Methods

		private string GetServiceType(int localPort, int proxyPort)
		{
			if (localPort == 80)
			{
				return "http-stream";
			}

#if false
			return $"tcp-stream-{proxyPort}-{localPort}";
#else
			return $"tcp-stream-{proxyPort}";
#endif
		}

		/// <summary>
		///		Create a response for the specified <see cref="T:HttpResult"/> asynchronously.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="httpResult"></param>
		/// <param name="isHttpRequest"></param>
		/// <param name="closedToken"></param>
		/// <returns></returns>
		private async Task SendResponseAsync(
			PipeWriter writer,
			HttpResult httpResult,
			bool isHttpRequest,
			CancellationToken closedToken
		)
		{
			if (closedToken.IsCancellationRequested)
			{
				return;
			}

			int totalBytes;
			Memory<byte> outputMemory;

			if (isHttpRequest)
			{
				string httpResponseHeader = string.Format(
					HTTP_RESPONSE_FORMAT,
					httpResult.HttpStatusCode.ToString("D"),
					HttpStatusDescription.Get(httpResult.HttpStatusCode),
					httpResult.ContentType,
					httpResult.Response.Length
				);

				byte[] httpResponseHeaderBytes = Encoding.UTF8.GetBytes(httpResponseHeader);
				totalBytes = httpResult.Response.Length + httpResponseHeaderBytes.Length;

				if (closedToken.IsCancellationRequested)
				{
					return;
				}

				outputMemory = writer.GetMemory(totalBytes);
				httpResponseHeaderBytes.CopyTo(outputMemory);
				httpResult.Response.CopyTo(outputMemory.Slice(httpResponseHeaderBytes.Length));
			}
			else
			{
				totalBytes = httpResult.Response.Length;

				if (closedToken.IsCancellationRequested)
				{
					return;
				}

				outputMemory = writer.GetMemory(totalBytes);
				httpResult.Response.CopyTo(outputMemory);
			}

			if (closedToken.IsCancellationRequested)
			{
				return;
			}

			writer.Advance(totalBytes);
			await writer.FlushAsync();

#if DEBUG
			_logger.LogDebug(
				"outputMemory {0} bytes ({1} bytes requested)",
				outputMemory.Span.Length,
				totalBytes
			);
#endif
		}

		#endregion
	}
}
