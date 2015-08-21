using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using log4net;
using log4net.Config;

namespace SUTI_svc
{
	/// <summary>
	/// Summary description for SyntaxError.
	/// </summary>
	public class SyntaxError
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(SyntaxError));
		private string ErrorMsg;
		private string sID;
		private int msgCount;

		public SyntaxError(string sourceID, string errMsg, int msgCounter)
		{
			ErrorMsg = errMsg;
			sID = sourceID;
			msgCount = msgCounter;
		}

		public string ReplySyntaxError()
		{
			XmlTextWriter w = new XmlTextWriter(@"C:\temp\ack2.xml", Encoding.GetEncoding("iso-8859-15"));

			w.Formatting = Formatting.None;
			w.WriteStartDocument();
			w.WriteStartElement("SUTI");

			Preamble preamb = new Preamble();

			w.WriteStartElement("msg");
			w.WriteAttributeString("msgType", "7032");
			w.WriteAttributeString("msgName", this.ErrorMsg);
			w.WriteStartElement("idMsg");
			w.WriteAttributeString("src", preamb.GetLocalName());
			w.WriteAttributeString("id", msgCount.ToString());
			w.WriteEndElement(); //</idMsg>
			w.WriteStartElement("referencesTo");
			w.WriteStartElement("idMsg");
			w.WriteAttributeString("src",preamb.GetRemoteName());
			w.WriteAttributeString("id", this.sID);
			w.WriteEndElement(); //</idMsg>
			w.WriteEndElement(); //</referencesTo>
			w.WriteEndElement(); //</msg>

			w.WriteEndElement(); //</SUTI>

			w.Close();

			// now read the formatted xml doc and send
			// to server with checksum attached
			XmlDocument xDoc = new XmlDocument();
			xDoc.Load(@"C:\temp\ack2.xml");

			log.InfoFormat("<-- {0}", xDoc.OuterXml);
			return xDoc.OuterXml;

		}
	}
}
