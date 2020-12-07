using System;
using System.Xml;

namespace WebApplication
{
	/// <summary>
	///		Summary description for XmlAcmeResponseBuildVersion.
	/// </summary>
	public class XmlAcmeResponseBuildVersion : XmlAcmeResponse
	{
		#region Constructors

		/// <summary>
		///		Create an instance of XmlAcmeResponseBuildVersion.
		/// </summary>
		/// <param name="osVersion"></param>
		/// <param name="clrVersion"></param>
		/// <param name="clrRevision"></param>
		/// <param name="assemblies"></param>
		public XmlAcmeResponseBuildVersion(
			string osVersion,
			string clrVersion,
			string? clrRevision,
			XmlAcmeSubResponseAssemblyVersion[] assemblies
		)
		{
			OsVersion = osVersion;
			ClrVersion = clrVersion;
			ClrRevision = clrRevision;
			Assemblies = assemblies;
		}

		#endregion

		#region Non-serialized Properties

		#endregion

		#region Serialized Properties

		/// <summary></summary>
		public XmlAcmeSubResponseAssemblyVersion[] Assemblies { get; set; }

		/// <summary></summary>
		public string? ClrRevision { get; set; }

		/// <summary></summary>
		public string ClrVersion { get; set; }

		/// <summary></summary>
		public string OsVersion { get; set; }

		#endregion

		#region IRenderXml Implementation

		/// <summary>
		///		Render the current instance to the specified <see cref="T:XmlWriter"/>.
		/// </summary>
		/// <param name="writer">The <see cref="T:XmlWriter"/>.</param>
		public override void RenderXml(XmlWriter writer)
		{
			base.RenderStartResponse(writer);

			if (StatusCode == AcmeStatusCode.OK)
			{
				writer.WriteElementString("osversion", OsVersion);
				writer.WriteElementString("clrversion", ClrVersion);
				writer.WriteElementString("clrrevision", ClrRevision);

				if (Assemblies != null)
				{
					writer.WriteStartElement("assemblies");

					foreach (IRenderXml assembly in Assemblies)
					{
						assembly.RenderXml(writer);
					}

					writer.WriteEndElement();// assemblies
				}
			}

			base.RenderEndResponse(writer);
		}

		#endregion
	}
}
