using System;
using System.Collections;
using System.Threading;
using System.Configuration;
using PI_Lib;
using System.Data;
using System.Data.SqlClient;
using System.Data.Odbc;
using log4net;
using log4net.Config;

namespace SUTI_svc
{
	/// <summary>
	/// Summary description for TPakTrip.
	/// </summary>
	public class TPakTrip
	{
		public string route_id;
		public string from_addr_street;
		public string from_addr_number;
		public string from_addr_suffix;
		public string from_addr_apart;
		public string from_addr_cmnt;
		public string from_zone;
		public string call_comment;
		public string from_addr_city;
		public string to_addr_street;
		public string to_addr_number;
		public string to_addr_suffix;
		public string to_addr_apart;
		public string to_addr_city;
		public string to_zone;
		public string passenger;
		public string call_nbr;
		public string estimate;
		public string trip_summary;
		public string wheelchairs;
		public string telephone;
		public ArrayList veh_attributes = new ArrayList();

		private static readonly ILog log = LogManager.GetLogger(typeof(TPakTrip));

		PI_Lib.PIClient myPISocket;
		public TPakTrip()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public bool ValidateVehicleAttr()
		{
			OdbcConnection connIfx = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));
			try
			{
				connIfx.Open();
			}
			catch (Exception exc)
			{
				log.Error(String.Format("Error opening Informix database: {0}", exc.Message));
				return false;
			}
			using (OdbcCommand ct = connIfx.CreateCommand())
			{
				for (int i = 0; i < this.veh_attributes.Count; i++ )
				{
					OdbcDataReader dr;
					ct.CommandText = String.Format("select at_nbr, at_type from attrans, attr where attr_tpak=at_abbrev and at_fleet='H' and attr_t800='{0}'", this.veh_attributes[i].ToString());
					ct.CommandType = CommandType.Text;
					dr = ct.ExecuteReader();
					if ( dr.Read() )
					{
						if ( dr["at_type"].ToString().Equals("T") )
							this.veh_attributes[i] = (object)dr["at_nbr"].ToString();
						else
							this.veh_attributes[i] = (object)"0"; //probably a driver attribute
					}
					else
					{
						dr.Close();
						connIfx.Close();
						return false;
					}

					dr.Close();
				}
			}

			connIfx.Close();

