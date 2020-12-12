using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

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
		/// <summary>The Acme XML processing instruction name.</summary>
		public const string ProcessingInstructionName = "Acme";
		/// <summary>The Acme XML processing instruction text.</summary>
		public const string ProcessingInstructionText = "version=acme/1.1";

		private const string HTTP_REQUEST_HEADER_BEGIN_1 = "GET ";
		private const string HTTP_REQUEST_HEADER_BEGIN_2 = "POST ";
		private const string HTTP_REQUEST_HEADER_END = "HTTP/1.1\r\n";
		private const string ACME_REQUEST_BEGIN_1 = "<?Acme ";
		private const string ACME_REQUEST_BEGIN_2 = "<acme>";
		private const string ACME_REQUEST_END = "</acme>";

		private const int MAX_REQUEST_OVERHEAD_SIZE = 2048;
		private const int MAX_URI_SIZE = 2048;

		private const string HTTP_RESPONSE_HEADER = "HTTP/1.1 {0} {1}\r\nContent-Type: {2}\r\nContent-Length: {3}\r\nServer: acme.com\r\nConnection: Keep-Alive\r\n\r\n";

		private static readonly byte[] _httpRequestHeaderBegin1Bytes = Encoding.UTF8.GetBytes(HTTP_REQUEST_HEADER_BEGIN_1);
		private static readonly byte[] _httpRequestHeaderBegin2Bytes = Encoding.UTF8.GetBytes(HTTP_REQUEST_HEADER_BEGIN_2);
		private static readonly byte[] _httpRequestHeaderEndBytes = Encoding.UTF8.GetBytes(HTTP_REQUEST_HEADER_END);
		private static readonly byte[] _acmeRequestBegin1Bytes = Encoding.UTF8.GetBytes(ACME_REQUEST_BEGIN_1);
		private static readonly byte[] _acmeRequestBegin2Bytes = Encoding.UTF8.GetBytes(ACME_REQUEST_BEGIN_2);
		private static readonly byte[] _acmeRequestEndBytes = Encoding.UTF8.GetBytes(ACME_REQUEST_END);

		private readonly int _maxRequestSize;
		private readonly int _connectionTimeout;

		private readonly ILogger<TcpHandler> _logger;
		private readonly ApplicationSettings _applicationSettings;

		// This object removes the default namespaces created by the XmlSerializer.
		private static readonly XmlSerializerNamespaces _xmlnsEmpty = new XmlSerializerNamespaces(
			new XmlQualifiedName[] {
				new XmlQualifiedName(string.Empty, string.Empty),
			}
		);

		private static readonly XmlWriterSettings _xmlWriterSettings = new XmlWriterSettings
		{
			Async = true,
			ConformanceLevel = ConformanceLevel.Document,// Fragment has a bug that requires a work-around.
			OmitXmlDeclaration = true,
			Encoding = new UTF8Encoding(false),
			NamespaceHandling = NamespaceHandling.OmitDuplicates,
			//Indent = true,
			//IndentChars = "\t",
		};

		private class ReadBufferResult
		{
			public ReadBufferResult(
				SequencePosition position,
				bool isHttpRequest
			)
			{
				Position = position;
				IsHttpRequest = isHttpRequest;
			}

			public bool IsHttpRequest { get; set; }

			public SequencePosition Position { get; set; }
		}

		private class TcpRequest
		{
			public TcpRequest(
				bool isHttpRequest,
				byte[] request
			)
			{
				Request = request;
				IsHttpRequest = isHttpRequest;
			}

			public bool IsHttpRequest { get; set; }

			public byte[] Request { get; set; }
		}

		private class TcpResponse
		{
			public TcpResponse(
				byte[]? header,
				byte[] body
			)
			{
				Body = body;
				Header = header;
			}

			public byte[] Body { get; set; }

			public byte[]? Header { get; set; }
		}


		#region Constructors

		/// <summary>
		///		Create an instance of TcpHandler.
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="applicationSettings"></param>
		public TcpHandler(
			ILogger<TcpHandler> logger,
			ApplicationSettings applicationSettings
		)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));

			_maxRequestSize = MAX_REQUEST_OVERHEAD_SIZE + MAX_URI_SIZE;
			_connectionTimeout = _applicationSettings.Tcp!.Timeout * 1000;// Tcp is required; therefore, it can't be null.
		}

		#endregion

		/// <summary>
		///		?
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public override async Task OnConnectedAsync(ConnectionContext connection)
		{
			_logger.LogInformation("{0} connected", connection.ConnectionId);

			CancellationTokenSource requestTimeoutToken;
			CancellationToken closedToken = connection.ConnectionClosed;
			PipeReader reader = connection.Transport.Input;
			PipeWriter writer = connection.Transport.Output;
			IPEndPoint remoteIpEndPoint = (IPEndPoint)connection.RemoteEndPoint;

			IPAddress remoteIpAddress = remoteIpEndPoint.Address;


			try
			{
				ReadBufferResult readBufferResult;
				long blockCounter = 0;

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

							if (!beginRequestMatched)
							{
								if (!beginHttpRequestMatched)
								{
									if (currentByte == _httpRequestHeaderBegin1Bytes[beginHttpRequestMatch1Index])
									{
										beginHttpRequestMatch1Index++;

										if (beginHttpRequestMatch1Index == _httpRequestHeaderBegin1Bytes.Length)
										{
											beginHttpRequestMatched = true;

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

								continue;
							}

							requestStream.WriteByte(currentByte);

							if (beginRequestMatched)
							{
								if (currentByte == _acmeRequestEndBytes[endRequestMatchIndex])
								{
									if (++endRequestMatchIndex == _acmeRequestEndBytes.Length)
									{
										readBufferResult = new ReadBufferResult(buffer.GetPosition(bytesRead), isHttpRequest);

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

				ReadResult readResult;
				byte[] buffer = new byte[_maxRequestSize];

				using (MemoryStream bufferStream = new MemoryStream(buffer, true))
				{
					bufferStream.SetLength(0L);

					while (!closedToken.IsCancellationRequested)
					{
						requestTimeoutToken = new CancellationTokenSource(_connectionTimeout);

						try
						{
							readResult = await reader.ReadAsync(
								requestTimeoutToken.Token
							);

							if (readResult.IsCanceled)
							{
								connection.Abort();
								break;
							}
							else if (readResult.IsCompleted)
							{
								break;
							}
						}
						catch (ConnectionResetException) { break; }
						catch (ConnectionAbortedException) { break; }
						catch (SocketException) { break; }
						catch (OperationCanceledException) { break; }

						ReadBufferResult? readPosition = parseReadBuffer(bufferStream, readResult.Buffer);
						reader.AdvanceTo(readPosition?.Position ?? readResult.Buffer.End);

						if (readPosition == null) { continue; }

						string xmlPayloadString = Encoding.UTF8.GetString(buffer, 0, (int)bufferStream.Length);
						byte[] response = await CreateResponseAsync();

						HttpResult httpResult = new HttpResult(
							HttpStatusCode.OK,
							MediaTypeNames.Application.Xml,
							response
						);

						await SendResponseAsync(
							writer,
							httpResult,
							readPosition.IsHttpRequest,
							closedToken
						);

						bufferStream.Seek(0L, SeekOrigin.Begin);
						bufferStream.SetLength(0);
					}
				}
			}
			catch (ConnectionAbortedException) { }
			catch (Exception ex)
			{
				connection.Abort();

				_logger.LogError(ex, "{0}.  [Client:{1}]", ex.Message, remoteIpAddress.ToString());
			}

			_logger.LogInformation("{0} disconnected", connection.ConnectionId);
		}

		#region Private Methods

		private async Task<byte[]> CreateResponseAsync()
		{
			using (MemoryStream memoryStream = new MemoryStream())
			using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream, _xmlWriterSettings))
			{
				xmlWriter.WriteProcessingInstruction(ProcessingInstructionName, ProcessingInstructionText);
				await xmlWriter.WriteStartElementAsync(null, "acme", null);

				await xmlWriter.WriteStartElementAsync(null, "response", null);

				await xmlWriter.WriteEndElementAsync(); // response

				await xmlWriter.WriteEndElementAsync(); // acme

				await xmlWriter.FlushAsync();
				memoryStream.Seek(0L, SeekOrigin.Begin);

				return memoryStream.ToArray();
			}
		}

		private async Task SendResponseAsync(
			PipeWriter writer,
			HttpResult httpResult,
			bool sendHttpheader,
			CancellationToken closedToken
		)
		{
			//Thread currentThread = Thread.CurrentThread;
			//_logger.LogInformation($"SendResponseAsync-BEGIN ({nameof(backgroundSendServiceAsync)}): {(string.IsNullOrWhiteSpace(currentThread.Name) ? currentThread.ManagedThreadId.ToString() : currentThread.Name)}");

			try
			{
				TcpResponse tcpResponse;

				if (closedToken.IsCancellationRequested)
				{
					return;
				}

				if (sendHttpheader)
				{
					string httpResponseHeader = string.Format(
						HTTP_RESPONSE_HEADER,
						httpResult.HttpStatusCode.ToString("D"),
						HttpStatusDescription.Get(httpResult.HttpStatusCode),
						httpResult.ContentType,
						httpResult.Response.Length
					);

					tcpResponse = new TcpResponse(
						Encoding.UTF8.GetBytes(httpResponseHeader),
						httpResult.Response
					);
				}
				else
				{
					tcpResponse = new TcpResponse(
						null,
						httpResult.Response
					);
				}

				if (closedToken.IsCancellationRequested)
				{
					return;
				}

				int totalBytes = (tcpResponse.Header?.Length ?? 0) + tcpResponse.Body.Length;
				Memory<byte> responseMemory;

				try
				{
					responseMemory = writer.GetMemory(totalBytes);
				}
				catch (InvalidOperationException ex) when ("Writing is not allowed after writer was completed.".Equals(ex.Message, StringComparison.InvariantCultureIgnoreCase))
				{
					_logger.LogWarning("{0} (closed: {1})", ex.Message, closedToken.IsCancellationRequested);
					return;
				}

				if (responseMemory.Length < totalBytes)
				{
					throw new InvalidOperationException($"The memory buffer ({responseMemory.Length}) is smaller than the requested size ({totalBytes}).");
				}

				if (tcpResponse.Header != null)
				{
					tcpResponse.Header.CopyTo(responseMemory);
					responseMemory = responseMemory.Slice(tcpResponse.Header.Length);
				}

				tcpResponse.Body.CopyTo(responseMemory);

				if (closedToken.IsCancellationRequested)
				{
					return;
				}

				writer.Advance(totalBytes);
				await writer.FlushAsync();

#if DEBUG
				_logger.LogDebug(
					"outputMemory {0} bytes ({1} bytes requested)",
					responseMemory.Span.Length,
					totalBytes
				);
#endif
			}
			catch (OperationCanceledException)
			{
				// Perform any cleanup, if needed.
			}
			finally
			{
				//_logger.LogInformation($"SendResponseAsync-END ({nameof(backgroundSendServiceAsync)}): {(string.IsNullOrWhiteSpace(currentThread.Name) ? currentThread.ManagedThreadId.ToString() : currentThread.Name)}");
			}
		}

		#endregion
	}
}
