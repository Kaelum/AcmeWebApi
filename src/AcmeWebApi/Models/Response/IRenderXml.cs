using System;
using System.Xml;

namespace WebApplication
{
	/// <summary>
	///		Summary description for IRenderXml.
	/// </summary>
	public interface IRenderXml
	{
		/// <summary>
		///		Render the current instance to the specified <see cref="T:XmlWriter"/>.
		/// </summary>
		/// <param name="writer">The <see cref="T:XmlWriter"/>.</param>
		void RenderXml(XmlWriter writer);
	}
}