			return true;

		}

		public bool Fill(Order theOrder)
		{
			//Retrieve first address for order
			RteNode firstNode = (RteNode)theOrder._route._nodes[0];

			from_addr_street = firstNode.Street;
			from_addr_number = firstNode.StreetNbr;

			from_addr_city = firstNode.Locality;
			from_zone = firstNode.Zone;
			from_addr_cmnt = theOrder._manualDescript;
			if ( firstNode.StreetNbrLtr != null )
				from_addr_apart = firstNode.StreetNbrLtr;

			if ( firstNode.Contents.Count > 0 )
			{
				NdeContent firstContent = (NdeContent)firstNode.Contents[0];
				passenger = firstContent.Name;
				telephone = firstContent.ContactPhone;
			}
			else 
			{
				passenger = String.Empty;
				telephone = String.Empty;
			}
			// if Description + ManualDescript exceeds 64 chars we can't process
			// this order.

			if ( theOrder._manualDescript != null && firstNode.Description != null )
			{
				if ( ( theOrder.OrderID.Length + firstNode.Description.Length + theOrder._manualDescript.Length ) < 64 )
					call_comment = theOrder.OrderID + "/" + theOrder._manualDescript + " " + firstNode.Description;
				else
					return false;
			}
			else if ( theOrder._manualDescript != null )
			{
				if ( theOrder.OrderID.Length + theOrder._manualDescript.Length < 64 )
					call_comment = theOrder.OrderID + "/" + theOrder._manualDescript;
				else
					return false;
			}
			else if ( firstNode.Description != null )
			{
				if ( theOrder.OrderID.Length + firstNode.Description.Length < 64 )
					call_comment = theOrder.OrderID + "/" + firstNode.Description;
				else
					return false;
			}
			else
				call_comment = theOrder.OrderID + "/";
			

			//Destination address in second node if present
			if ( theOrder._route._nodes.Count > 1 )
			{
				RteNode secondNode = (RteNode)theOrder._route._nodes[1];
				if ( secondNode != null )
				{
					to_addr_street = secondNode.Street;
					to_addr_number = secondNode.StreetNbr;
					if ( secondNode.StreetNbrLtr != null )
						to_addr_apart = secondNode.StreetNbrLtr;
					to_addr_city = secondNode.Locality;
				}
				if ( secondNode.Contents.Count > 0 )
				{
					NdeContent secondContent = (NdeContent)secondNode.Contents[0];
					passenger = secondContent.Name;
					telephone = secondContent.ContactPhone;
				}
			}

			// vehicle attributes
			if ( theOrder.VehAttributes.Count > 0 ) // have vehicle attributes
			{
				this.veh_attributes = theOrder.VehAttributes;
			}

			return true;
			
		}
		public Int32 Dispatch()
		{
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
					return (-1);
				}
				myPISocket.SetType(MessageTypes.PI_DISPATCH_CALL);
				PI_DISPATCH_CALL myCall = new PI_DISPATCH_CALL();
				
				myCall.call_type = ConfigurationSettings.AppSettings["CallType"].ToCharArray();
				myCall.fleet = Convert.ToChar(ConfigurationSettings.AppSettings["FleetID"]);
				myCall.priority = Convert.ToInt16("25");
				myCall.number_of_calls = Convert.ToChar("1");
				myCall.from_addr_street = this.from_addr_street.ToCharArray();
				if ( this.from_addr_apart != null )
					myCall.from_addr_apart = this.from_addr_apart.ToCharArray();
				//myCall.from_addr_cmnt = this.from_addr_cmnt.ToCharArray();
				myCall.call_comment = (this.call_comment + " " + this.route_id).ToCharArray();
				if ( this.telephone != null )
					myCall.phone = this.telephone.ToCharArray();

				try
				{
					myCall.from_addr_number = Convert.ToInt32(this.from_addr_number);
					if ( this.from_addr_suffix != null )
						myCall.from_addr_apart = this.from_addr_suffix.ToCharArray();
				}
				catch (FormatException)
				{
					//addr number must contain a non-numeric
					this.from_addr_suffix = this.from_addr_number.Substring(this.from_addr_number.Length-1, 1);
					string tmp_from_addr_number = this.from_addr_number.Substring(0,this.from_addr_number.Length-1);
					try
					{
						myCall.from_addr_number = Convert.ToInt32(tmp_from_addr_number);
						myCall.from_addr_apart = this.from_addr_suffix.ToCharArray();
					}
					catch
					{
						myCall.from_addr_street = this.from_addr_number.ToCharArray();
						myCall.from_addr_number = 0;
					}

				}
			

				myCall.from_addr_city = this.from_addr_city.ToCharArray();
				myCall.from_addr_zone = Convert.ToInt16(this.from_zone);
				
				if ( this.to_addr_street != null )
				{
					myCall.to_addr_street = this.to_addr_street.ToCharArray();
					if ( this.to_addr_apart != null )
						myCall.to_addr_apart = this.to_addr_apart.ToCharArray();
					try
					{
						myCall.to_addr_number = Convert.ToInt32(this.to_addr_number);
						if ( this.to_addr_suffix != null )
							myCall.to_addr_apart = this.to_addr_suffix.ToCharArray();
					}
					catch (FormatException)
					{
						this.to_addr_suffix = this.to_addr_number.Substring(this.to_addr_number.Length-1, 1);
						string tmp_to_addr_number = this.to_addr_number.Substring(0,this.to_addr_number.Length-1);
						try
						{
							myCall.to_addr_number = Convert.ToInt32(tmp_to_addr_number);
							myCall.to_addr_apart = this.to_addr_suffix.ToCharArray();
						}
						catch
						{
							myCall.to_addr_street = this.to_addr_number.ToCharArray();
							myCall.to_addr_number = 0;
						}
					}
					myCall.to_addr_city = this.to_addr_city.ToCharArray();
				}

				if ( this.passenger != null )
					myCall.passenger = this.passenger.ToCharArray();

				myCall.car_attrib = ConfigurationSettings.AppSettings["Vehicle_Attr"].ToCharArray();

				if ( this.veh_attributes.Count > 0 )
				{
					foreach ( string vehAttr in this.veh_attributes )
					{
						int at_nbr = Int32.Parse(vehAttr);
						if ( at_nbr > 0 && at_nbr <= 32 )
							myCall.car_attrib[at_nbr-1] = 'K';
					}
				}

				
				
				// Set the 'wheelchair' attribute if required
				if ( Convert.ToInt32(this.wheelchairs) > 0 )
					myCall.car_attrib[30] = 'K';

				
				myPISocket.sendBuf = myCall.ToByteArray();

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
					return -1;
				}

				this.call_nbr = myCall.call_number.ToString();
				// Update route record with trip number


				return Int32.Parse(this.call_nbr);
			}
			else
			{
				return -1;
			}
		}
	}
}

