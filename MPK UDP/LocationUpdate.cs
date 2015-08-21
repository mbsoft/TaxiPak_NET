using System;
using System.IO;
using System.Text;
using System.Configuration;
using System.Net;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Collections;
using IBM.Data.Informix;
using log4net;
using log4net.Config;

namespace MPKBridge
{
	/// <summary>
	/// Summary description for RouteAccept.
	/// </summary>
	public class LocationUpdate
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(LocationUpdate));
		private HttpWebRequest myWebRequestMPK;
		private HttpWebResponse myWebResponseMPK;

		public string VehicleID;
		public string RouteID;
		public string LocationTime;
		public double x;
		public double y;

		public LocationUpdate()
		{
			myWebRequestMPK = (HttpWebRequest)WebRequest.Create(ConfigurationSettings.AppSettings["MPKServer"]);
		}

		public void GetPosition( out int x, out int y )
		{
			x = 0;y = 0;
			string strConn = ConfigurationSettings.AppSettings.Get("MadsConnect");
			IfxConnection conn = new IfxConnection(strConn);
			try
			{
				conn.Open();
			}
			catch
			{
				return;
			}
			using (IfxCommand cmd = conn.CreateCommand())
			{
				cmd.CommandText = "select vh_gps_lat,vh_gps_long from vehicle where vh_nbr=" + this.VehicleID;
				IfxDataReader rdr = cmd.ExecuteReader();
				if ( rdr.Read() )
				{
					x = Int32.Parse(rdr["vh_gps_long"].ToString());
					y = Int32.Parse(rdr["vh_gps_lat"].ToString());

				}
			}
			conn.Close();

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
				w = new XmlTextWriter(@"C:\inetpub\wwwroot\MPKService\Data\locationupdate.xml",Encoding.UTF8);
				w.Formatting = Formatting.None;
				w.WriteStartDocument();
				w.WriteStartElement("location_update");
				
				
				w.WriteStartElement("vehicle");
				w.WriteString(this.VehicleID);
				w.WriteEndElement();
				
				
				w.WriteStartElement("time");
				w.WriteString(System.DateTime.Now.ToString("yyyyMMdd:HHmmss"));
				w.WriteEndElement();

				w.WriteStartElement("route_id");
				w.WriteString(this.RouteID);
				w.WriteEndElement();

				w.WriteStartElement("location");
				w.WriteAttributeString("x", this.x.ToString());
				w.WriteAttributeString("y", this.y.ToString());
				w.WriteEndElement();
				w.WriteEndElement();
				w.Close();

				XmlDocument xDoc = new XmlDocument();
				xDoc.Load(@"C:\inetpub\wwwroot\MPKService\Data\locationupdate.xml");

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
				//Console.WriteLine("Exception raised: {0}", e.Message);
			}

			try
			{
				myWebResponseMPK = (HttpWebResponse)myWebRequestMPK.GetResponse();
				using (StreamReader sr = new StreamReader(myWebResponseMPK.GetResponseStream()))
				{
					result = sr.ReadToEnd();
					Console.WriteLine("Received reply: {0}", result);
					return(result);
				}
			}
			catch (WebException e)
			{
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

