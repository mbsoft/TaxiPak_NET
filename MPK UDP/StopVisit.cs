using System;
using System.IO;
using System.Text;
using System.Configuration;
using System.Net;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Collections;
using log4net;
using log4net.Config;

namespace MPKBridge
{
	/// <summary>
	/// Summary description for RouteAccept.
	/// </summary>
	public class StopVisit
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(StopVisit));

		private HttpWebRequest myWebRequestMPK;
		private HttpWebResponse myWebResponseMPK;

		public string StopID;
		public string Accept;
		public string VehicleID;
		public string ArrivalTime;
		public string DepartureTime;
		public string Status;
		public int x;
		public int y;

		public StopVisit()
		{
			log.InfoFormat("RouteAccept WebRequest server {0}", ConfigurationSettings.AppSettings["MPKServer"]);
			myWebRequestMPK = (HttpWebRequest)WebRequest.Create(ConfigurationSettings.AppSettings["MPKServer"]);
			myWebRequestMPK.Timeout = 10000;
		}

		public string Send()
		{
			String result = "";

			myWebRequestMPK.ContentType = "text/xml;charset=\"utf-8\"";
			myWebRequestMPK.Method = "POST";
			XmlTextWriter w;
			try
			{
				Stream reqStream = myWebRequestMPK.GetRequestStream();
				w = new XmlTextWriter(@".\stopvisit.xml",Encoding.UTF8);
				w.Formatting = Formatting.None;
				w.WriteStartDocument();
				w.WriteStartElement("stop_visit");
				w.WriteStartElement("id");
				w.WriteString(this.StopID);
				w.WriteEndElement();
				
				w.WriteStartElement("vehicle");
				w.WriteString(this.VehicleID);
				w.WriteEndElement();
				
				
				w.WriteStartElement("arrival_time");
				w.WriteString(this.ArrivalTime);
				//w.WriteString(this.PriceGroup);
				w.WriteEndElement();
				w.WriteStartElement("departure_time");
				w.WriteString(this.DepartureTime);
				w.WriteEndElement();
				w.WriteStartElement("status");
				w.WriteString(this.Status);
				w.WriteEndElement();
				w.WriteStartElement("location");
				w.WriteAttributeString("x", this.x.ToString());
				w.WriteAttributeString("y", this.y.ToString());
				w.WriteEndElement();
				w.WriteEndElement();
				w.Close();

				XmlDocument xDoc = new XmlDocument();
				xDoc.Load(@".\stopvisit.xml");

				log.InfoFormat("<stop_visit> sent: {0}", xDoc.OuterXml);
				//Console.WriteLine("Sending <stop_visit> msg: {0}",xDoc.OuterXml);

				MD5Verifier ver = new MD5Verifier(Encoding.UTF8);
				ver.doVerify(xDoc.OuterXml);

				StreamWriter sw = new StreamWriter(reqStream);
				sw.Write(xDoc.OuterXml);
				sw.Write(ver.GetCheckSum());
				sw.Close();
			}
			catch (Exception e)
			{
				log.ErrorFormat("Exception raised in <stop_visit> {0}", e.Message);
				//Console.WriteLine("Exception raised: {0}", e.Message);
			}

			try
			{
				myWebResponseMPK = (HttpWebResponse)myWebRequestMPK.GetResponse();
				using (StreamReader sr = new StreamReader(myWebResponseMPK.GetResponseStream()))
				{
					result = sr.ReadToEnd();
					log.InfoFormat("Received server reply: {0}", result);
					//Console.WriteLine("Received reply: {0}", result);
					return(result);
				}
			}
			catch (WebException e)
			{
				if ( e.Status == WebExceptionStatus.Timeout )
				{
					log.Error("Timeout error awaiting StopVisit response from server");
					return null;
				}

				log.ErrorFormat("Error with server reply: {0}", e.Message);
				myWebResponseMPK = (HttpWebResponse)e.Response;
				if ( myWebResponseMPK != null )
				{
					using (StreamReader sr = new StreamReader(myWebResponseMPK.GetResponseStream()))
					{
						result = sr.ReadToEnd();
						log.InfoFormat("{0}", result);
					}
				}
				//Console.WriteLine("Server error {0}", e.Message);
				return e.Message;
			}
		}

			
	}
}


