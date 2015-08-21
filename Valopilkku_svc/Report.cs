using System;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.IO;
using System.Data;
using System.Data.Odbc;
using System.Configuration;
using log4net;
using log4net.Config;

namespace SUTI_svc
{
	/// <summary>
	/// 
	/// </summary>
	public class Report
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Report));
		private string reply;
		private string sSrc;
		private string sID;
		private int msgCount;
		private string sVehicleID;
		private string sOrderID;

		public Report(XmlNode confirmNode, int msgCounter, XmlDocument xDoc)
		{

			XmlAttributeCollection msgAttr = confirmNode.Attributes;
			sSrc = msgAttr.GetNamedItem("src").InnerXml;
			sID  = msgAttr.GetNamedItem("id").InnerXml;
			msgCount = msgCounter;

			XmlNode idVehicle = xDoc.SelectSingleNode("/SUTI/msg/referencesTo/idVehicle");
			if ( idVehicle != null )
			{
				XmlAttributeCollection idVehAttr = idVehicle.Attributes;
				sVehicleID = idVehAttr.GetNamedItem("id").InnerXml;
			}
			XmlNode idOrder = xDoc.SelectSingleNode("/SUTI/msg/referencesTo/idOrder");
			if ( idOrder != null )
			{
				XmlAttributeCollection idOrderAttr = idOrder.Attributes;
				sOrderID = idOrderAttr.GetNamedItem("id").InnerXml;
			}

			if ( sOrderID.Length > 0 )
				ConfirmOrderReport( sOrderID, sVehicleID );

		}


		private void ConfirmOrderReport( string sOrderID, string sVehicleID )
		{

			OdbcConnection connIfx = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));
			try
			{
				connIfx.Open();
			}
			catch
			{
				return;
			}

			using (OdbcCommand ct = connIfx.CreateCommand() )
			{

				ct.CommandText = String.Format("update ilink set status='FINISHED' where tpak_nbr={0}",
					this.sOrderID);
				ct.CommandType = CommandType.Text;
				ct.ExecuteNonQuery();

				ct.CommandText = String.Format("update calls set cl_status='VALMIS', cl_pri_status=64,cl_close_date_time={0},cl_close_date='{1}',cl_close_time='{2}' where cl_nbr={3}",
					getUnixTimeStamp(System.DateTime.Now), System.DateTime.Now.ToString("dd.MM.yy"), DateTime.Now.ToString("HH:mm"), this.sOrderID);
				ct.CommandType = CommandType.Text;
				ct.ExecuteNonQuery();

				ct.CommandText = String.Format("insert into callh values (0,{0}, 0, 'H', 'T',{1},'{2}','{3}','LTXSULJE',0,0,0,' ',' ','{4}',{5},0,0,0)",
					this.sOrderID.ToString(), getUnixTimeStamp(System.DateTime.Now), System.DateTime.Now.ToString("dd.MM.yy"), DateTime.Now.ToString("HH:mm"),
					" ", getUnixTimeStamp(System.DateTime.Now));
				ct.CommandType = CommandType.Text;
				ct.ExecuteNonQuery();

			}
			connIfx.Close();
		}

		private string getUnixTimeStamp(DateTime date_time_convert)
		{
			DateTime date_time_base = new DateTime(1970,1,1,0,0,0,0);
			TimeSpan span = date_time_convert.ToUniversalTime() - date_time_base;
			Int32 nbrSecs = Convert.ToInt32(span.TotalSeconds);
			return(nbrSecs.ToString());
		}

		public string ReportConfirm()
		{
			XmlTextWriter w = new XmlTextWriter(@"C:\temp\ack2.xml", Encoding.GetEncoding("iso-8859-15"));

			w.Formatting = Formatting.None;
			w.WriteStartDocument();
			w.WriteStartElement("SUTI");

			//Preamble preamb = new Preamble();
			//preamb.FromLocal(ref w);

			w.WriteStartElement("msg");
			w.WriteAttributeString("msgType", "7032");
			w.WriteAttributeString("msgName", "Acknowledge");
			w.WriteStartElement("idMsg");
			w.WriteAttributeString("src", "mbsoft_htd_001");
			w.WriteAttributeString("id", msgCount.ToString());
			w.WriteEndElement(); //</idMsg>
			w.WriteStartElement("referencesTo");
			w.WriteStartElement("idMsg");
			w.WriteAttributeString("src","planit_lahitaksi_003");
			w.WriteAttributeString("id", this.sID);
			w.WriteEndElement(); //</idMsg>
			w.WriteStartElement("idOrder");
			w.WriteAttributeString("src", "mbsoft_htd_001");
			w.WriteAttributeString("id", this.sOrderID);
			w.WriteEndElement(); // </idOrder>
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

