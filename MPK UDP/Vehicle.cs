using System;
using System.Configuration;
using System.Text;
using System.Collections;
using System.Net;
using System.Threading;
using PI_Lib;
using System.Data;
using System.Data.Odbc;
using log4net;
using log4net.Config;

namespace MPKBridge
{
	/// <summary>
	/// Summary description for Vehicle.
	/// </summary>
	public class Vehicle
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Vehicle));

		private string licenseNbr;
		public string License
		{
			get { return licenseNbr; }
			set { licenseNbr = value; }
		}

		private int vehID;
		private string make;
		private string model;

		private string mdtTelephone;
		public string MobilePhone
		{
			get { return mdtTelephone; }
			set { mdtTelephone = value; }
		}

		private double _locX;
		public double LocX
		{
			get { return _locX; }
			set { _locX = value; }
		}
		private double _locY;
		public double LocY
		{
			get { return _locY; }
			set { _locY = value; }
		}
		private bool _wheelchair;
		public bool Wheelchair
		{
			get { return _wheelchair; }
			set { _wheelchair = value; }
		}
		private int _pass_capacity;
		public int PassCapacity
		{
			get { return _pass_capacity; }
			set { _pass_capacity = value; }
		}
	

		private string currentStop;
		private string nextStop;
		private string routeID;
		private string arriveTime;
		private string departTime;

		public Vehicle(string MID)
		{
			OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));
			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{
					string sqlQuery = "select vh_fleet,vh_nbr,vh_license_nbr,vh_make,vh_model,vh_mdt_tele,vh_attr, vh_gps_lat,vh_gps_long from vehicle where vh_fleet='H' and vh_mobile_rf_id='" +
						MID.ToLower() + "'";
					ct.CommandText = sqlQuery;
					ct.CommandTimeout = 5;
					try
					{
						OdbcDataReader dr = ct.ExecuteReader(CommandBehavior.SingleResult);
						if ( dr.Read() )
						{
							vehID = Convert.ToInt32(dr["vh_nbr"]);
							licenseNbr = dr["vh_license_nbr"].ToString().Trim();
							make = dr["vh_make"].ToString().Trim();
							model = dr["vh_model"].ToString().Trim();
							mdtTelephone = dr["vh_mdt_tele"].ToString().Trim();
							LocX = Convert.ToDouble(dr["vh_gps_long"]);
							LocY = Convert.ToDouble(dr["vh_gps_lat"]);
							string vh_attr = dr["vh_attr"].ToString();
							if ( vh_attr.Substring(30,1).Equals("K") )
								this.Wheelchair = true;
							else
								this.Wheelchair = false;
							if ( vh_attr.Substring(1,1).Equals("K") )
								this.PassCapacity = 8;
							else if ( vh_attr.Substring(2,1).Equals("K") )
								this.PassCapacity = 6;
							else if ( vh_attr.Substring(0,1).Equals("K") )
								this.PassCapacity = 5;
							else
								this.PassCapacity = 4;
						}
						else
							log.Error(String.Format("Vehicle {0} not found in TaxiPak table", vehID));
						dr.Close();
					}
					catch (Exception exc)
					{
						log.InfoFormat("IFX read error {0}", exc.Message);
					}

				}
				conn.Close();
			}
			catch (Exception exc)
			{
				log.InfoFormat("Error retrieving IFX vehicle - {0}", exc.Message);
				conn.Close();
			}
		}

		public Vehicle(int vehID)
		{
			this.vehID = vehID;
			OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));
			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{
					string sqlQuery = "select vh_fleet,vh_nbr,vh_license_nbr,vh_make,vh_model,vh_mdt_tele,vh_attr, vh_gps_lat,vh_gps_long from vehicle where vh_fleet='H' and vh_nbr=" +
						vehID.ToString();
					ct.CommandText = sqlQuery;
					ct.CommandTimeout = 5;
					try
					{
						OdbcDataReader dr = ct.ExecuteReader(CommandBehavior.SingleResult);
						if ( dr.Read() )
						{
							licenseNbr = dr["vh_license_nbr"].ToString().Trim();
							make = dr["vh_make"].ToString().Trim();
							model = dr["vh_model"].ToString().Trim();
							mdtTelephone = dr["vh_mdt_tele"].ToString().Trim();
							LocX = Convert.ToDouble(dr["vh_gps_long"]);
							LocY = Convert.ToDouble(dr["vh_gps_lat"]);
							string vh_attr = dr["vh_attr"].ToString();
							if ( vh_attr.Substring(30,1).Equals("K") )
								this.Wheelchair = true;
							else
								this.Wheelchair = false;
							if ( vh_attr.Substring(1,1).Equals("K") )
								this.PassCapacity = 8;
							else if ( vh_attr.Substring(2,1).Equals("K") )
								this.PassCapacity = 6;
							else if ( vh_attr.Substring(0,1).Equals("K") )
								this.PassCapacity = 5;
							else
								this.PassCapacity = 4;
						}
						else
							log.Error(String.Format("Vehicle {0} not found in TaxiPak table", vehID));
						dr.Close();
					}
					catch (Exception exc)
					{
						log.InfoFormat("IFX read error {0}", exc.Message);
					}

				}
				conn.Close();
			}
			catch (Exception exc)
			{
				log.InfoFormat("Error retrieving IFX vehicle - {0}", exc.Message);
				conn.Close();
			}
		}

		public void SetFirstStop(string routeID)
		{
			string firstStopID;
			string nextStopID;


			OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MPKODBC"));

			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{
					string sqlQuery = "select * from stop, vehicle where stop.route_id='" + routeID + "' and stop.route_id=vehicle.route_id order by sequence_nbr";
					ct.CommandText = sqlQuery;
					OdbcDataReader dr = ct.ExecuteReader();
					if ( dr.Read() )
					{
						firstStopID = dr["stop_id"].ToString();
						if ( dr.Read() )
							nextStopID = dr["stop_id"].ToString();
						else
							nextStopID = "";
					}
					else
					{
						log.Error(String.Format("Error retrieving stops for vehicle {0} route {1}",
							this.vehID.ToString(), routeID));
						dr.Close();
						conn.Close();
						return;
					}
					dr.Close();
					// Now set the stops in the vehicle record
					sqlQuery = "update vehicle set current_stop='" + firstStopID + "',next_stop='" + nextStopID + "' where vehicle.route_id='" + routeID + "' and veh_id=" + this.vehID;
					ct.CommandText = sqlQuery;
					ct.ExecuteNonQuery();
					log.InfoFormat("Vehicle {0} First Stop {1} Next Stop {2}",
						vehID, firstStopID, nextStopID);
				}
				conn.Close();
			}
			catch (OdbcException exc)
			{
				conn.Close();
				log.InfoFormat("Error accessing DB {0}",exc.Message);
			}
			catch (Exception e)
			{
				conn.Close();
				log.InfoFormat("Error in SetFirstStop - {0}", e.Message);
			}


		}

		public void SendFirstStop(string routeID)
		{
			string firstStopID="";

			OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MPKODBC"));

			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{
					string sqlQuery = "select * from stop, vehicle where stop.route_id='" + routeID + "' and stop.route_id=vehicle.route_id order by sequence_nbr";
					ct.CommandText = sqlQuery;
					OdbcDataReader dr = ct.ExecuteReader();
					if ( dr.Read() )
					{
						firstStopID = dr["stop_id"].ToString();
					}
					else
					{
						log.Error(String.Format("Error retrieving stops for vehicle {0} route {1}",
							this.vehID.ToString(), routeID));
						dr.Close();
						conn.Close();
						return;
					}
					dr.Close();
				}
				conn.Close();
			}
			catch (OdbcException exc)
			{
				conn.Close();
				log.InfoFormat("Error accessing DB {0}",exc.Message);
			}
			catch (Exception exc)
			{
				conn.Close();
				log.InfoFormat("Error in SendFirstStop - {0}", exc.Message);
			}

			log.InfoFormat("Sending first stop {0} to vehicle {1}", firstStopID, this.vehID.ToString());

			
			string msgText="";
			string pickupAddr="";

			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{
					string sqlQuery = "select * from stop,route,passenger where stop_id='" + firstStopID + "' and route.route_id=stop.route_id and (pickup=stop_id)";
					ct.CommandText = sqlQuery;
					OdbcDataReader dr = ct.ExecuteReader();
					while ( dr.Read() )
					{
						pickupAddr = String.Format("{0} {1}%R", dr["ad_str_name"].ToString(), dr["ad_city"].ToString());
						// Pickup Node
						if ( dr["pickup"].ToString().Equals(dr["stop_id"].ToString()))
						{
							msgText += VehFormatPickup(dr);
						}
					}
					msgText = String.Format("{0}{1}", pickupAddr, msgText);
				}
				conn.Close();
			}
			catch (OdbcException exc)
			{
				conn.Close();
				log.InfoFormat("Error accessing DB {0}",exc.Message);
			}
			catch (Exception exc)
			{
				conn.Close();
				log.InfoFormat("Error in SendFirstStop - {0}", exc.Message);
			}


			
			TPakMsg myTPakMsg = new TPakMsg(vehID.ToString(), "H", "T");
			myTPakMsg.Msg = msgText;
			myTPakMsg.Send();

		}


		public void GetAllStops()
		{

			if ( GetVehInfo() == false ) // couldn't find any data for this vehicle in MPK tables
				return;

			string msgText = "";
			string pickupAddr = "";
			OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MPKODBC"));

			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{

					string sqlQuery = "select * from stop,passenger,route where stop.route_id='" + this.routeID + "' and (pickup=stop_id or dropoff=stop_id) and route.route_id='" + this.routeID + "' order by sequence_nbr";
					ct.CommandText = sqlQuery;
					OdbcDataReader dr = ct.ExecuteReader();
					int count = 0;
					while ( dr.Read() )
					{
						PI_Lib.PIClient myPISocket = new PI_Lib.PIClient();
						myPISocket.SetType(MessageTypes.PI_SEND_MESSAGE);
						PI_SEND_MESSAGE mySendMessage = new PI_SEND_MESSAGE();
						mySendMessage.Fleet = Char.Parse("H");
						mySendMessage.ReceiveGroup = Convert.ToChar("Q");
						mySendMessage.ReceiveID = this.vehID.ToString().ToCharArray();

						if ( count == 0 )
							log.InfoFormat("Sending all stops for route {0} vehicle {1}", dr["route_id"].ToString(), this.vehID.ToString());

						++count;
						if ( dr["arrive"].ToString().Length > 0 && dr["depart"].ToString().Length > 0 )
							continue;
						// Pickup Node
						if ( dr["pickup"].ToString().Equals(dr["stop_id"].ToString()))
						{
							pickupAddr = String.Format("{0} {1}%R", dr["ad_str_name"].ToString(), dr["ad_city"].ToString());
							msgText = VehFormatPickup(dr);
							msgText = String.Format("{0}{1}", pickupAddr, msgText);

							//if ( msgText.Length > 200 )
							//	msgText = msgText.Substring(0, 200);  // have to 'trim' it
							msgText += "%.L" + String.Format("{0:X4}", Int32.Parse(dr["tpak_id"].ToString()) % 65535) + dr["sequence_nbr"].ToString() + dr["total_stops"].ToString() + ".";

							mySendMessage.MessageText = msgText.ToCharArray();

						}
							// Dropoff Node
						else
						{
							pickupAddr = String.Format("{0} {1}%R", dr["ad_str_name"].ToString(), dr["ad_city"].ToString());
							msgText = VehFormatDropoff(dr);
							msgText = String.Format("{0}{1}", pickupAddr, msgText);

							//if ( msgText.Length > 200 )
							//	msgText = msgText.Substring(0, 200);  // have to 'trim' it

							msgText += "%.L" + String.Format("{0:X4}", Int32.Parse(dr["tpak_id"].ToString()) % 65535) + dr["sequence_nbr"].ToString() + dr["total_stops"].ToString() + ".";

							mySendMessage.MessageText = msgText.ToCharArray();

						}
						myPISocket.sendBuf = mySendMessage.ToByteArray();

						myPISocket.SendMessage();
						myPISocket.CloseMe();
						System.Threading.Thread.Sleep(1000);
						//myPISocket.ReceiveMessage();

					}  // end while loop

					if ( count == 0 ) // No stops for this taxi
					{
						PI_Lib.PIClient myPISocket = new PI_Lib.PIClient();
						myPISocket.SetType(MessageTypes.PI_SEND_MESSAGE);
						PI_SEND_MESSAGE mySendMessage = new PI_SEND_MESSAGE();
						mySendMessage.Fleet = Char.Parse("H");
						mySendMessage.ReceiveGroup = Convert.ToChar("T");
						mySendMessage.ReceiveID = this.vehID.ToString().ToCharArray();
						mySendMessage.MessageText = "NO STOPS FOUND".ToCharArray();
						myPISocket.sendBuf = mySendMessage.ToByteArray();

						myPISocket.SendMessage();
						myPISocket.CloseMe();
					}
					dr.Close();
				}
				
				conn.Close();
			}
			catch (OdbcException exc)
			{
				conn.Close();
				log.InfoFormat("Error accessing DB {0}", exc.Message);
			}
			catch (Exception exc)
			{
				conn.Close();
				log.InfoFormat("Error in GetAllStops - {0}", exc.Message);
			}
		


		}


		public void ConfirmArrive()
		{

			// Sets routeID, currentStop, nextStop, LocX, LocY properties
			if ( GetVehInfo() == false ) // couldn't find any data for this vehicle in tables
				return;

			log.InfoFormat("Vehicle {0} confirms arrival at stop {1}", this.vehID.ToString(), currentStop);
			// Retrieve this stop from STOP table and update the arrival time with a timestamp
			

			OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MPKODBC"));
			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{
					arriveTime = System.DateTime.Now.ToString("yyyyMMdd:HHmmss");
					string sqlUpdate = "update stop set arrive='" + arriveTime + "' where stop_id='" + currentStop + "'";
					ct.CommandText = sqlUpdate;
					ct.ExecuteNonQuery();
				}
				conn.Close();
			}
			catch (OdbcException exc)
			{
				conn.Close();
				log.InfoFormat("Error accessing DB {0}",exc.Message);
			}
			catch (Exception exc)
			{
				conn.Close();
				log.InfoFormat("Error in ConfirmArrive - {0}", exc.Message);
			}
	
			// If current_stop is same as next_stop we've completed the route
			// send the <stop_visit> to MPK
			if ( this.currentStop.Equals(this.nextStop) )
			{
				this.departTime = System.DateTime.Now.ToString("yyyyMMdd:HHmmss");
				VehStopVisit();
				TPakMsg myTPakMsg = new TPakMsg(vehID.ToString(), "H", "T");
				myTPakMsg.Msg = "==REITTI LOPPUUN SUORITETTU==";
				myTPakMsg.Send();

				// update the vehicle record so current stop is null
				try
				{
					conn.Open();
					using (OdbcCommand ct = conn.CreateCommand())
					{
						string sqlUpdate = "update vehicle set current_stop='' where veh_id=" + this.vehID.ToString(); 
						ct.CommandText = sqlUpdate;
						ct.ExecuteNonQuery();
					}
					conn.Close();
				}
				catch (OdbcException exc)
				{
					conn.Close();
					log.InfoFormat("Error accessing DB {0}",exc.Message);
				}
				catch (Exception exc)
				{
					conn.Close();
					log.InfoFormat("Error in ConfirmArrive - {0}", exc.Message);
				}

			}

		}

		public void NoShow()
		{
		}

		public void ConfirmDepart()
		{

			// Sets routeID, currentStop, nextStop, LocX, LocY properties
			if ( GetVehInfo() == false )
				return;

			if ( currentStop.Length == 0 )
			{
				log.InfoFormat("Vehicle {0} - no current stops", vehID.ToString());
				return;
			}

			log.InfoFormat("Vehicle {0} confirms departure for stop {1}", vehID.ToString(), currentStop);

			// Retrieves arrive_time that should have been previously set by msg #97
			GetVehArriveTime();

			// Taxi didn't send the arrive msg before depart msg
			if ( arriveTime.Length.Equals(0) )
			{
				log.ErrorFormat("Received departure without arrival: Vehicle {0} stop {1}", vehID.ToString(), currentStop);
				return;
			}

			// Sets depart_time value in stop record
			SetVehDepartTime();

			// Send <stop_visit> message to MPK to confirm
			VehStopVisit();
			
			// Setup for next stop
			SetVehNextStop();

			// Send Next Stop information to taxi using PI client
			SendVehNextStop();

		}

		private void GetVehArriveTime()
		{

			// Retrieve current stop from DB and set depart time
			

			arriveTime=System.DateTime.Now.ToString("yyyyMMdd:HHmmss");


			OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MPKODBC"));
			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{
						
					string sqlQuery = "select arrive from stop where stop_id='" + currentStop + "'";
					ct.CommandText = sqlQuery;
					OdbcDataReader dr = ct.ExecuteReader();
					if ( dr.Read() )
						arriveTime = dr["arrive"].ToString();
					dr.Close();
					string sqlUpdate = "update stop set depart='" + departTime + "' where stop_id='" + currentStop + "'";
					ct.CommandText = sqlUpdate;
					ct.ExecuteNonQuery();

				}
				conn.Close();
			}
			catch (OdbcException exc)
			{
				conn.Close();
				log.InfoFormat("Error accessing DB {0}",exc.Message);
				
			}
			catch (Exception exc)
			{
				conn.Close();
				log.InfoFormat("Error in GetVehArriveTime - {0}", exc.Message);
			}

		}

		private void SetVehDepartTime()
		{

			departTime = System.DateTime.Now.ToString("yyyyMMdd:HHmmss");


			OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MPKODBC"));
			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{
					string sqlUpdate = "update stop set depart='" + departTime + "' where stop_id='" + currentStop + "'";
					ct.CommandText = sqlUpdate;
					ct.ExecuteNonQuery();

				}
				conn.Close();
			}
			catch (OdbcException exc)
			{
				conn.Close();
				log.InfoFormat("Error accessing DB {0}",exc.Message);
				
			}
			catch (Exception exc)
			{
				conn.Close();
				log.InfoFormat("Error in SetVehDepartTime - {0}", exc.Message);
			}

			log.InfoFormat("Veh {0} departed stop {1} at {2}", this.vehID.ToString(), this.currentStop, this.departTime);

		}

		public bool GetVehInfo()
		{


			OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MPKODBC"));
			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{
					string sqlQuery = "select * from vehicle where veh_id=" + this.vehID.ToString();
					ct.CommandText = sqlQuery;
					OdbcDataReader dr = ct.ExecuteReader();
					if ( dr.Read() )
					{
						routeID = dr["route_id"].ToString();
						currentStop = dr["current_stop"].ToString();
						nextStop = dr["next_stop"].ToString();
						LocX = Convert.ToInt32(dr["loc_x"].ToString());
						LocY = Convert.ToInt32(dr["loc_y"].ToString());
					}
					else
					{
						log.ErrorFormat("Error retrieving Vehicle {0} stop data", this.vehID.ToString());
						dr.Close();
						conn.Close();
						return false;
					}
				}
				conn.Close();
			}
			catch (OdbcException exc)
			{
				conn.Close();
				log.InfoFormat("Error accessing DB {0}",exc.Message);
				return false;
			}
			catch (Exception exc)
			{
				conn.Close();
				log.InfoFormat("Error in GetVehInfo - {0}", exc.Message);
				return false;
			}


			return true;
		}

		private void SetVehNextStop()
		{

			OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MPKODBC"));

			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{
					string sqlQuery = "select * from stop, vehicle where stop.route_id='" + routeID + "' and stop.route_id=vehicle.route_id order by sequence_nbr";
					ct.CommandText = sqlQuery;
					OdbcDataReader dr = ct.ExecuteReader();
					while( dr.Read() )
					{
						if ( dr["stop_id"].ToString().Equals(nextStop) )
						{
							currentStop = nextStop;
							if ( dr.Read() )
								nextStop = dr["stop_id"].ToString();
							if ( nextStop.Equals(currentStop) )
							{
								if ( dr.Read() )
									nextStop = dr["stop_id"].ToString();
							}
							break;
						}
					}
					dr.Close();
					//Update vehicle record with new stop info
					string sqlUpdate = "update vehicle set current_stop='" + currentStop + "',next_stop='" + nextStop + "' where vehicle.route_id='" + routeID + "' and veh_id=" + this.vehID;
					ct.CommandText = sqlUpdate;
					ct.ExecuteNonQuery();
					log.InfoFormat("Vehicle {0} Pending Stop {1} Next Stop {2}",
						vehID, currentStop, nextStop);
				}
				conn.Close();
			}
			catch (OdbcException exc)
			{
				conn.Close();
				log.InfoFormat("Error accessing DB {0}",exc.Message);
			}
			catch (Exception exc)
			{
				conn.Close();
				log.InfoFormat("Error - SetVehNextStop - {0}", exc.Message);
			}

		}


		private void VehStopVisit()
		{
			StopVisit sv = new StopVisit();
			sv.StopID = currentStop;
			sv.VehicleID = this.vehID.ToString();
			sv.ArrivalTime = arriveTime;
			sv.DepartureTime = departTime;
			sv.x = Convert.ToInt32(LocX);
			sv.y = Convert.ToInt32(LocY);
			sv.Status = "ok";
			sv.Send();

		}

		public void SendVehNextStop(string preamble)
		{

			string msgText = "";
			string pickupAddr = "";


			OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MPKODBC"));
			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{
					string sqlQuery = "select * from stop, route, passenger where stop.stop_id='" + currentStop + "' and stop.route_id=route.route_id and (pickup=stop.stop_id or dropoff=stop.stop_id)";
					ct.CommandText = sqlQuery;
					OdbcDataReader dr = ct.ExecuteReader();
					int count = 0;

					while ( dr.Read() )
					{
						++count;
						pickupAddr = String.Format("{0} {1}%R", dr["ad_str_name"].ToString(), dr["ad_city"].ToString());
						// Pickup Node
						if ( dr["pickup"].ToString().Equals(dr["stop_id"].ToString()))
						{
							msgText += VehFormatPickup(dr);
						}
							// Dropoff Node
						else
						{
							msgText += VehFormatDropoff(dr);
						}

					}
					msgText = String.Format("{0}{1}{2}", preamble, pickupAddr, msgText);
				}
				conn.Close();
			}
			catch (OdbcException exc)
			{
				conn.Close();
				log.InfoFormat("Error accessing DB {0}",exc.Message);
			}
			catch (Exception exc)
			{
				conn.Close();
				log.InfoFormat("Error - SendVehNextStop - {0}", exc.Message);
			}

			TPakMsg myTPakMsg = new TPakMsg(vehID.ToString(), "H", "T");
			myTPakMsg.Msg = msgText;
			myTPakMsg.Send();
			
		}

		public void SendVehNextStop()
		{

			string msgText = "";
			string pickupAddr = "";

			if ( this.currentStop.Length == 0 )
				return;


			OdbcConnection conn = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MPKODBC"));
			try
			{
				conn.Open();
				using (OdbcCommand ct = conn.CreateCommand())
				{
					string sqlQuery = "select * from stop, route, passenger where stop.stop_id='" + currentStop + "' and stop.route_id=route.route_id and (pickup=stop.stop_id or dropoff=stop.stop_id)";
					ct.CommandText = sqlQuery;
					OdbcDataReader dr = ct.ExecuteReader();
					int count = 0;

					while ( dr.Read() )
					{
						++count;
						pickupAddr = String.Format("{0} {1}%R", dr["ad_str_name"].ToString(), dr["ad_city"].ToString());
						// Pickup Node
						if ( dr["pickup"].ToString().Equals(dr["stop_id"].ToString()))
						{
							msgText += VehFormatPickup(dr);
						}
							// Dropoff Node
						else
						{
							msgText += VehFormatDropoff(dr);
						}

					}
					dr.Close();
					msgText = String.Format("{0}{1}", pickupAddr, msgText);
				}
				conn.Close();
			}
			catch (OdbcException exc)
			{
				conn.Close();
				log.InfoFormat("Error accessing DB {0}",exc.Message);
			}
			catch (Exception exc)
			{
				conn.Close();
				log.InfoFormat("Error - SendVehNextStop - {0}", exc.Message);
			}



			TPakMsg myTPakMsg = new TPakMsg(vehID.ToString(), "H", "T");
			myTPakMsg.Msg = msgText;
			myTPakMsg.Send();
			
		}


		private string VehFormatPickup(OdbcDataReader dr)
		{
			string returnString = "";
			try
			{
				string PickupTime = String.Format("HAKU {0}:{1} ({2}/{3})",
					dr["promised_pickup"].ToString().Substring(11,2),
					dr["promised_pickup"].ToString().Substring(14,2),
					dr["sequence_nbr"].ToString(), dr["total_stops"].ToString());

				returnString = String.Format("{0}: {1}%R", PickupTime, dr["name"].ToString());
				if ( dr["ad_note"].ToString().Length > 0 )
					returnString += String.Format("{0}%R",dr["ad_note"].ToString());

				if ( dr["phone"].ToString().Length > 0 )
					returnString += String.Format("{0}%R",dr["phone"].ToString());

				string pickupNote = dr["pickup_note"].ToString();
				pickupNote = pickupNote.Replace("\n"," ");
				pickupNote = pickupNote.Replace("\r","");
				if ( pickupNote.Length > 0 )
					returnString += String.Format("{0}%R",pickupNote);
			}
			catch (Exception exc)
			{
				log.InfoFormat("Error - VehFormatPickup - {0}", exc.Message);
				return null;
			}
			
			return(returnString);
		}

		private string VehFormatDropoff(OdbcDataReader dr)
		{
			string returnString = "";
			try
			{
				string PickupTime = String.Format("VIENTI {0}:{1} ({2}/{3})",
					dr["promised_pickup"].ToString().Substring(11,2),
					dr["promised_pickup"].ToString().Substring(14,2),
					dr["sequence_nbr"].ToString(), dr["total_stops"].ToString());

				returnString = String.Format("{0}: {1}%R", PickupTime, dr["name"].ToString());
				if ( dr["ad_note"].ToString().Length > 0 )
					returnString += String.Format("{0}%R",dr["ad_note"].ToString());

				if ( dr["phone"].ToString().Length > 0 )
					returnString += String.Format("{0}%R",dr["phone"].ToString());

				string dropNote = dr["dropoff_note"].ToString();
				dropNote = dropNote.Replace("\n"," ");
				dropNote = dropNote.Replace("\r","");
				if ( dropNote.Length > 0 )
					returnString += String.Format("{0}%R",dropNote.ToString());
			}
			catch (Exception exc)
			{
				log.InfoFormat("Error - VehFormatDropoff - {0}", exc.Message);
				return null;
			}
			return(returnString);
		}

	}
}
