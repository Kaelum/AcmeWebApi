using System;
using System.Reflection;
using System.Xml;

namespace WebApplication
{
	/// <summary>
	///		Summary description for XmlAcmeSubResponseAssemblyVersion.
	/// </summary>
	public class XmlAcmeSubResponseAssemblyVersion : IRenderXml
	{
		#region Constructors

		/// <summary>
		///		Create an instance of XmlAcmeSubResponseAssemblyVersion.
		/// </summary>
		public XmlAcmeSubResponseAssemblyVersion(Assembly assembly)
		{
			AssemblyName assemblyName = assembly.GetName();

			Name = assemblyName.Name ?? "[Unknown]";
			Version = assemblyName.Version?.ToString() ?? "[Unknown]";
		}

		#endregion

		#region Serialized Properties

		/// <summary></summary>
		public string Name { get; set; }

		/// <summary></summary>
		public string Version { get; set; }

		#endregion

		#region IRenderXml Implementation

		/// <summary>
		///		Render the current instance to the specified <see cref="T:XmlWriter"/>.
		/// </summary>
		/// <param name="writer">The <see cref="T:XmlWriter"/>.</param>
		public void RenderXml(XmlWriter writer)
		{
			writer.WriteStartElement("assembly");

			writer.WriteElementString("name", Name);
			writer.WriteElementString("version", Version);

			writer.WriteEndElement();// assembly
		}

		#endregion
	}
}
