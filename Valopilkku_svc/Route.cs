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
	/// Summary description for Route.
	/// </summary>
	public class Route
	{
		public ArrayList _nodes = new ArrayList();
		public string RteID;

		public Route(string xmlFragment)
		{
			XmlTextReader xRdr = new XmlTextReader(xmlFragment, XmlNodeType.Element, null);
			XmlDocument xDoc = new XmlDocument();
			xDoc.Load(xRdr);

			Int16 seqNbr = 0;
			foreach (XmlNode stopNode in xDoc.SelectNodes("/SUTI/msg/order/route/node"))
			{
				
				string nodeXml = stopNode.OuterXml;
				XmlTextReader rdrStop = new XmlTextReader(nodeXml, XmlNodeType.Element, null);
				XmlDocument xDocStop = new XmlDocument();
				xDocStop.Load(rdrStop);
				++seqNbr;

				RteNode myRteNode = new RteNode();
				myRteNode.SeqNbr = seqNbr;
				XmlAttributeCollection stopAttr = stopNode.Attributes;
				if ( stopAttr.GetNamedItem("nodeType").InnerXml.ToUpper().Equals("PICKUP") )
					myRteNode.NodeType = 'P';
				else
					myRteNode.NodeType = 'D';

				XmlNode addrNode = xDocStop.SelectSingleNode("/node/addressNode");
				XmlAttributeCollection addrAttr = addrNode.Attributes;
				myRteNode.Street = addrAttr.GetNamedItem("street").InnerXml.ToUpper();
				myRteNode.StreetNbr = addrAttr.GetNamedItem("streetNo").InnerXml.ToUpper();
				if ( addrAttr.GetNamedItem("streetNoLetter") != null )
					myRteNode.StreetNbrLtr = addrAttr.GetNamedItem("streetNoLetter").InnerXml.ToUpper();
				if ( addrAttr.GetNamedItem("community") != null )
					myRteNode.Locality = addrAttr.GetNamedItem("community").InnerXml.ToUpper();
				else
					myRteNode.Locality = String.Empty;

				
				XmlNode timesNode = xDocStop.SelectSingleNode("/node/timesNode/time");
				if (timesNode != null )
				{
					XmlAttributeCollection timeAttr = timesNode.Attributes;

					if ( timeAttr.GetNamedItem("time") != null )
						myRteNode.DueTime = timeAttr.GetNamedItem("time").InnerXml;
				}

				//Get Contents for the Node
				XmlNode passContent = xDocStop.SelectSingleNode("//contactInfo[@contactType='traveller']");
				XmlNode phoneContent = xDocStop.SelectSingleNode("//contactInfo[@contactType='phone']");

				if ( passContent != null )
				{
					NdeContent myContent = new NdeContent();
					XmlAttributeCollection contactAttr = passContent.Attributes;
					myContent.Name = contactAttr.GetNamedItem("contactInfo").InnerXml;

					if ( phoneContent != null )
					{
						contactAttr = phoneContent.Attributes;
						myContent.ContactPhone = contactAttr.GetNamedItem("contactInfo").InnerXml;
						myRteNode.Contents.Add(myContent);
					}

				}



				XmlNode zoneNode = xDocStop.SelectSingleNode("/node/addressNode/geographicLocation");
				if ( zoneNode != null )
				{
					XmlAttributeCollection zoneDescriptAttr = zoneNode.Attributes;
					myRteNode.Zone = zoneDescriptAttr.GetNamedItem("zone").InnerXml;
				}

				XmlNode manualDescriptNode = xDocStop.SelectSingleNode("/node/addressNode/manualDescriptionAddress");
				if ( manualDescriptNode != null )
				{
					XmlAttributeCollection manualDescriptAttr = manualDescriptNode.Attributes;
					myRteNode.Description = manualDescriptAttr.GetNamedItem("manualText").InnerXml;
				}

				XmlNode contactNode = xDocStop.SelectSingleNode("/node/contents/content/contactInfosContent/contactInfo");
				if ( contactNode != null )
				{
					NdeContent myContent = new NdeContent();
					XmlAttributeCollection contactAttr = contactNode.Attributes;
					myContent.ContactPhone = contactAttr.GetNamedItem("contactInfo").InnerXml;
					myRteNode.Contents.Add(myContent);
				}

				_nodes.Add(myRteNode);


			}
		}
	}
}
