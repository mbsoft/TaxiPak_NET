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
	/// Summary description for Ping.
	/// </summary>
	public class Ping
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Ping));
		private HttpWebRequest myWebRequestMPK;
		private HttpWebResponse myWebResponseMPK;
		public Ping()
		{
			log.InfoFormat("RouteAccept WebRequest server {0}", ConfigurationSettings.AppSettings["MPKServer"]);
			myWebRequestMPK = (HttpWebRequest)WebRequest.Create(ConfigurationSettings.AppSettings["MPKServer"]);
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
				w = new XmlTextWriter(@".\ping.xml",Encoding.UTF8);
				w.Formatting = Formatting.None;
				w.WriteStartDocument();
				w.WriteStartElement("ping");
				w.WriteEndElement();
				w.Close();

				XmlDocument xDoc = new XmlDocument();
				xDoc.Load(@".\ping.xml");

				log.InfoFormat("<ping> sent: {0}", xDoc.OuterXml);
				Console.WriteLine("Sending <ping> msg: {0}",xDoc.OuterXml);

				MD5Verifier ver = new MD5Verifier(Encoding.UTF8);
				ver.doVerify(xDoc.OuterXml);

				StreamWriter sw = new StreamWriter(reqStream);
				sw.Write(xDoc.OuterXml);
				sw.Write(ver.GetCheckSum());
				sw.Close();
			}
			catch (Exception e)
			{
				log.ErrorFormat("Exception raised in <ping> {0}", e.Message);
				Console.WriteLine("Exception raised: {0}", e.Message);
			}

			try
			{
				myWebResponseMPK = (HttpWebResponse)myWebRequestMPK.GetResponse();
				using (StreamReader sr = new StreamReader(myWebResponseMPK.GetResponseStream()))
				{
					result = sr.ReadToEnd();
					log.InfoFormat("Received server reply: {0}", result);
					Console.WriteLine("Received reply: {0}", result);
					return(result);
				}
			}
			catch (WebException e)
			{
				log.ErrorFormat("Error with server reply: {0}", e.Message);
				myWebResponseMPK = (HttpWebResponse)e.Response;
				using (StreamReader sr = new StreamReader(myWebResponseMPK.GetResponseStream()))
				{
					result = sr.ReadToEnd();
					log.InfoFormat("{0}", result);
				}
				Console.WriteLine("Server error {0}", e.Message);
				return e.Message;
			}
		}
		


	}
}
