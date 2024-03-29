using System;
using System.Threading;
using System.Configuration;
using PI_Lib;
#if MSSQL
using System.Data;
using System.Data.SqlClient;
#else
using Finisar.SQLite;
#endif
using log4net;
using log4net.Config;

namespace MPKBridge
{
	/// <summary>
	/// Summary description for TPakTrip.
	/// </summary>
	public class TPakTrip
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(TPakTrip));
		public string route_id;
		public string from_addr_street;
		public string from_addr_number;
		public string from_addr_suffix;
		public string call_comment;
		public string from_addr_city;
		public string passenger;
		public string call_nbr;
		public string estimate;
		public string trip_summary;
		public string wheelchairs;

		PI_Lib.PIClient myPISocket;
		public TPakTrip()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public CallMonitor Dispatch()
		{
			if ( System.Configuration.ConfigurationSettings.AppSettings["TPak_dispatch"].Equals("YES") )
			{
				log.Info(String.Format("Dispatch ACTIVE. Sending to TaxiPak {0}", ConfigurationSettings.AppSettings["PIServer"]));
				try
				{
					myPISocket = new PI_Lib.PIClient();
				}
				catch (System.Net.Sockets.SocketException ex)
				{
					log.Error(String.Format("Error on PI socket {0}", ex.Message));
					Console.WriteLine("Error on PI socket ({0})", ex.Message);
					return (null);
				}
				myPISocket.SetType(MessageTypes.PI_DISPATCH_CALL);
				PI_DISPATCH_CALL myCall = new PI_DISPATCH_CALL();
				
				myCall.call_type = "MPK ".ToCharArray();
				myCall.fleet = Convert.ToChar("H");
				myCall.priority = Convert.ToInt16("25");
				myCall.number_of_calls = Convert.ToChar("1");
				myCall.from_addr_street = this.from_addr_street.ToCharArray();
				myCall.call_comment = (this.call_comment + " " + this.route_id).ToCharArray();
				
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
			
				
				myCall.to_addr_cmnt = (String.Format("{0}:{1}", this.estimate, this.trip_summary)).ToCharArray();
				myCall.to_addr_street = (String.Format("{0}:{1}", this.estimate, this.trip_summary)).ToCharArray();
				myCall.from_addr_city = this.from_addr_city.ToCharArray();
				myCall.passenger = this.passenger.ToCharArray();
				myCall.car_attrib = ConfigurationSettings.AppSettings["Vehicle_Attr"].ToCharArray();
				
				// Set the 'wheelchair' attribute if required
				if ( Convert.ToInt32(this.wheelchairs) > 0 )
					myCall.car_attrib[30] = 'K';

				
				myPISocket.sendBuf = myCall.ToByteArray();

				try
				{
					myPISocket.SendMessage();
					myPISocket.ReceiveMessage();

					myCall.Deserialize(myPISocket.recvBuf);
					myPISocket.CloseMe();
				}
				catch
				{
					log.InfoFormat("Error entering TaxiPak trip for route {0}", this.route_id);
					return null;
				}

				this.call_nbr = myCall.call_number.ToString();
				// Update route record with trip number
				SqlConnection conn = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));
				try
				{
					conn.Open();
					using (SqlCommand ct = conn.CreateCommand())
					{
						string sqlQuery = "update route set tpak_id=" + this.call_nbr + " where route_id='" + this.route_id + "'";
						ct.CommandText = sqlQuery;
						ct.ExecuteNonQuery();
					}
					conn.Close();
				}
				catch (Exception exc)
				{
					conn.Close();
					log.InfoFormat("Error accessing DB {0}", exc.Message);
				}



				CallMonitor cm = new CallMonitor(Convert.ToInt32(this.call_nbr), this.route_id);
				return cm;
			}
			else
			{
				log.Info("Dispatch INACTIVE - set by config parameter");
				return null;
			}
		}
	}
}
