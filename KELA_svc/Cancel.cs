using System;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.IO;
using PI_Lib;
using System.Data;
using System.Data.Odbc;
using System.Configuration;
using log4net;
using log4net.Config;

namespace SUTI_svc
{
	/// <summary>
	/// Summary description for Cancel.
	/// </summary>
	public class Cancel
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Cancel));
		private string reply;
		private string sSrc;
		private string sID;
		private int msgCount;
		private string sVehicleID;
		private string sOrderID;
		private string sTPakNbr;
		PI_Lib.PIClient myPISocket;

		public Cancel(XmlNode confirmNode, int msgCounter, XmlDocument xDoc)
		{
			XmlAttributeCollection msgAttr = confirmNode.Attributes;
			sSrc = msgAttr.GetNamedItem("src").InnerXml;
			sID  = msgAttr.GetNamedItem("id").InnerXml;
			msgCount = msgCounter;

			XmlNode idOrder = xDoc.SelectSingleNode("/SUTI/msg/referencesTo/idOrder");
			if ( idOrder != null )
			{
				XmlAttributeCollection idOrderAttr = idOrder.Attributes;
				sOrderID = idOrderAttr.GetNamedItem("id").InnerXml;
			}

			if ( sOrderID.Length > 0 ) // cancel the order in TaxiPak
			{
				// Cancel trip using PI interface

				if ( System.Configuration.ConfigurationSettings.AppSettings["TPak_dispatch"].Equals("YES") )
				{
					log.InfoFormat("<-- initiating socket connect");
					try
					{
						myPISocket = new PI_Lib.PIClient();
						log.InfoFormat("<-- Successful PI socket connection");
					}
					catch (System.Net.Sockets.SocketException ex)
					{
						log.InfoFormat("Error on PI socket ({0})", ex.Message);
						return;
					}
					myPISocket.SetType(MessageTypes.PI_CANCEL_CALL);
					PI_CANCEL_CALL myCall = new PI_CANCEL_CALL();
					
					myPISocket.sendBuf = myCall.ToByteArray(Int32.Parse(sOrderID));
					
					try
					{
						log.InfoFormat("<-- Starting PI Socket SEND");
						myPISocket.SendMessage();
						log.InfoFormat("<-- Done with PI Socket SEND");
						log.InfoFormat("<-- Starting PI Socket RECV");
						myPISocket.ReceiveMessage();
						log.InfoFormat("<-- Done with PI Socket RECV");

						myCall.Deserialize(myPISocket.recvBuf);
						myPISocket.CloseMe();
						log.InfoFormat("<-- success send PI socket");
					}
					catch
					{
						log.InfoFormat("<--- error on PI socket send");
						return;
					}
				}
			}

		}

		public string ReplyCancel()
		{
			XmlTextWriter w = new XmlTextWriter(@"C:\temp\ack2.xml", Encoding.GetEncoding("iso-8859-15"));
			Preamble preamb = new Preamble();
			w.Formatting = Formatting.None;
			w.WriteStartDocument();
			w.WriteStartElement("SUTI");

			//Preamble preamb = new Preamble();
			//preamb.FromLocal(ref w);

			w.WriteStartElement("msg");
			w.WriteAttributeString("msgType", "2011");
			w.WriteAttributeString("msgName", "Order Cancellation Accepted");
			w.WriteStartElement("idMsg");
			w.WriteAttributeString("src", "mbsoft_htd_001");
			w.WriteAttributeString("id", msgCount.ToString());
			w.WriteEndElement(); //</idMsg>
			w.WriteStartElement("referencesTo");
			w.WriteStartElement("idOrder");
			w.WriteAttributeString("src", preamb.GetRemoteName());
			w.WriteAttributeString("id", this.sOrderID);
			w.WriteEndElement(); // </idOrder>
			w.WriteStartElement("idOrder");
			w.WriteAttributeString("src", preamb.GetLocalName());
			w.WriteAttributeString("id", this.sTPakNbr);
			w.WriteEndElement(); // </idOrder>
			w.WriteEndElement(); // </referencesTo>
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
