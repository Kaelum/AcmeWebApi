using System;
using System.Xml;

namespace WebApplication
{
	/// <summary>
	///		Summary description for XmlAcmeSubResponseCatConf.
	/// </summary>
	public class XmlAcmeSubResponseCatConf : IRenderXml
	{
		#region Constructors

		/// <summary>
		///		Creates an instance of XmlAcmeSubResponseCatConf.
		/// </summary>
		/// <param name="categoryId"></param>
		/// <param name="confidence"></param>
		public XmlAcmeSubResponseCatConf(int categoryId, int? confidence)
		{
			CategoryId = categoryId;
			Confidence = confidence;
		}

		#endregion

		/// <summary></summary>
		public int CategoryId { get; set; }

		/// <summary></summary>
		public int? Confidence { get; set; }

		#region IRenderXml Implementation

		/// <summary>
		///		Render the current instance to the specified <see cref="T:XmlWriter"/>.
		/// </summary>
		/// <param name="writer">The <see cref="T:XmlWriter"/>.</param>
		public void RenderXml(XmlWriter writer)
		{
			writer.WriteStartElement("cat");

			writer.WriteElementString("catid", CategoryId.ToString());

			if (Confidence.HasValue)
			{
				writer.WriteElementString("conf", Confidence.Value.ToString());
			}

			writer.WriteEndElement();// cat
		}

		#endregion
	}
}
