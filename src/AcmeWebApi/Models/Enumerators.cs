using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace WebApplication
{
	/// <summary>
	///		Summary description for AcmePostMethod.
	/// </summary>
	[Serializable]
	public enum AcmePostMethod
	{
		/// <summary></summary>
		[XmlEnum("auth")]
		[Description("Authorization")]
		Auth,

		/// <summary></summary>
		[XmlEnum("buildversion")]
		[Description("BuildVersion")]
		BuildVersion,

		/// <summary></summary>
		[XmlEnum("uriinfo")]
		[Description("UriInfo")]
		UriInfo,
	}

	/// <summary>
	///		Summary description for AcmeStatusCode.
	/// </summary>
	[Serializable]
	public enum AcmeStatusCode
	{
		/// <summary>The status code has not been set.</summary>
		[Description("Undefined")]
		Undefined = 0,

		/// <summary>OK</summary>
		[Description("OK")]
		OK = 200,

		/// <summary>Bad Request</summary>
		[Description("Bad Request")]
		BadRequest = 400,

		/// <summary>Unauthorized</summary>
		[Description("Unauthorized")]
		Unauthorized = 401,

		/// <summary>Unsupported Protocol</summary>
		[Description("Unsupported Protocol")]
		UnsupportedProtocol = 402,

		/// <summary>Malformed URL</summary>
		[Description("Malformed URL")]
		MalformedUrl = 403,

		/// <summary>Malformed XML</summary>
		[Description("Malformed XML")]
		MalformedXml = 405,

		/// <summary>Request Timeout</summary>
		[Description("Request Timeout")]
		RequestTimeout = 408,

		/// <summary>Invalid Method</summary>
		[Description("Invalid Method")]
		InvalidMethod = 409,

		/// <summary>License In Use</summary>
		[Description("License In Use")]
		LicenseInUse = 497,

		/// <summary>License Exceeded</summary>
		[Description("License Exceeded")]
		LicenseExceeded = 499,

		/// <summary>Internal Server Error</summary>
		[Description("Internal Server Error")]
		InternalServerError = 500,

		/// <summary>Not Implemented</summary>
		[Description("Not Implemented")]
		NotImplemented = 501,

		/// <summary>Server Not Available</summary>
		[Description("Server Not Available")]
		ServerNotAvailable = 503,

		/// <summary>Invalid State Condition</summary>
		[Description("Invalid State Condition")]
		InvalidStateCondition = 551,

		/// <summary>Server Busy</summary>
		[Description("Server Busy")]
		ServerBusy = 590,

		/// <summary>Unsupported ACME Version</summary>
		[Description("Unsupported ACME Version")]
		UnsupportedAcmeVersion = 591,

		/// <summary>Invalid OEM</summary>
		[Description("Invalid OEM")]
		InvalidOem = 601,

		/// <summary>Invalid Product</summary>
		[Description("Invalid Product")]
		InvalidProduct = 602,

		/// <summary>License Expired</summary>
		[Description("License Expired")]
		LicenseExpired = 603,

		/// <summary>License Not Supported</summary>
		[Description("License Not Supported")]
		LicenseNotSupported = 604,

		/// <summary>Invalid OEM Id</summary>
		[Description("Invalid OEM Id")]
		InvalidOemId = 605,

		/// <summary>Invalid Device Id</summary>
		[Description("Invalid Device Id")]
		InvalidDeviceId = 606,

		/// <summary>Inactive Account</summary>
		[Description("Inactive Account")]
		InactiveAccount = 607,

		/// <summary></summary>
		[Description("Invalid UID Format")]
		InvalidUniqueIdFormat = 608,

		/// <summary>No update available</summary>
		[Description("No update available")]
		NoUpdateAvailable = 701,

		/// <summary>Invalid major version</summary>
		[Description("Invalid major version")]
		InvalidMajorVersion = 702,

		/// <summary>Invalid minor version</summary>
		[Description("Invalid minor version")]
		InvalidMinorVersion = 703,

		/// <summary>Invalid major or minor version</summary>
		[Description("Invalid major or minor version")]
		InvalidMajorOrMinorVersion = 704,

		/// <summary>Invalid RTU Version</summary>
		[Description("Invalid RTU Version")]
		InvalidRtuVersion = 705,

		/// <summary>No RTU available</summary>
		[Description("No RTU available")]
		NoRtuAvailable = 706,

		/// <summary>No new GEO available</summary>
		[Description("No new GEO available")]
		NoNewGeoAvailable = 707,
	}
}
