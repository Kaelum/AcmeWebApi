using System;
using System.Xml;

namespace WebApplication
{
	/// <summary>
	///		Summary description for XmlAcmeResponseUriInfo.
	/// </summary>
	public class XmlAcmeResponseUriInfo : XmlAcmeResponse
	{
		private const string NO_KEYS_MATCHED = "";


		#region Constructors

		/// <summary>
		///		Create an instance of XmlAcmeResponseUriInfo.
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="normalizedUri"></param>
		/// <param name="lcp"></param>
		/// <param name="match"></param>
		/// <param name="categories"></param>
		/// <param name="bcri"></param>
		/// <param name="all1Category"></param>
		/// <param name="rtap"></param>
		/// <param name="keysMatched"></param>
		public XmlAcmeResponseUriInfo(
			string uri,
			string normalizedUri,
			string lcp,
			string match,
			XmlAcmeSubResponseCatConf[]? categories,
			int bcri,
			bool all1Category,
			bool rtap,
			string? keysMatched
		)
		{
			Uri = uri;
			NormalizedUri = normalizedUri;
			Lcp = lcp;
			Match = match;
			Categories = categories;
			Bcri = bcri;
			All1Category = all1Category;
			Rtap = rtap;
			KeysMatched = keysMatched ?? NO_KEYS_MATCHED;
		}

		#endregion

		#region Serialized Properties

		/// <summary></summary>
		public bool All1Category { get; set; }

		/// <summary></summary>
		public int Bcri { get; set; }

		/// <summary></summary>
		public XmlAcmeSubResponseCatConf[]? Categories { get; set; }

		/// <summary></summary>
		public string KeysMatched { get; set; }

		/// <summary></summary>
		public string Lcp { get; set; }

		/// <summary></summary>
		public string Match { get; set; }

		/// <summary></summary>
		public string NormalizedUri { get; set; }

		/// <summary></summary>
		public bool Rtap { get; set; }

		/// <summary></summary>
		public string? Uri { get; set; }

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
				writer.WriteElementString("uri", Uri);
				writer.WriteElementString("normalizedUri", NormalizedUri);
				writer.WriteElementString("lcp", Lcp);
				writer.WriteElementString("match", Match);
				writer.WriteElementString("keysMatched", KeysMatched);

				if (Categories != null)
				{
					writer.WriteStartElement("categories");

					foreach (IRenderXml category in Categories)
					{
						category.RenderXml(writer);
					}

					writer.WriteEndElement();// categories
				}

				writer.WriteElementString("bcri", Bcri.ToString());
				writer.WriteElementString("a1cat", All1Category.ToOneZeroString());
				writer.WriteElementString("rtap", Rtap.ToOneZeroString());
			}

			base.RenderEndResponse(writer);
		}

		#endregion
	}
}
