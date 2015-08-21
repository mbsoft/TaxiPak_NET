using System;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using IBM.Data.Informix;
using System.Collections;
using System.Configuration;

namespace MPKService
{
	/// <summary>
	/// Summary description for Location.
	/// </summary>
	public class Location
	{
		private class Vehicle
		{
			private int veh_nbr;
			public int VehNbr
			{
				get { return veh_nbr; }
			}
			private int loc_x;
			public int X
			{
				get { return loc_x; }
				set { loc_x = value; }
			}
			private int loc_y;
			public int Y
			{
				get { return loc_y; }
				set { loc_y = value; }
			}
			public Vehicle(string VehNbr)
			{
				veh_nbr = Convert.ToInt32(VehNbr);
			}
		}
		private Vehicle theVehicle;

		public Location(XmlNode locationNode)
		{
			
			theVehicle = new Vehicle(locationNode.SelectSingleNode("/location_request/vehicle").InnerXml);

			//string sqlConnString = "Host=192.168.1.120;Service=6032;Server=mads_se;User ID=net_book;password=Mickey;Database=/usr/taxi/mads";
			string sqlConnString = ConfigurationSettings.AppSettings.Get("MadsOBC");
			IfxConnection conn = new IfxConnection(sqlConnString);
			conn.Open();
			using (IfxCommand ct = conn.CreateCommand())
			{
				string sqlQuery = "select vh_gps_long,vh_gps_lat from vehicle where vh_nbr=" + theVehicle.VehNbr.ToString();
				ct.CommandText = sqlQuery;
				IfxDataReader dr = ct.ExecuteReader();

				if ( dr.Read() )
				{
					theVehicle.X = Convert.ToInt32(dr["vh_gps_long"]);
					theVehicle.Y = Convert.ToInt32(dr["vh_gps_lat"]);
				}
				else
				{
					theVehicle.X = 0;
					theVehicle.Y = 0;
				}
			}
			conn.Close();
			
		}

		public string ReplyLocation()
		{
			try
			{
				XmlTextWriter w = new XmlTextWriter(@"C:\temp\location_update.xml", Encoding.UTF8);

				w.Formatting = Formatting.None;
				w.WriteStartDocument();
				w.WriteStartElement("location_update");
				w.WriteStartElement("vehicle");
				w.WriteString(theVehicle.VehNbr.ToString());
				w.WriteEndElement();
				w.WriteStartElement("time");
				w.WriteString(String.Format("{0:yyyyMMdd}:{1:HHmmss}",System.DateTime.Now,System.DateTime.Now));
				w.WriteEndElement();
				w.WriteStartElement("route_id");
				w.WriteString("null");
				w.WriteEndElement();
				w.WriteStartElement("location");
				w.WriteAttributeString("x", theVehicle.X.ToString());
				w.WriteAttributeString("y", theVehicle.Y.ToString());
				w.WriteEndElement();
				w.WriteEndElement(); // </location_update>

				w.Close();

				// now load the ack.xml document and calculate checksum
				XmlDocument xDoc = new XmlDocument();
				xDoc.Load(@"c:\temp\location_update.xml");

				MD5Verifier ver = new MD5Verifier(Encoding.UTF8);
				
				ver.doVerify(xDoc.OuterXml);

				return xDoc.OuterXml + ver.GetCheckSum();
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e.Message);
				return null;
			}
			
		}
	}
}
