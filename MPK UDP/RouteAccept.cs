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
	public class RouteAccept
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(RouteAccept));
		private HttpWebRequest myWebRequestMPK;
		private HttpWebResponse myWebResponseMPK;

		public string RouteID;
		public string Accept;
		public string VehicleID;
		public string VehPax;
		public string VehWheels;
		public string PriceGroup;
		public string CompanyID;
		public string Version;

		public RouteAccept()
		{
			log.Info(String.Format("RouteAccept WebRequest server {0}", ConfigurationSettings.AppSettings["MPKServer"]));

			myWebRequestMPK = (HttpWebRequest)WebRequest.Create(ConfigurationSettings.AppSettings["MPKServer"]);
			myWebRequestMPK.Timeout = 10000; // 10000 milliseconds timeout to await responses

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
				w = new XmlTextWriter("routeaccept.xml",Encoding.UTF8);
				w.Formatting = Formatting.None;
				w.WriteStartDocument();
				w.WriteStartElement("route_accept");
				w.WriteStartElement("id");
				w.WriteString(this.RouteID);
				w.WriteEndElement();
				w.WriteStartElement("version");
				w.WriteString(this.Version);
				w.WriteEndElement();
				w.WriteStartElement("accept");
				w.WriteString(this.Accept);
				w.WriteEndElement();
				w.WriteStartElement("vehicle");
				w.WriteString(this.VehicleID);
				w.WriteEndElement();
				if ( this.VehicleID != null )
				{
					w.WriteStartElement("vehicle_capacity");
					w.WriteStartElement("passengers");
					w.WriteString(this.VehPax);
					w.WriteEndElement();
					w.WriteStartElement("wheelchairs");
					w.WriteString(this.VehWheels);
					w.WriteEndElement();
					w.WriteEndElement();
				}
				w.WriteStartElement("price_group");
				w.WriteString("1");
				//w.WriteString(this.PriceGroup);
				w.WriteEndElement();
				w.WriteStartElement("company_id");
				w.WriteString(this.CompanyID);
				w.WriteEndElement();
				w.WriteEndElement();
				w.Close();

				XmlDocument xDoc = new XmlDocument();
				xDoc.Load("routeaccept.xml");

				log.Info(String.Format("<route_accept> sent: {0}", xDoc.OuterXml));
				//Console.WriteLine("Sending <route_accept> msg: {0}",xDoc.OuterXml);

				MD5Verifier ver = new MD5Verifier(Encoding.UTF8);
				ver.doVerify(xDoc.OuterXml);

				StreamWriter sw = new StreamWriter(reqStream);
				sw.Write(xDoc.OuterXml);
				sw.Write(ver.GetCheckSum());
				sw.Close();
			}
			catch (WebException e)
			{
				log.ErrorFormat("Timeout error awaiting route_accept response from server {0}", e.Message);
				return null;
			}
			catch (Exception e)
			{
				log.Error(String.Format("Error formatting <route_accept> {0}", e.Message));
				//Console.WriteLine("Exception raised: {0}", e.Message);
			}

			try
			{
				myWebResponseMPK = (HttpWebResponse)myWebRequestMPK.GetResponse();
				using (StreamReader sr = new StreamReader(myWebResponseMPK.GetResponseStream()))
				{
					result = sr.ReadToEnd();
					log.Info(String.Format("MPK Server reply: {0}", result));
					//Console.WriteLine("Received reply: {0}", result);
					return(result);
				}
			}
			catch (WebException e)
			{
				if ( e.Status == WebExceptionStatus.Timeout )
				{
					log.Error("Timeout error awaiting route_accept response from server");
					return null;
				}
				log.Error(String.Format("MPK Server returned error: {0}", e.Message));
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
				return null;
			}
		}

			
	}
}
