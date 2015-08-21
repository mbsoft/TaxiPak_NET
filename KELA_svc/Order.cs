using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Configuration;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Data;
using System.Data.SqlClient;
using System.Data.Odbc;
using log4net;
using log4net.Config;

namespace SUTI_svc
{
	/// <summary>
	/// Summary description for Order.
	/// </summary>
	public class Order
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Order));
		private string _rawXml;
		private string _msgID;
		public string OrderID
		{
			get { return _orderID; }
			set { _orderID = value; }
		}
		private string _orderID;
		public string _manualDescript;
		public Int32 TPakID
		{
			get { return _tpakID; }
			set { _tpakID = value; }
		}
		private Int32 _tpakID;
		public Route _route;
		private Economy _economy;
		private Vehicle _vehicle;
		private Driver _driver;
		private int _msgCounter;
		private ArrayList _vehicleAttributes = new ArrayList();
		public ArrayList VehAttributes
		{
			get { return _vehicleAttributes; }
			set { _vehicleAttributes = value; }
		}

		public Order(string xmlFragment, int msgCounter)
		{
			_rawXml = xmlFragment;
			XmlTextReader xRdr = new XmlTextReader(xmlFragment, XmlNodeType.Element, null);
			XmlDocument xDoc = new XmlDocument();
			XmlNode idOrderNode, manualDescriptNode;
			XmlAttributeCollection xAttr;
			
			_msgCounter = msgCounter;

			try
			{
				xDoc.Load(xRdr);
			}
			catch (Exception exc)
			{
				Console.WriteLine(exc.Message);
			}
			
			XmlNode msgNode = xDoc.SelectSingleNode("/SUTI/msg/idMsg");
			XmlAttributeCollection msgAttr = msgNode.Attributes;
			_msgID = msgAttr.GetNamedItem("id").InnerXml;
			idOrderNode = xDoc.SelectSingleNode("/SUTI/msg/order/idOrder");
			if ( idOrderNode != null )
			{
				xAttr = idOrderNode.Attributes;
				this.OrderID = xAttr.GetNamedItem("id").InnerXml;
			}
	
			manualDescriptNode = xDoc.SelectSingleNode("/SUTI/msg/order/manualDescriptionOrder");
			if ( manualDescriptNode != null )
			{
				xAttr = manualDescriptNode.Attributes;
				if ( xAttr.GetNamedItem("manualText") != null )
					_manualDescript = xAttr.GetNamedItem("manualText").InnerXml;
			}

			foreach (XmlNode attrNode in xDoc.SelectNodes("/SUTI/msg/order/resourceOrder/vehicle/attributesVehicle/idAttribute"))
			{
				string nodeXml = attrNode.OuterXml;

				XmlAttributeCollection vehAttr = attrNode.Attributes;
				if ( vehAttr != null )
				{
					System.Diagnostics.Debug.WriteLine(vehAttr.GetNamedItem("id").InnerXml);
					_vehicleAttributes.Add(vehAttr.GetNamedItem("id").InnerXml);
				}
				
			}
			_route = new Route(xmlFragment);

			_economy = new Economy(xmlFragment);

			ValidateOrder();

		}

		public void UpdateTPakID(Int32 taxipakID)
		{

			try
			{

				OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));
				conn.Open();

				using (OdbcCommand ct = conn.CreateCommand())
				{
					ct.CommandType = CommandType.Text;
					ct.CommandText = String.Format("insert into ilink values ({0},'{1}','NEW', 'LTX')", taxipakID, this.OrderID);

					ct.ExecuteNonQuery();
				}
				conn.Close();
			}
			catch (Exception exc)
			{
				Console.WriteLine(exc.Message);
			}


		}

		public string ReplyOrder()
		{
			XmlTextWriter w;

			try
			{
				w = new XmlTextWriter(@"C:\temp\ackorder.xml", Encoding.GetEncoding("iso-8859-15"));


				w.Formatting = Formatting.None;
				w.WriteStartDocument();
				w.WriteStartElement("SUTI");

				Preamble preamb = new Preamble();
				//preamb.FromLocal(ref w);

				w.WriteStartElement("msg");
				w.WriteAttributeString("msgType", "2001");
				w.WriteAttributeString("msgName", "ORDER CONFIRMATION");
				w.WriteStartElement("idMsg");
				w.WriteAttributeString("src",preamb.GetLocalName());
				w.WriteAttributeString("id", this._msgCounter.ToString());
				w.WriteAttributeString("unique", "true");
				w.WriteEndElement(); //</idMsg>
				w.WriteStartElement("referencesTo");
				w.WriteStartElement("idMsg");
				w.WriteAttributeString("src", preamb.GetRemoteName());
				w.WriteAttributeString("id", this._msgID);
				w.WriteEndElement(); //</idMsg>
				w.WriteStartElement("idOrder");
				w.WriteAttributeString("src", preamb.GetRemoteName());
				w.WriteAttributeString("id", this.OrderID);
				w.WriteEndElement(); // </idOrder>
				w.WriteStartElement("idOrder");
				w.WriteAttributeString("src", preamb.GetLocalName());
				w.WriteAttributeString("id", this._tpakID.ToString());
				w.WriteEndElement(); // </idOrder>
				w.WriteEndElement(); // </referencesTo>
				w.WriteEndElement(); //</msg>

				w.Close();

			}
			catch (Exception exc)
			{
				Console.WriteLine(exc.Message);
			}
			XmlDocument xDoc = new XmlDocument();
			xDoc.Load(@"C:\temp\ackorder.xml");

			log.InfoFormat("<-- {0}", xDoc.OuterXml);
			return(xDoc.OuterXml);
		}

		private void ValidateOrder()
		{
			Route theRoute = this._route;
			RteNode thePickup = (RteNode)theRoute._nodes[0];

			if (thePickup != null )
			{
				//validate the zone
				OdbcConnection connIfx = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));
				try
				{
					connIfx.Open();
				}
				catch (Exception exc)
				{
					log.Error(String.Format("Error opening Informix database: {0}", exc.Message));
					thePickup.Zone = "-1";
					return;
				}
				using (OdbcCommand ct = connIfx.CreateCommand())
				{
					OdbcDataReader dr;
					ct.CommandText = String.Format("select * from zonetrans where zntr_t800='{0}'", thePickup.Zone);
					ct.CommandType = CommandType.Text;
					dr = ct.ExecuteReader();
					if ( dr.Read() )
					{
						thePickup.Zone = dr["zntr_tpak"].ToString();
					}
					else
						thePickup.Zone = "-1";

					dr.Close();
				}

				connIfx.Close();
			}

		}
	}
}
