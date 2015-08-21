using System;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using log4net;
using log4net.Config;

namespace MPKService
{

		
	public class Ack
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Ack));
		public enum AckType
		{
			route = 1,
			ping = 2,
			location_update = 3,
			test = 4
		}
		
		private AckType _type;
		public AckType ackType
		{
			get { return _type; }
			set { _type = value; }
		}

		private string _checksum;
		public string CheckSum
		{
			get { return _checksum; }
			set { _checksum = value; }
		}

		public Ack()
		{

		}

		public string BuildAck()
		{
			try
			{
				XmlTextWriter w = new XmlTextWriter(@"C:\temp\ack_" + System.Threading.Thread.CurrentThread.GetHashCode().ToString() + ".xml", Encoding.UTF8);

				w.Formatting = Formatting.None;
				w.WriteStartDocument();
				w.WriteStartElement("ack");
				w.WriteAttributeString("message",ackType.ToString());
				w.WriteStartElement("md5");
				w.WriteString(CheckSum);
				w.WriteEndElement();
				w.WriteStartElement("status");
				w.WriteString("ok");
				w.WriteEndElement();
				w.WriteEndElement(); // </ack>
				w.WriteEndDocument();
				w.Close();
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e.Message);
			}

			// now load the ack.xml document and calculate checksum
			XmlDocument xDoc = new XmlDocument();

			xDoc.Load(@"c:\temp\ack_" + System.Threading.Thread.CurrentThread.GetHashCode().ToString() + ".xml");

			MD5Verifier ver = new MD5Verifier(Encoding.UTF8);
				
			ver.doVerify(xDoc.OuterXml);

			//if (xDoc.OuterXml.IndexOf("ping") == 0)
				log.InfoFormat("Reply: {0}", xDoc.OuterXml + ver.GetCheckSum());

			return xDoc.OuterXml + ver.GetCheckSum() + System.Environment.NewLine;
		}

		public string BuildError()
		{
			try
			{
				XmlTextWriter w = new XmlTextWriter(@"C:\temp\ack_" + System.Threading.Thread.CurrentThread.GetHashCode().ToString() + ".xml", Encoding.UTF8);

				w.Formatting = Formatting.None;
				w.WriteStartDocument();
				w.WriteStartElement("ack");
				w.WriteAttributeString("message",ackType.ToString());
				w.WriteStartElement("md5");
				w.WriteString(CheckSum);
				w.WriteEndElement();
				w.WriteStartElement("status");
				w.WriteString("error");
				w.WriteEndElement();
				w.WriteEndElement(); // </ack>
				w.Close();
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e.Message);
			}

			// now load the ack.xml document and calculate checksum
			XmlDocument xDoc = new XmlDocument();
			xDoc.Load(@"c:\temp\ack_" + System.Threading.Thread.CurrentThread.GetHashCode().ToString() + ".xml");

			MD5Verifier ver = new MD5Verifier(Encoding.UTF8);
				
			ver.doVerify(xDoc.OuterXml);
			log.InfoFormat("Reply: {0}", xDoc.OuterXml + ver.GetCheckSum());
			return xDoc.OuterXml + ver.GetCheckSum();
		}
	}
}
