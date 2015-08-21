using System;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Configuration;

namespace SUTI_svc
{
	/// <summary>
	/// Summary description for Preamble.
	/// </summary>
	public class Preamble
	{
		public Preamble()
		{

		}

		public void FromLocal(ref XmlTextWriter w)
		{
			w.WriteStartElement("orgSender");
			w.WriteAttributeString("name",System.Configuration.ConfigurationSettings.AppSettings["localOrgName"]);
			w.WriteStartElement("idOrg");
			w.WriteAttributeString("src","SUTI");
			w.WriteAttributeString("id",System.Configuration.ConfigurationSettings.AppSettings["localOrgID"]);
			w.WriteAttributeString("unique","true");
			w.WriteEndElement(); //</idOrg>
			w.WriteEndElement(); //</orgSender>

			w.WriteStartElement("orgReceiver");
			w.WriteAttributeString("name",System.Configuration.ConfigurationSettings.AppSettings["remoteOrgName"]);
			w.WriteStartElement("idOrg");
			w.WriteAttributeString("src","SUTI");
			w.WriteAttributeString("id",System.Configuration.ConfigurationSettings.AppSettings["remoteOrgID"]);
			w.WriteAttributeString("unique","true");
			w.WriteEndElement(); //</idOrg>
			w.WriteEndElement(); //</orgReceiver>

		}

		public string GetRemoteName()
		{
			return (System.Configuration.ConfigurationSettings.AppSettings["remoteOrgID"]);
		}

		public string GetLocalName()
		{
			return (System.Configuration.ConfigurationSettings.AppSettings["localOrgID"]);
		}

	}
}
