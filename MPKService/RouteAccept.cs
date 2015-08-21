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
using PI_Lib;

namespace MPKService
{
	/// <summary>
	/// Summary description for RouteAccept.
	/// </summary>
	public class RouteAccept
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(RouteAccept));
		private HttpWebRequest myWebRequestMPK;
		private HttpWebResponse myWebResponseMPK;
		private PIClient myPISocket;

		public string RouteID;
		public string Accept;
		public string VehicleID;
		public string VehPax;
		public string VehWheels;
		public string PriceGroup;
		public string CompanyID;
		public string Version;
		public string TPakID;

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
				
				w = new XmlTextWriter(@"C:\\temp\\routeaccept_" + System.Threading.Thread.CurrentThread.GetHashCode().ToString() + ".xml",Encoding.UTF8);
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
				xDoc.Load(@"c:\\temp\\routeaccept_" + System.Threading.Thread.CurrentThread.GetHashCode().ToString() + ".xml");

				log.Info(String.Format("<route_accept> sent: {0}", xDoc.OuterXml));
				//Console.WriteLine("Sending <route_accept> msg: {0}",xDoc.OuterXml);

				MD5Verifier ver = new MD5Verifier(Encoding.UTF8);
				ver.doVerify(xDoc.OuterXml);

				Stream reqStream = myWebRequestMPK.GetRequestStream();
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
					XmlNode idMsgNode;
					result = sr.ReadToEnd();
					result = result.Substring(0,result.LastIndexOf("</ack>")+6);
					log.Info(String.Format("MPK Server reply: {0}", result));
					// Check for ERROR response
					XmlTextReader xRdr = new XmlTextReader( result, XmlNodeType.Element, null);
					XmlDocument xDoc = new XmlDocument();
					xDoc.Load(xRdr);
					idMsgNode = xDoc.SelectSingleNode("/ack/status");
					if ( idMsgNode != null )
					{
						if ( idMsgNode.InnerText.Equals("offer_expired") ||
							idMsgNode.InnerText.Equals("error") )
						{
							log.InfoFormat("MPK Server indicates ERROR. Cancelling trip {0} in TaxiPak", this.TPakID);
							// Cancel trip in TaxiPak....MPK side doesn't like our ACCEPT
							try
							{
								myPISocket = new PIClient();
							}
							catch (System.Net.Sockets.SocketException ex)
							{
								log.Info(String.Format("Error connecting to TaxiPak for cancel: {0}", ex.Message));
								//Console.WriteLine("Error connecting to TaxiPak ({0})", ex.Message);
								return result;
							}
							catch (Exception ex)
							{
								log.InfoFormat("Generic error connecting to TaxiPak for cancel: {0}", ex.Message);
								return result;
							}

							myPISocket.SetType(MessageTypes.PI_CANCEL_CALL);
							PI_Lib.PI_CANCEL_CALL myCancelCall = new PI_CANCEL_CALL();
							myPISocket.sendBuf = myCancelCall.ToByteArray(Convert.ToInt32(this.TPakID));
							try
							{
								myPISocket.SendMessage();
								//myPISocket.ReceiveMessage();

								//PI_CANCEL_CALL.Deserialize(myPISocket.recvBuf);
								myPISocket.CloseMe();
							}
							catch (Exception ex)
							{
								log.InfoFormat("Error cancelling trip in TaxiPak {0} {1}", this.TPakID, ex.Message);
								return(result);
							}
							log.InfoFormat("Trip cancelled {0}", this.TPakID);
						}
					}
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
