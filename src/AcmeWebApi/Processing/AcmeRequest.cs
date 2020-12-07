using System;
using System.Net;

namespace WebApplication
{
	/// <summary>
	///		Summary description for AcmeRequest.
	/// </summary>
	public class AcmeRequest
	{
		#region Constructors

		/// <summary>
		///		Creates an instance of AcmeRequest from the specified <see cref="T:XmlRequestAcme"/>
		///		object.
		/// </summary>
		/// <param name="serviceType"></param>
		/// <param name="remoteIpAddress"></param>
		/// <param name="seqNum"></param>
		/// <param name="authorization"></param>
		/// <param name="rawRequest"></param>
		/// <param name="request"></param>
		public AcmeRequest(
			string serviceType,
			IPAddress remoteIpAddress,
			int? seqNum,
			AcmeAuthorization authorization,
			string rawRequest,
			XmlRequestAcmeRequest request
		)
		{
			ServiceType = serviceType;
			RemoteIpAddress = remoteIpAddress;
			SeqNum = seqNum;
			Authorization = authorization;

			RawRequest = rawRequest;
			RawMethod = request.Method;

			Method = (Enum.TryParse(RawMethod, true, out AcmePostMethod acmeMethod) ? acmeMethod : (AcmePostMethod?)null);
			Uri = request.Uri;
			Keys = request.Keys;
			Limit = (int.TryParse(request.Limit, out int limit) ? (int?)limit : null);
		}

		#endregion

		#region Public Properties

		/// <summary>Gets the authorization.</summary>
		public AcmeAuthorization Authorization { get; protected set; }

		/// <summary>The array of keys to lookup.</summary>
		public string[]? Keys { get; protected set; }

		/// <summary>Gets the maximum number of items to return.</summary>
		public int? Limit { get; protected set; }

		/// <summary>Gets the parsed ACME method.</summary>
		public AcmePostMethod? Method { get; private set; }

		/// <summary>Gets the raw ACME method.</summary>
		public string? RawMethod { get; protected set; }

		/// <summary>Gets the raw request string.</summary>
		public string RawRequest { get; protected set; }

		/// <summary>Gets the unique identifier for the request (sent by client).</summary>
		public IPAddress RemoteIpAddress { get; protected set; }

		/// <summary>Gets the request type ("rest" or "stream").</summary>
		public string ServiceType { get; protected set; }

		/// <summary>Gets the unique identifier for the request (sent by client).</summary>
		public int? SeqNum { get; protected set; }

		/// <summary>The array of URIs to process.</summary>
		public string? Uri { get; protected set; }

		#endregion
	}
}
