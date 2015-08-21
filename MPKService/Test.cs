using System;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;

namespace MPKService
{
	/// <summary>
	/// Summary description for Test.
	/// </summary>
	public class Test
	{

		private Ack reply;

		public Test(XmlNode pingNode, string checksum)
		{
			reply = new Ack();
			reply.ackType = MPKService.Ack.AckType.test;
			reply.CheckSum = checksum;

		}

		public string ReplyTest()
		{
			try
			{
				XmlTextWriter w = new XmlTextWriter(@"C:\temp\ack.xml", Encoding.UTF8);

				w.Formatting = Formatting.None;
				w.WriteStartDocument();
				w.WriteStartElement("ack");
				w.WriteAttributeString("message","route");
				w.WriteStartElement("md5");
				w.WriteString(reply.CheckSum);
				w.WriteEndElement();
				w.WriteStartElement("status");
				w.WriteString("offer_expired");
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
			xDoc.Load(@"c:\temp\ack.xml");

			MD5Verifier ver = new MD5Verifier(Encoding.UTF8);
				
			ver.doVerify(xDoc.OuterXml);
			return xDoc.OuterXml + ver.GetCheckSum();
		}
	}
}
