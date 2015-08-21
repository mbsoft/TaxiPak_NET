using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Data;
using System.Drawing;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.IO;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Diagnostics;
using log4net;
using log4net.Config;

namespace MPKService
{
	/// <summary>
	/// Summary description for HTD_MPK1.
	/// </summary>
	public class HTD_MPK1 : System.Web.UI.Page
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(HTD_MPK1));

		protected System.Web.UI.WebControls.Label Label1;
	
		private void Page_Load(object sender, System.EventArgs e)
		{
			
			// Put user code to initialize the page here
			string opcode = Request.Params["op"];
			string reqXml;
			
			Thread.CurrentThread.CurrentCulture = new CultureInfo("fi-FI");
			Thread.CurrentThread.CurrentUICulture = new CultureInfo("fi-FI");

			Stream reqStream = Request.InputStream;
			StreamReader rdr = new StreamReader(reqStream);

			try
			{
				reqXml = rdr.ReadToEnd();

				FileStream f = new FileStream(@"c:\temp\in.txt", FileMode.Append);
				StreamWriter sw = new StreamWriter(f);
				sw.Write(reqXml);
				sw.Close();
				f.Close();
			}
			catch
			{
				Response.Redirect(@".\MPK_HTD.xsd");
				return;
			}

			if (reqXml.Length == 0)
				Response.Redirect(@".\MPK_HTD.xsd");

			int endXml = reqXml.LastIndexOf("</route>");
			
			if ( endXml < 0 )
			{
				endXml = reqXml.LastIndexOf("</ping>");
				if ( endXml < 0 )
				{
					endXml = reqXml.LastIndexOf("<ping />");
					if ( endXml < 0 )
					{
						endXml = reqXml.LastIndexOf("</location_request>");
						if ( endXml < 0 )
						{

							
								Response.Redirect(@".\MPK_HTD.xsd");
								return;
					

						}
						else
						{
							log.InfoFormat("Request is <location_request>");
							log.InfoFormat("New request: {0}", reqXml);
							endXml += 19; // </location_request> document
						}
					}
					else
					{
						log.InfoFormat("Request is <ping>");
						endXml += 8; // <ping /> document
					}
				}
				else
					endXml += 7;
			}
			else
			{
				log.InfoFormat("Request is <route>");
				log.InfoFormat("New request: {0}", reqXml);
				endXml += 8; // </route> document
			}
			
			string checksum = reqXml.Substring(endXml,(reqXml.Length-endXml));
			string xmlFragment = reqXml.Substring(0,endXml);
			XmlTextReader xRdr = new XmlTextReader(xmlFragment, XmlNodeType.Element, null);
			XmlDocument xDoc = new XmlDocument();
			xDoc.Load(xRdr);

			 FileStream f1 = new FileStream(@"c:\temp\in.txt", FileMode.Append);
			 StreamWriter sw1 = new StreamWriter(f1);
			sw1.Write(Environment.NewLine + "Phase 1: complete" + Environment.NewLine);
			sw1.Close();
			f1.Close();

			XmlNode routeNode = xDoc.SelectSingleNode("/route");

			if ( routeNode != null )
			{
				
				Route myRoute = new Route(routeNode, checksum);
				
				
				log.InfoFormat("Route handling completed. Sending reply to client.");
				Response.Write(myRoute.ReplyRoute());
				log.InfoFormat("Reply to client completed.");
			}
			
			XmlNode locationNode = xDoc.SelectSingleNode("/location_request");
			if ( locationNode != null )
			{
				f1 = new FileStream(@"c:\temp\in.txt", FileMode.Append);
				sw1 = new StreamWriter(f1);
				sw1.Write("Phase 2: location found"  + Environment.NewLine);
				sw1.Close();
				f1.Close();
				Location myLoc = new Location(locationNode);
				Response.Write(myLoc.ReplyLocation());
			}

			XmlNode pingNode = xDoc.SelectSingleNode("/ping");
			if ( pingNode != null )
			{
				f1 = new FileStream(@"c:\temp\in.txt", FileMode.Append);
				sw1 = new StreamWriter(f1);
				sw1.Write("Phase 2: ping found"  + Environment.NewLine);
				sw1.Close();
				f1.Close();
				Ping myPing = new Ping(pingNode, checksum);
				Response.Write(myPing.ReplyPing());
			}

			XmlNode testNode = xDoc.SelectSingleNode("/route_accept");  // testing only
			if ( testNode != null )
			{
				Test myTest = new Test(testNode, checksum);
				Response.Write(myTest.ReplyTest());
			}



		}


		string HandleLocationRequest( string msgLocationReq )
		{

			
			// Retrieve taxi position and return in location_update message
			XmlTextReader reader = new XmlTextReader(msgLocationReq, XmlNodeType.Document, null);
			XmlValidatingReader validReader = new XmlValidatingReader(reader);
			validReader.ValidationType = ValidationType.Schema;
			
			// Load the schema for validation purposes
			XmlSchemaCollection schemaCollection = new XmlSchemaCollection();
			try
			{
				schemaCollection.Add(null, @"C:\Inetpub\wwwroot\MPKService\MPK_HTD.xsd");
				validReader.Schemas.Add(schemaCollection);

				validReader.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.Message);
			}

			XPathDocument xpathdoc;
			try
			{
				xpathdoc = new XPathDocument(validReader, XmlSpace.Preserve);
			}
			catch (XmlException e)
			{
				return "<?xml version=\"1.0\" ?><envelope><error>" + e.Message + "</error></envelope>";
			}

			XPathNavigator xpathNav = xpathdoc.CreateNavigator();
			string xpathQuery = "/envelope/location_request/vehicle";
			XPathNodeIterator xpathIter = xpathNav.Select(xpathQuery);

			StringWriter sw = new StringWriter();			
			
			try
			{
				while (xpathIter.MoveNext())
					sw.WriteLine("VEH_NBR: {0}", xpathIter.Current.Value);
			}
			catch (XmlException e)
			{
				return "<?xml version=\"1.0\" ?><envelope><error>" + e.Message + "</error></envelope>";
			}


			
			
			return msgLocationReq;
		}

		#region Web Form Designer generated code
		override protected void OnInit(EventArgs e)
		{
			//
			// CODEGEN: This call is required by the ASP.NET Web Form Designer.
			//
			InitializeComponent();
			base.OnInit(e);
		}
		
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{    
			this.Load += new System.EventHandler(this.Page_Load);

		}
		#endregion

		private void ValidationCallBack(object sender, ValidationEventArgs e)
		{
			Debug.WriteLine("Validation Error: {0}", e.Message);
			throw new XmlException(e.Message);
		}
	}
}
