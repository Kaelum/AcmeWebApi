using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebApplication
{
	/// <summary>
	///		Summary description for AcmeSerialization.
	/// </summary>
	public class AcmeSerialization
	{
		/// <summary>The Acme XML processing instruction name.</summary>
		public const string ProcessingInstructionName = "Acme";
		/// <summary>The Acme XML processing instruction text.</summary>
		public const string ProcessingInstructionText = "version=acme/1.1";

		private readonly ILogger _logger;
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
		};


		#region Constructors

		/// <summary>
		///		Create an instance of .
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="applicationSettings"></param>
		[ActivatorUtilitiesConstructor]
		public AcmeSerialization(
			ILogger<AcmeSerialization> logger,
			ApplicationSettings applicationSettings
		)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));

			//if (_applicationSettings.Options?.BeautifyXml ?? false)
			//{
			//	_xmlWriterSettings.Indent = true;
			//	_xmlWriterSettings.IndentChars = "\t";
			//}
		}

		#endregion

		#region Create

		/// <summary>
		///		Create a <see cref="T:AcmePostRequest"/> from the specified XML payload.
		/// </summary>
		/// <param name="serviceType"></param>
		/// <param name="remoteIpAddress"></param>
		/// <param name="xmlPayloadString"></param>
		/// <returns></returns>
		public AcmeRequest? CreatePostRequest(
			string serviceType,
			IPAddress remoteIpAddress,
			string xmlPayloadString
		)
		{
			XmlRequestAcme requestPayload;
			int? seqNum = null;
			AcmeAuthorization? authorization = null;

			try
			{
				if (!ApiProcessor.IsReady)
				{
					throw new AcmeException(
						seqNum,
						AcmeStatusCode.ServerNotAvailable
					);
				}

				if (string.IsNullOrEmpty(xmlPayloadString)) { return null; }

				using (StringReader reader = new StringReader(xmlPayloadString))
				{
					requestPayload = (XmlRequestAcme)XmlRequestAcme.XmlSerializer.Deserialize(reader);
				}

				// Only the sequence number and encrypt type are guaranteed to be available here.
				if (int.TryParse(requestPayload.SeqNum, out int seqNumTemp))
				{
					seqNum = seqNumTemp;
				}
			}
			catch (Exception ex)
			{
				throw new AcmeException(
					seqNum,
					authorization,
					null,
					AcmeStatusCode.MalformedXml,
					$"Failure deserializing a request from host:\n-------------------\n{xmlPayloadString}\n-------------------",
					ex
				);
			}

			try
			{
				if (requestPayload.Request == null)
				{
					throw new AcmeException(
						seqNum,
						AcmeStatusCode.MalformedXml,
						$"<encrypt-type> is none, and <request> is empty:\n-------------------\n{xmlPayloadString}\n-------------------"
					);
				}

				// The credentials are now guaranteed to be available.
				authorization = new AcmeAuthorization(
					requestPayload.Request.OemId,
					requestPayload.Request.ProductId,
					requestPayload.Request.UniqueId
				);

				return new AcmeRequest(
					serviceType,
					remoteIpAddress,
					seqNum,
					authorization,
					xmlPayloadString,
					requestPayload.Request
				);
			}
			catch (AcmeException ex)
			{
				ex.SeqNum = seqNum;
				ex.Authorization = authorization;
				ex.RawMethod = requestPayload.Request?.Method;
				throw;
			}
			catch (Exception ex)
			{
				throw new AcmeException(
					seqNum,
					authorization,
					requestPayload.Request?.Method,
					AcmeStatusCode.InternalServerError,
					$"{ex.Message}:\n-------------------\n{xmlPayloadString}\n-------------------",
					ex
				);
			}
		}

		#endregion

		/// <summary>
		///		Create a <see cref="T:XmlWriter"/> for ACME response serialization.
		/// </summary>
		/// <param name="output">The <see cref="T:TextWriter"/> to which you want to write. The
		///		<see cref="T:XmlWriter"/> writes XML 1.0 text syntax and appends it to the specified
		///		<see cref="T:TextWriter"/>.</param>
		/// <returns>
		///		An initialized <see cref="T:XmlWriter"/> for ACME response serialization.
		/// </returns>
		public static XmlWriter CreateAcmeXmlWriter(Stream output)
		{
			return XmlWriter.Create(output, _xmlWriterSettings);
		}

		/// <summary>
		///		Create a <see cref="T:XmlWriter"/> for ACME response serialization.
		/// </summary>
		/// <param name="output">The <see cref="T:TextWriter"/> to which you want to write. The
		///		<see cref="T:XmlWriter"/> writes XML 1.0 text syntax and appends it to the specified
		///		<see cref="T:TextWriter"/>.</param>
		/// <returns>
		///		An initialized <see cref="T:XmlWriter"/> for ACME response serialization.
		/// </returns>
		public static XmlWriter CreateAcmeXmlWriter(TextWriter output)
		{
			return XmlWriter.Create(output, _xmlWriterSettings);
		}

		/// <summary>
		///		Finish a <see cref="T:XmlWriter"/> with the ACME closing elements.
		/// </summary>
		/// <param name="xmlWriter"></param>
		public static void FinishAcmeXmlWriter(XmlWriter xmlWriter)
		{
			xmlWriter.WriteEndElement();// "acme"

			xmlWriter.Flush();
		}

		/// <summary>
		///		Initialize a <see cref="T:XmlWriter"/> with the ACME headers and opening elements.
		/// </summary>
		/// <param name="xmlWriter"></param>
		/// <param name="seqNum">The sequence number.</param>
		public static void InitializeAcmeXmlWriter(XmlWriter xmlWriter, int? seqNum)
		{
			xmlWriter.WriteProcessingInstruction(ProcessingInstructionName, ProcessingInstructionText);
			xmlWriter.WriteStartElement("acme");

			if (seqNum.HasValue)
			{
				xmlWriter.WriteElementString("seqnum", seqNum.Value.ToString("G"));
			}
		}

		/// <summary>
		///		SerializeAcmeResponse
		/// </summary>
		/// <param name="xmlWriter"></param>
		/// <param name="input"></param>
		public static void SerializeAcmeResponse(XmlWriter xmlWriter, IRenderXml input)
		{
			input.RenderXml(xmlWriter);
		}
	}
}
