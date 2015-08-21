using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;

namespace SUTI_svc
{
	/// <summary>
	/// Summary description for Economy.
	/// </summary>
	public class Economy
	{
		private bool _fixed_price;
		public bool FixedPrice
		{
			get { return _fixed_price; }
			set { _fixed_price = value; }
		}
		private string _price;
		public string Price
		{
			get { return _price; }
			set { _price = value; }
		}
		private string _vat_percent;
		public string VatPercent
		{
			get { return _vat_percent; }
			set { _vat_percent = value; }
		}
		private bool _vat_incl;
		public bool VatIncl
		{
			get { return _vat_incl; }
			set { _vat_incl = value; }
		}

		public Economy(string xmlFragment)
		{
			XmlTextReader xRdr = new XmlTextReader(xmlFragment, XmlNodeType.Element, null);
			XmlDocument xDoc = new XmlDocument();
			XmlNode idEconNode;
			XmlAttributeCollection xAttr;

			try
			{
				xDoc.Load(xRdr);
			}
			catch (Exception exc)
			{
				Console.WriteLine(exc.Message);
			}
			idEconNode = xDoc.SelectSingleNode("/SUTI/msg/order/economyOrder/price");
			if ( idEconNode != null )
			{
				xAttr = idEconNode.Attributes;
				Price = xAttr.GetNamedItem("price").InnerXml;
				VatPercent = xAttr.GetNamedItem("vatPercent").InnerXml;
				VatIncl = xAttr.GetNamedItem("vatIncluded").InnerXml.ToUpper().Equals("TRUE");
				FixedPrice = xAttr.GetNamedItem("fixedPrice").InnerXml.ToUpper().Equals("TRUE");
			}
				
		}
	}
}
