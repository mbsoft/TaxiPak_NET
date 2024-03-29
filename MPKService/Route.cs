using System;
using System.IO;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Threading;
using System.Configuration;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
#if !MSSQL
using Finisar.SQLite;
#endif
using log4net;
using log4net.Config;

namespace MPKService
{
	/// <summary>
	/// Summary description for Route.
	/// </summary>
	public class Route
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Route));
		private Ack reply;
		private bool errFlag = false;
		private string call_nbr = String.Empty;
		private string route_id = String.Empty;

		public Route(XmlNode routeNode, string checksum)
		{
			string tpakID = String.Empty;
			string vehID = String.Empty;
			TPakTrip myCanxTrip;;
			// break out route and add rows to 
			// consituent tables
			XmlTextReader rdr = new XmlTextReader(routeNode.OuterXml, XmlNodeType.Element,null);
			XmlDocument xDoc = new XmlDocument();
			xDoc.Load(rdr);

			RouteRec myRouteRec = new RouteRec();
			XmlAttributeCollection routeAttr = routeNode.Attributes;

			Int64 iRouteID = Int64.Parse(routeAttr.GetNamedItem("id").InnerXml);

			myRouteRec.RouteID = iRouteID.ToString(); //routeAttr.GetNamedItem("id").InnerXml;
			try
			{
				myRouteRec.Version = routeAttr.GetNamedItem("version").InnerXml;
				myRouteRec.Status = routeAttr.GetNamedItem("status").InnerXml;
				if ( myRouteRec.Status.Equals("canceled") )
				{
					log.InfoFormat("Route CANCEL received");
					myCanxTrip = new TPakTrip();
					string sqlConnSQL = ConfigurationSettings.AppSettings.Get("ConnStringSQL");

					SqlDataReader recrdr = null;
					SqlConnection connmssql = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));
					try
					{
						connmssql.Open();
						try
						{
							using (SqlCommand ct2 = connmssql.CreateCommand())
							{
								ct2.CommandText = "select tpak_id from route where route_id='" + myRouteRec.RouteID + "'";
								ct2.CommandType = CommandType.Text;
								
								recrdr = ct2.ExecuteReader();
								if ( recrdr.Read() )
								{
									tpakID = recrdr["tpak_id"].ToString();

									if (Convert.ToInt32(tpakID) > 0)
									{
										myCanxTrip.call_nbr = tpakID;
									}
								}
								recrdr.Close();
							}
						}
						catch (SqlException exc)
						{
							log.InfoFormat("Error accessing DB {0}", exc.Message);
						}
						connmssql.Close();
					}
					catch (SqlException exc)
					{
						log.InfoFormat("Error accessing DB {0}", exc.Message);
					}

					// Check TaxiPak database for trip assignment

					OdbcConnection connIfx = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));
					connIfx.Open();

					using (OdbcCommand ct = connIfx.CreateCommand())
					{
						string sqlQuery = "select cl_veh_nbr from calls where cl_nbr=" + tpakID;
						ct.CommandText = sqlQuery;
						OdbcDataReader dr = ct.ExecuteReader();
						if ( dr.Read() )
						{
							vehID = dr["cl_veh_nbr"].ToString().Trim();
						}
						dr.Close();
					}
					connIfx.Close();

					if ( Convert.ToInt32(vehID) > 0 )
					{
						log.InfoFormat("Can't cancel trip already assigned {0}", tpakID);
						reply = new Ack();
						reply.ackType = MPKService.Ack.AckType.route;
						reply.CheckSum = checksum;
						reply.BuildError();
						this.errFlag = true;
						return;
					}
					else
					{
						myCanxTrip.Cancel();
						reply = new Ack();
						log.InfoFormat("Cancel trip successful. Sending ACK");
						reply.ackType = MPKService.Ack.AckType.route;
						reply.CheckSum = checksum;
						reply.BuildAck();
						return;
					}

				}
			}
			catch (Exception exc)
			{
				Console.WriteLine(exc.Message);
			}

			XmlNode lengthNode = routeNode.SelectSingleNode("/route/estimated_length");
			log.InfoFormat("estimated length parsed");
			if ( lengthNode != null )
			{
				routeAttr = lengthNode.Attributes;
				myRouteRec.Length = Convert.ToInt32(routeAttr.GetNamedItem("meters").InnerXml);
				myRouteRec.Duration = Convert.ToInt32(routeAttr.GetNamedItem("minutes").InnerXml);
			}

			if ( routeNode.SelectSingleNode("/route/capacity_need/passengers") != null)
			{
				myRouteRec.CapacityPax = Convert.ToInt32(routeNode.SelectSingleNode("/route/capacity_need/passengers").InnerXml);
				log.InfoFormat("passengers parse");
			}
			if ( routeNode.SelectSingleNode("/route/capacity_need/wheelchairs") != null)
				myRouteRec.CapacityWheels = Convert.ToInt32(routeNode.SelectSingleNode("/route/capacity_need/wheelchairs").InnerXml);

			if ( routeNode.SelectSingleNode("/route/price_group") != null )
				myRouteRec.PriceGroup = routeNode.SelectSingleNode("/route/price_group").InnerXml;
			if ( routeNode.SelectSingleNode("/route/assign_before") != null )
				myRouteRec.AssignBefore = routeNode.SelectSingleNode("/route/assign_before").InnerXml;

			// this is that repeating empty route!!!
			log.InfoFormat("Route received {0}", myRouteRec.RouteID);

			//Does this route already exist?
			if ( myRouteRec.Exists() )
			{
				myRouteRec.ClearRouteData();
				UpdateRoute(myRouteRec, routeNode, checksum);
				log.InfoFormat("Route {0} update received.", myRouteRec.RouteID);
				// Insert a new record into updateroute table
				return;
			}

			log.InfoFormat("Route summary: {0}m {1}min due: {2}",myRouteRec.Length,myRouteRec.Duration,myRouteRec.AssignBefore);

			// retrieve all Stops..
			int count_stops = 0;
			foreach ( XmlNode stopNode in routeNode.SelectNodes("/route/stop") )
			{
				string xmlFrag = stopNode.OuterXml;
				XmlTextReader rdrStop = new XmlTextReader(xmlFrag, XmlNodeType.Element,null);
				XmlDocument xDocStop = new XmlDocument();
				xDocStop.Load(rdrStop);
				++count_stops;
				
				XmlAttributeCollection stopAttr = stopNode.Attributes;
				StopRec myStop = new StopRec();
				myStop.StopId = stopAttr.GetNamedItem("id").InnerXml;
				XmlNode addrNode = xDocStop.SelectSingleNode("/stop/address");

				myStop.AdStrName = addrNode.SelectSingleNode("/stop/address/street").InnerXml;
				myStop.AdStrName = myStop.AdStrName.ToUpper();
				myStop.AdCity = addrNode.SelectSingleNode("/stop/address/city").InnerXml;
				myStop.AdCity = myStop.AdCity.ToUpper();
				if ( myStop.AdCity.Equals("HELSINKI") )
					myStop.AdCity = "HEL";
				else if ( myStop.AdCity.Equals("ESPOO") )
					myStop.AdCity = "ESP";
				else if ( myStop.AdCity.Equals("KIRKKONUMMI") )
					myStop.AdCity = "KIR";
				else if ( myStop.AdCity.Equals("VANTAA") )
					myStop.AdCity = "VAN";
				else if ( myStop.AdCity.Equals("NURMIJ�RVI") )
					myStop.AdCity = "NUR";
				else if ( myStop.AdCity.Equals("KERAVA") )
					myStop.AdCity = "KER";
				else if ( myStop.AdCity.Equals("VIHTI") )
					myStop.AdCity = "VIH";
				else if ( myStop.AdCity.Equals("M�NTS�L�") )
					myStop.AdCity = "M�N";
				else if ( myStop.AdCity.Equals("J�RVENP��") )
					myStop.AdCity = "J�R";
				else if ( myStop.AdCity.Equals("SIPOO") )
					myStop.AdCity = "SIP";
				else if ( myStop.AdCity.Equals("KAUNIAINEN") )
					myStop.AdCity = "KAU";
				else if ( myStop.AdCity.Equals("SIUNTIO") )
					myStop.AdCity = "SIU";
				else if ( myStop.AdCity.Equals("INKOO") )
					myStop.AdCity = "INK";
				else if ( myStop.AdCity.Equals("HYVINK��") )
					myStop.AdCity = "HYV";
				else if ( myStop.AdCity.Equals("TUUSULA") )
					myStop.AdCity = "TUU";

				myStop.AdNote = addrNode.SelectSingleNode("/stop/address/note").InnerXml;

				XmlNode locationNode = xDocStop.SelectSingleNode("/stop/location");
				if ( locationNode != null )
				{
					stopAttr = locationNode.Attributes;
					try
					{
						string sXcoord = stopAttr.GetNamedItem("x").InnerXml.Replace(".",",");
						string sYcoord = stopAttr.GetNamedItem("y").InnerXml.Replace(".",",");
						double dTemp = Convert.ToDouble(Convert.ToDouble(sXcoord) * 10000.0);
						myStop.X = Convert.ToInt32(dTemp);
						dTemp = Convert.ToDouble(Convert.ToDouble(sYcoord) * 10000.0);
						myStop.Y = Convert.ToInt32(dTemp);
						//log.InfoFormat("Received location coords {0} - {1}", myStop.X, myStop.Y);
					}
					catch 
					{
						log.InfoFormat("Erorr with location coordinates.");
					}

				}
				myStop.Eta = xDocStop.SelectSingleNode("/stop/estimated_arrival").InnerXml;
				myStop.RouteId = myRouteRec.RouteID;
				myStop.SequenceNbr = count_stops;
				myStop.AddRec();
			}

			// retrieve all Passengers..
			foreach ( XmlNode paxNode in routeNode.SelectNodes("/route/passenger") )
			{
				string xmlFrag = paxNode.OuterXml;
				XmlTextReader rdrPax = new XmlTextReader(xmlFrag, XmlNodeType.Element,null);
				XmlDocument xDocPax = new XmlDocument();
				xDocPax.Load(rdrPax);

				PaxRec myPax = new PaxRec();
				myPax.PaxId = paxNode.Attributes.GetNamedItem("id").InnerXml;
				myPax.Name = xDocPax.SelectSingleNode("/passenger/name").InnerXml;
				myPax.Phone = xDocPax.SelectSingleNode("/passenger/phone").InnerXml;
				myPax.PromisedPickup = xDocPax.SelectSingleNode("/passenger/promised_pickup").InnerXml;
				myPax.ExtraPeople = xDocPax.SelectSingleNode("/passenger/extra_people").InnerXml;
				myPax.PickupNote = xDocPax.SelectSingleNode("/passenger/pickup_note").InnerXml;
				myPax.DropoffNote = xDocPax.SelectSingleNode("/passenger/dropoff_note").InnerXml;
				// recipient phone??
				myPax.PickupID = xDocPax.SelectSingleNode("/passenger/pickup").InnerXml;
				myPax.DropoffID = xDocPax.SelectSingleNode("/passenger/dropoff").InnerXml;

				myPax.AddRec();
				
			}

			myRouteRec.TotalStops = count_stops;
			myRouteRec.AddRec();

			reply = new Ack();
			reply.ackType = MPKService.Ack.AckType.route;
			reply.CheckSum = checksum;


			//Enter trip into TaxiPak
			string sConn = System.Configuration.ConfigurationSettings.AppSettings["ConnString2"];
			SqlConnection conn = new SqlConnection(sConn);
			string sQuery;

			TPakTrip theTrip = new TPakTrip();

			try
			{
				conn.Open();
				using (SqlCommand ct2 = conn.CreateCommand())
				{
					sQuery = "select * from route,stop,passenger where route.route_id='" + myRouteRec.RouteID + "' and route.route_id=stop.route_id and passenger.pickup=stop.stop_id order by sequence_nbr";
					ct2.CommandText = sQuery;
					SqlDataReader rdr2 = ct2.ExecuteReader();
					if ( rdr2.Read() )
					{
						log.InfoFormat("Route {0} version {1}", rdr2["route_id"].ToString(), rdr2["version"].ToString());
							
						log.InfoFormat("First stop: {0} {1} {2}", rdr2["ad_str_name"].ToString(), rdr2["ad_city"].ToString(), rdr2["name"].ToString());
						theTrip.trip_summary = String.Format("1/{0}",rdr2["total_stops"].ToString());
						theTrip.call_comment = rdr2["ad_note"].ToString();
						theTrip.from_addr_city = rdr2["ad_city"].ToString();
						theTrip.estimate = String.Format("{0} KM/{1} MIN", Convert.ToInt32(rdr2["length"].ToString())/1000, rdr2["duration"].ToString());
						theTrip.gpsx = rdr2["loc_x"].ToString();
						theTrip.gpsy = rdr2["loc_y"].ToString();

						string strName = rdr2["ad_str_name"].ToString();
						theTrip.from_addr_number = "0";
						theTrip.from_addr_street = strName;
						string [] addrTokens = strName.Trim().Split(new char[1]{' '});
						for (int i = 0; i < addrTokens.Length; i++)
						{
							if ( IsInteger(addrTokens[i]) )
							{
								theTrip.from_addr_number = addrTokens[i];
								theTrip.from_addr_street = "";
								for (int j=0; j<i; j++)
									theTrip.from_addr_street += addrTokens[j] + " ";
								if ( i < addrTokens.Length-1)
									theTrip.from_addr_suffix = addrTokens[addrTokens.Length-1];
								break;
							}
						}
						// if we still don't have an address number let's try a different approach
						// street number is possibly of the format [0-9][A-Z]
						if ( theTrip.from_addr_number.Equals("0") )
						{
							string tmpStrNbr = addrTokens[addrTokens.Length-1].Substring(0,addrTokens[addrTokens.Length-1].Length-1);
							if ( IsInteger(tmpStrNbr) )
							{
								theTrip.from_addr_street = "";
								theTrip.from_addr_number = tmpStrNbr;
								theTrip.from_addr_suffix = addrTokens[addrTokens.Length-1].Substring(addrTokens[addrTokens.Length-1].Length-1,1);
								for (int j=0; j < addrTokens.Length-1; j++)
									theTrip.from_addr_street += addrTokens[j] + " ";
							}
						}
						// still don't have a valid street number.
						// might be in format [0-9]-[0-9]
						if ( theTrip.from_addr_number.Equals("0") )
						{
							string tmpStrNbr = addrTokens[addrTokens.Length-1];
							string [] tmpTokens = tmpStrNbr.Split(new char[1]{'-'});
							if ( tmpTokens.Length > 0 )
							{
								theTrip.from_addr_street = "";
								theTrip.from_addr_number = tmpTokens[0];
								for (int j=0; j < addrTokens.Length-1; j++)
									theTrip.from_addr_street += addrTokens[j] + " ";
							}
						}



								
						theTrip.passenger = rdr2["name"].ToString();
						theTrip.route_id = rdr2["route_id"].ToString();
						theTrip.wheelchairs = rdr2["capacity_wheels"].ToString();
					}
					else
						log.ErrorFormat("Route query failed {0}", sQuery);

					rdr2.Close();
				}
			}
			catch (Exception exc)
			{
				log.InfoFormat("Error accessing DB {0}", exc.Message);
			}

			conn.Close();

            log.Info("Checking for route_id");
            if (theTrip.route_id != null)
            {
                log.Info("Preparing to dispatch to TaxiPak...");
                theTrip.Dispatch();
                //if (cm != null )
                //	callList.Add(cm);
                this.call_nbr = theTrip.call_nbr;
                this.route_id = theTrip.route_id;

                log.Info(String.Format("Dispatched TaxiPak trip #{0}", theTrip.call_nbr));


            }
            else
            {
                log.Info("No route id for trip!");
            }

		}

		public void RouteMonitor()
		{
			CallMonitor cm = new CallMonitor(Int32.Parse(call_nbr), route_id);
			cm.Monitor();
			cm.Dispose();
		}

		private void UpdateRoute(RouteRec myRouteRec, XmlNode routeNode, string checksum)
		{
			// First clear out all stops associated with this route
			//StopRec delStops = new StopRec();
			//delStops.DeleteStops(myRouteRec.RouteID);

			// retrieve and add the updated stops
			int count_stops = 0;
			foreach ( XmlNode stopNode in routeNode.SelectNodes("/route/stop") )
			{
				string xmlFrag = stopNode.OuterXml;
				XmlTextReader rdrStop = new XmlTextReader(xmlFrag, XmlNodeType.Element,null);
				XmlDocument xDocStop = new XmlDocument();
				xDocStop.Load(rdrStop);
				++count_stops;
				
				XmlAttributeCollection stopAttr = stopNode.Attributes;
				StopRec myStop = new StopRec();
				myStop.StopId = stopAttr.GetNamedItem("id").InnerXml;
				XmlNode addrNode = xDocStop.SelectSingleNode("/stop/address");

				myStop.AdStrName = addrNode.SelectSingleNode("/stop/address/street").InnerXml;
				myStop.AdStrName = myStop.AdStrName.ToUpper();
				myStop.AdCity = addrNode.SelectSingleNode("/stop/address/city").InnerXml;
				myStop.AdCity = myStop.AdCity.ToUpper();
				if ( myStop.AdCity.Equals("HELSINKI") )
					myStop.AdCity = "HEL";
				else if ( myStop.AdCity.Equals("ESPOO") )
					myStop.AdCity = "ESP";
				else if ( myStop.AdCity.Equals("KIRKKONUMMI") )
					myStop.AdCity = "KIR";
				else if ( myStop.AdCity.Equals("VANTAA") )
					myStop.AdCity = "VAN";
				else if ( myStop.AdCity.Equals("NURMIJ�RVI") )
					myStop.AdCity = "NUR";
				else if ( myStop.AdCity.Equals("KERAVA") )
					myStop.AdCity = "KER";
				else if ( myStop.AdCity.Equals("VIHTI") )
					myStop.AdCity = "VIH";
				else if ( myStop.AdCity.Equals("M�NTS�L�") )
					myStop.AdCity = "M�N";
				else if ( myStop.AdCity.Equals("J�RVENP��") )
					myStop.AdCity = "J�R";
				else if ( myStop.AdCity.Equals("SIPOO") )
					myStop.AdCity = "SIP";
				else if ( myStop.AdCity.Equals("KAUNIAINEN") )
					myStop.AdCity = "KAU";
				else if ( myStop.AdCity.Equals("SIUNTIO") )
					myStop.AdCity = "SIU";
				else if ( myStop.AdCity.Equals("INKOO") )
					myStop.AdCity = "INK";
				else if ( myStop.AdCity.Equals("HYVINK��") )
					myStop.AdCity = "HYV";
				else if ( myStop.AdCity.Equals("TUUSULA") )
					myStop.AdCity = "TUU";

				myStop.AdNote = addrNode.SelectSingleNode("/stop/address/note").InnerXml;

				XmlNode locationNode = xDocStop.SelectSingleNode("/stop/location");
				if ( locationNode != null )
				{
					stopAttr = locationNode.Attributes;
					try
					{
						string sXcoord = stopAttr.GetNamedItem("x").InnerXml.Replace(".",",");
						string sYcoord = stopAttr.GetNamedItem("y").InnerXml.Replace(".",",");
						double dTemp = Convert.ToDouble(Convert.ToDouble(sXcoord) * 10000.0);
						myStop.X = Convert.ToInt32(dTemp);
						dTemp = Convert.ToDouble(Convert.ToDouble(sYcoord) * 10000.0);
						myStop.Y = Convert.ToInt32(dTemp);
					}
					catch
					{
						log.InfoFormat("Error with location coordinates.");
					}
				}
				myStop.Eta = xDocStop.SelectSingleNode("/stop/estimated_arrival").InnerXml;
				myStop.RouteId = myRouteRec.RouteID;
				myStop.SequenceNbr = count_stops;
				myStop.AddUpdateRec();
			}

			
			// retrieve all Passengers..
			foreach ( XmlNode paxNode in routeNode.SelectNodes("/route/passenger") )
			{
				string xmlFrag = paxNode.OuterXml;
				XmlTextReader rdrPax = new XmlTextReader(xmlFrag, XmlNodeType.Element,null);
				XmlDocument xDocPax = new XmlDocument();
				xDocPax.Load(rdrPax);

				PaxRec myPax = new PaxRec();
				myPax.PaxId = paxNode.Attributes.GetNamedItem("id").InnerXml;
				myPax.Name = xDocPax.SelectSingleNode("/passenger/name").InnerXml;
				myPax.Phone = xDocPax.SelectSingleNode("/passenger/phone").InnerXml;
				myPax.PromisedPickup = xDocPax.SelectSingleNode("/passenger/promised_pickup").InnerXml;
				myPax.ExtraPeople = xDocPax.SelectSingleNode("/passenger/extra_people").InnerXml;
				myPax.PickupNote = xDocPax.SelectSingleNode("/passenger/pickup_note").InnerXml;
				myPax.DropoffNote = xDocPax.SelectSingleNode("/passenger/dropoff_note").InnerXml;
				// recipient phone??
				myPax.PickupID = xDocPax.SelectSingleNode("/passenger/pickup").InnerXml;
				myPax.DropoffID = xDocPax.SelectSingleNode("/passenger/dropoff").InnerXml;

				myPax.AddUpdateRec();
				
			}

			myRouteRec.TotalStops = count_stops;
			myRouteRec.UpdateRec();

			reply = new Ack();
			reply.ackType = MPKService.Ack.AckType.route;
			reply.CheckSum = checksum;

		}

		public string ReplyRoute()
		{
			if (errFlag)
				return reply.BuildError();
			else
				return reply.BuildAck();


		}

		private static bool IsInteger(string theValue)
		{
			try
			{
				Convert.ToInt32(theValue);
				return true;
			}
			catch
			{
				return false;
			}
		}
	}

	public class PaxRec
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(PaxRec));
		private string _pax_id;
		public string PaxId
		{
			get { return _pax_id; }
			set { _pax_id = value; }
		}
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}
		private string _phone;
		public string Phone
		{
			get { return _phone; }
			set { _phone = value; }
		}
		private string _promised_pickup;
		public string PromisedPickup
		{
			get { return _promised_pickup; }
			set { _promised_pickup = value; }
		}
		private string _extra_people;
		public string ExtraPeople
		{
			get { return _extra_people; }
			set { _extra_people = value; }
		}
		private string _pickup_note;
		public string PickupNote
		{
			get { return _pickup_note; }
			set { _pickup_note = value; }
		}
		private string _dropoff_note;
		public string DropoffNote
		{
			get { return _dropoff_note; }
			set { _dropoff_note = value; }
		}
		private string _pickup_id;
		public string PickupID
		{
			get { return _pickup_id; }
			set { _pickup_id = value; }
		}
		private string _dropoff_id; 
		public string DropoffID
		{
			get { return _dropoff_id; }
			set { _dropoff_id = value; }
		}

		public PaxRec()
		{
		}

		public void DeleteRec(string StopID)
		{
			string sqlConnSQL = ConfigurationSettings.AppSettings.Get("ConnStringSQL");
			string sqlDelete = "delete from passenger where pickup='" + StopID + "' or dropoff='" + StopID + "'";

			SqlConnection connmssql = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));
			
			try
			{
				connmssql.Open();
				using (SqlCommand ct = connmssql.CreateCommand())
				{
					ct.CommandText = sqlDelete;
					ct.ExecuteNonQuery();
				}
				connmssql.Close();
			}
			catch (Exception exc)
			{
				connmssql.Close();
				log.InfoFormat("Error accessing DB {0}", exc.Message);
			}


		}

		public void AddUpdateRec()
		{
			string sqlConnSQL = ConfigurationSettings.AppSettings.Get("ConnStringSQL");
			string sqlInsert = "insert into passenger (pass_id,name,phone,promised_pickup,extra_people,pickup_note,dropoff_note,pickup,dropoff) values ('" +
				this.PaxId + "','" +
				this.Name + "','" +
				this.Phone + "','" +
				this.PromisedPickup + "','" +
				this.ExtraPeople + "','" +
				this.PickupNote + "','" +
				this.DropoffNote + "','" +
				this.PickupID + "','" +
				this.DropoffID + "')";
			string sqlUpdate = "update passenger set name='" +
				this.Name + "',phone='" +
				this.Phone + "',promised_pickup='" +
				this.PromisedPickup + "',extra_people='" +
				this.ExtraPeople + "',pickup_note='" +
				this.PickupNote + "',dropoff_note='" +
				this.DropoffNote + "',pickup='" +
				this.PickupID + "',dropoff='" +
				this.DropoffID + "' where pass_id='" +
				this.PaxId + "'";

			string sqlCheck = "select pass_id from passenger where pass_id='" +
				this.PaxId + "'";


			SqlConnection connmssql = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));
			
			try
			{
				connmssql.Open();
				using (SqlCommand ct = connmssql.CreateCommand())
				{
					ct.CommandText = sqlCheck;
					SqlDataReader rdr = ct.ExecuteReader();
					if ( rdr.Read() ) // pax already exists
					{
						rdr.Close();
						ct.CommandText = sqlUpdate;
						ct.ExecuteNonQuery();
					}
					else
					{
						rdr.Close();
						ct.CommandText = sqlInsert;
						ct.ExecuteNonQuery();
					}
				}
				connmssql.Close();
			}
			catch (Exception exc)
			{
				connmssql.Close();
				log.InfoFormat("Error accessing DB {0}", exc.Message);
			}



		}

		public void AddRec()
		{
			string sqlConnSQL = ConfigurationSettings.AppSettings.Get("ConnStringSQL");
			string sqlInsert = "insert into passenger (pass_id,name,phone,promised_pickup,extra_people,pickup_note,dropoff_note,pickup,dropoff) values ('" +
				this.PaxId + "','" +
				this.Name + "','" +
				this.Phone + "','" +
				this.PromisedPickup + "','" +
				this.ExtraPeople + "','" +
				this.PickupNote + "','" +
				this.DropoffNote + "','" +
				this.PickupID + "','" +
				this.DropoffID + "')";


			SqlConnection connmssql = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));

			try
			{
				connmssql.Open();
				using (SqlCommand ct = connmssql.CreateCommand())
				{
					ct.CommandText = sqlInsert;
					ct.ExecuteNonQuery();
				}
				connmssql.Close();
			}
			catch (Exception exc)
			{
				connmssql.Close();
				log.InfoFormat("MSSQL exception: {0}", exc.Message);
			}

		}

	}

	public class StopRec
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(StopRec));
		private string _stop_id;
		public string StopId
		{
			get { return _stop_id; }
			set { _stop_id = value; }
		}
		private string _ad_str_name;
		public string AdStrName
		{
			get { return _ad_str_name; }
			set { _ad_str_name = value; }
		}
		private string _ad_str_nbr; 
		public string AdStrNbr
		{
			get { return _ad_str_nbr; }
			set { _ad_str_nbr = value; }
		}
		private string _ad_str_nbr_suffix;
		public string AdStrNbrSuffix
		{
			get { return _ad_str_nbr_suffix; }
			set { _ad_str_nbr_suffix = value; }
		}
		private string _ad_apart;
		public string AdApart
		{
			get { return _ad_apart; }
			set { _ad_apart = value; }
		}
		private string _ad_city;
		public string AdCity
		{
			get { return _ad_city; }
			set { _ad_city = value; }
		}
		private string _ad_note; 
		public string AdNote
		{
			get { return _ad_note; }
			set { _ad_note = value; }
		}
		private int _loc_x;
		public int X
		{
			get { return _loc_x; }
			set { _loc_x = value; }
		}
		private int _loc_y;
		public int Y
		{
			get { return _loc_y; }
			set { _loc_y = value; }
		}
		private string _eta;
		public string Eta
		{
			get { return _eta; }
			set { _eta = value; }
		}
		private string _route_id;
		public string RouteId
		{
			get { return _route_id; }
			set { _route_id = value; }
		}
		private int _sequence_nbr;
		public int SequenceNbr
		{
			get { return _sequence_nbr; }
			set { _sequence_nbr = value; }
		}


		public StopRec()
		{

		}


		public void DeleteStops(string RouteID)
		{
			string sqlConnSQL = ConfigurationSettings.AppSettings.Get("ConnStringSQL");
			string sqlDelete = "delete from stop where route_id='" + RouteID + "'" ;
			string sqlDelPax1 = "delete from passenger where dropoff=(select stop_id from stop where route_id='" + RouteID + "')";
			string sqlDelPax2 = "delete from passenger where pickup=(select stop_id from stop where route_id='" + RouteID + "')";

			SqlConnection connmssql = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));
			
			try
			{
				connmssql.Open();
				using (SqlCommand ct = connmssql.CreateCommand())
				{
					ct.CommandText = sqlDelPax1;
					ct.ExecuteNonQuery();
					ct.CommandText = sqlDelPax2;
					ct.ExecuteNonQuery();
					ct.CommandText = sqlDelete;
					ct.ExecuteNonQuery();
				}
				connmssql.Close();
			}
			catch (Exception exc)
			{
				connmssql.Close();
				log.InfoFormat("Error accessing DB {0}", exc.Message);
			}


		}

		public void AddUpdateRec()
		{
			string sqlConnSQL = ConfigurationSettings.AppSettings.Get("ConnStringSQL");

			string sqlUpdate = "update stop set ad_str_name='" +
				this.AdStrName + "',ad_str_nbr='" +
				this.AdStrNbr + "',ad_str_nbr_suffix='" +
				this.AdStrNbrSuffix + "',ad_apart='" +
				this.AdApart + "',ad_city='" +
				this.AdCity + "',ad_note='" +
				this.AdNote + "',loc_x=" +
				this.X.ToString() + ",loc_y="+
				this.Y.ToString() + ",sequence_nbr="+
				this.SequenceNbr + " where stop_id='" +
				this.StopId + "'";

			string sqlCheck = "select stop_id from stop where stop_id='" + this.StopId + "'";


			SqlConnection connmssql = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));
			

			try
			{
				connmssql.Open();

				//Check to see if the streetname has an entry in the Alias table
				try
				{
					using (SqlCommand ct = connmssql.CreateCommand())
					{
						ct.CommandType = CommandType.StoredProcedure;
						ct.CommandText = "GetAliasAddress";
						ct.Parameters.Add("@aAliasNameLong", this.AdStrName);

						//Output parameters
						SqlParameter paramStreetAddress = ct.Parameters.Add("@aStreetAddress", SqlDbType.VarChar, 50);
						paramStreetAddress.Direction = ParameterDirection.Output;
						SqlParameter paramCityLong = ct.Parameters.Add("@aCityLong", SqlDbType.VarChar, 20);
						paramCityLong.Direction = ParameterDirection.Output;
						SqlParameter paramCityShort = ct.Parameters.Add("@aCityShort", SqlDbType.VarChar, 20);
						paramCityShort.Direction = ParameterDirection.Output;
						
						SqlDataReader rdr = ct.ExecuteReader();
						string StreetName = Convert.ToString(ct.Parameters["@aStreetAddress"].Value);
						string CityName = Convert.ToString(ct.Parameters["@aCityLong"].Value);
						if (StreetName.Length > 0)
						{
							this.AdStrName = StreetName;
							log.InfoFormat("Alias match {0} --> {1}", this.AdStrName, StreetName);
						}
						if (CityName.Length > 0)
							this.AdCity = CityName;

						rdr.Close();
					}
				}
				catch (Exception e)
				{
					log.InfoFormat("Error on Alias query {0}", e.Message);
				}

				// Do the insert
				string sqlInsert = "insert into stop (stop_id,ad_str_name,ad_str_nbr,ad_str_nbr_suffix,ad_apart,ad_city,ad_note,loc_x,loc_y,eta,route_id,status,arrive,depart,sequence_nbr) values ('" +
					this.StopId + "','" +
					this.AdStrName + "','" +
					this.AdStrNbr + "','" +
					this.AdStrNbrSuffix + "','" +
					this.AdApart + "','" +
					this.AdCity + "','" +
					this.AdNote + "'," +
					this.X.ToString() + "," +
					this.Y.ToString() + ",'" +
					this.Eta + "','" +
					this.RouteId + "', 2,'',''," +
					this.SequenceNbr + ")";

				using (SqlCommand ct = connmssql.CreateCommand())
				{
					ct.CommandText = sqlCheck;
					SqlDataReader rdr = ct.ExecuteReader();
					if ( rdr.Read() ) // stop already exists
					{
						rdr.Close();
						ct.CommandText = sqlUpdate;
						ct.ExecuteNonQuery();
					}
					else
					{
						rdr.Close();
						ct.CommandText = sqlInsert;
						ct.ExecuteNonQuery();
					}
				}
				connmssql.Close();
			}
			catch (Exception exc)
			{
				connmssql.Close();
				log.InfoFormat("Error accessing DB {0}", exc.Message);
			}


		}


		public void AddRec()
		{
			string sqlConnSQL = ConfigurationSettings.AppSettings.Get("ConnStringSQL");
			

			SqlConnection connmssql = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));

			try
			{
				connmssql.Open();

				//Check to see if the streetname has an entry in the Alias table
				try
				{
					using (SqlCommand ct = connmssql.CreateCommand())
					{
						ct.CommandType = CommandType.StoredProcedure;
						ct.CommandText = "GetAliasAddress";
						ct.Parameters.Add("@aAliasNameLong", this.AdStrName);

						//Output parameters
						SqlParameter paramStreetAddress = ct.Parameters.Add("@aStreetAddress", SqlDbType.VarChar, 50);
						paramStreetAddress.Direction = ParameterDirection.Output;
						SqlParameter paramCityLong = ct.Parameters.Add("@aCityLong", SqlDbType.VarChar, 20);
						paramCityLong.Direction = ParameterDirection.Output;
						SqlParameter paramCityShort = ct.Parameters.Add("@aCityShort", SqlDbType.VarChar, 20);
						paramCityShort.Direction = ParameterDirection.Output;
						
						SqlDataReader rdr = ct.ExecuteReader();
						string StreetName = Convert.ToString(ct.Parameters["@aStreetAddress"].Value);
						string CityName = Convert.ToString(ct.Parameters["@aCityShort"].Value);
						if (StreetName.Length > 0)
						{
							log.InfoFormat("Alias match {0} --> {1}", this.AdStrName, StreetName);
							this.AdNote = this.AdStrName + "/" + this.AdNote;
							this.AdStrName = StreetName;
						}
						if (CityName.Length > 0)
							this.AdCity = CityName;

						rdr.Close();
					}

				}
				catch (Exception e)
				{
					log.InfoFormat("Error on Alias query {0}", e.Message);
				}

				
				// Do the insert
				string sqlInsert = "insert into stop (stop_id,ad_str_name,ad_str_nbr,ad_str_nbr_suffix,ad_apart,ad_city,ad_note,loc_x,loc_y,eta,route_id,status,arrive,depart,sequence_nbr) values ('" +
					this.StopId + "','" +
					this.AdStrName + "','" +
					this.AdStrNbr + "','" +
					this.AdStrNbrSuffix + "','" +
					this.AdApart + "','" +
					this.AdCity + "','" +
					this.AdNote + "'," +
					this.X.ToString() + "," +
					this.Y.ToString() + ",'" +
					this.Eta + "','" +
					this.RouteId + "', 2,'',''," +
					this.SequenceNbr + ")";


				using (SqlCommand ct = connmssql.CreateCommand())
				{
					ct.CommandText = sqlInsert;
					ct.ExecuteNonQuery();
				}
				connmssql.Close();
			}
			catch (Exception exc)
			{
				connmssql.Close();
				log.InfoFormat("MSSQL exception - StopRec.AddRec(): {0}", exc.Message);
			}

		}
	}

	public class RouteRec
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(RouteRec));
		private string _route_id;
		public string RouteID
		{
			get { return _route_id; }
			set { _route_id = value; }
		}
		private int _length;
		public int Length
		{
			get { return _length; }
			set { _length = value; }
		}
		private int _duration;
		public int Duration
		{
			get { return _duration; }
			set { _duration = value; }
		}
		private string _mandatory;
		public string Mandatory
		{
			get { return _mandatory; }
			set { _mandatory = value; }
		}
		private string _previous_route; 
		public string PrevRoute
		{
			get { return _previous_route; }
			set { _previous_route = value; }
		}
		private int _capacity_pax;
		public int CapacityPax
		{
			get { return _capacity_pax; }
			set { _capacity_pax = value; }
		}
		private int _capacity_wheels;
		public int CapacityWheels
		{
			get { return _capacity_wheels;}
			set { _capacity_wheels = value; }
		}
		private string _price_group;
		public string PriceGroup
		{
			get { return _price_group; }
			set { _price_group = value; }
		}
		private string _assign_before;
		public string AssignBefore
		{
			get { return _assign_before; }
			set { _assign_before = value; }
		}
		private string _status;
		public string Status
		{
			get { return _status; }
			set { _status = value; }
		}
		private string _version;
		public string Version
		{
			get { return _version; }
			set { _version = value; }
		}
		private int _total_stops;
		public int TotalStops
		{
			get { return _total_stops; }
			set { _total_stops = value; }
		}


		public RouteRec()
		{

		}

		public void ClearRouteData()
		{
			SqlConnection connmssql = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));
			string sqlDeletePax1 = "delete from passenger where pickup in (select stop_id from stop where route_id='" + this.RouteID + "')";
			string sqlDeletePax2 = "delete from passenger where dropoff in (select stop_id from stop where route_id='" + this.RouteID + "')";
			string sqlDeleteStop = "delete from stop where route_id='" + this.RouteID + "'";
			try
			{
				connmssql.Open();
				using (SqlCommand ct = connmssql.CreateCommand())
				{
					ct.CommandText = sqlDeletePax1;
					ct.ExecuteNonQuery();
					ct.CommandText = sqlDeletePax2;
					ct.ExecuteNonQuery();
					ct.CommandText = sqlDeleteStop;
					ct.ExecuteNonQuery();
				}
				connmssql.Close();
			}
			catch (Exception exc)
			{
				connmssql.Close();
				log.InfoFormat("MSSQL exception: {0}", exc.Message);
			}
		}

		public bool Exists()
		{
			string sqlConnSQL = ConfigurationSettings.AppSettings.Get("ConnStringSQL");
			string sqlQuery = "select * from route where route_id = '" + this.RouteID + "'";


			SqlConnection connmssql = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));

			try
			{
				connmssql.Open();
				using (SqlCommand ct = connmssql.CreateCommand())
				{
					ct.CommandText = sqlQuery;
					SqlDataReader rdr = ct.ExecuteReader();
					if ( rdr.Read() )
					{
						connmssql.Close();
						return(true);
					}
					else
					{
						connmssql.Close();
						return(false);
					}
				}
			}
			catch (Exception exc)
			{
				connmssql.Close();
				log.InfoFormat("MSSQL exception: {0}", exc.Message);
			}

			return false;

			
		}
			


		public void UpdateRec()
		{
			string sqlConnSQL = ConfigurationSettings.AppSettings.Get("ConnStringSQL");
			string sqlUpdate = "update route set length=" +
				this.Length.ToString() + ",duration=" +
				this.Duration.ToString() + ",mandatory='" +
				this.Mandatory + "',previous_route='" +
				this.PrevRoute + "',capacity_pax=" +
				this.CapacityPax.ToString() + ",capacity_wheels=" +
				this.CapacityWheels.ToString() + ", price_group='" +
				this.PriceGroup + "',assign_before='" +
				this.AssignBefore + "',status='" +
				this.Status + "',version=" +
				this.Version.ToString() + ",total_stops=" +
				this.TotalStops.ToString() + " where route_id='" +
				this.RouteID + "'";


			SqlConnection connmssql = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));

			try
			{
				connmssql.Open();
				using (SqlCommand ct = connmssql.CreateCommand())
				{
					ct.CommandText = sqlUpdate;
					ct.ExecuteNonQuery();
					ct.CommandText = "insert into routeUpdate (changeID) values ('" + this.RouteID + "')";
					ct.ExecuteNonQuery();
				}
				connmssql.Close();
			}
			catch (Exception exc)
			{
				connmssql.Close();
				log.InfoFormat("MSSQL exception RouteRec.UpdateRec(): {0}", exc.Message);
			}

		}


		public void AddRec()
		{
			string sqlConnSQL = ConfigurationSettings.AppSettings.Get("ConnStringSQL");
			string sqlInsert = "insert into route (route_id,length,duration,mandatory,previous_route,capacity_pax,capacity_wheels,price_group,assign_before,status,version,total_stops,tpak_id,veh_id) values ('" + this.RouteID + "'," +
				this.Length.ToString() + "," +
				this.Duration.ToString() + ",'" +
				this.Mandatory + "','" +
				this.PrevRoute + "'," +
				this.CapacityPax + "," +
				this.CapacityWheels + ",'" +
				this.PriceGroup + "','" +
				this.AssignBefore + "','" +
				this.Status + "'," +
				this.Version + "," +
				this.TotalStops + ",0, 0)";

			SqlConnection connmssql = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));
			string cn = ConfigurationSettings.AppSettings.Get("ConnString2");
			try
			{
				connmssql.Open();

				using (SqlCommand ct = connmssql.CreateCommand())
				{
					ct.CommandText = sqlInsert;
					ct.ExecuteNonQuery();
					ct.CommandText = "insert into routeNew (changeID, route_id) values (0, '" + this.RouteID + "')";
					ct.ExecuteNonQuery();
				}

				connmssql.Close();
			}
			catch (Exception exc)
			{
				connmssql.Close();
				log.InfoFormat("MSSQL exception (retrying) - RouteRec.AddRec(): {0}", exc.Message);
				Thread.Sleep(4000);
				try
				{
					connmssql.Open();
					using (SqlCommand ct = connmssql.CreateCommand())
					{
						ct.CommandText = sqlInsert;
						ct.ExecuteNonQuery();
						ct.CommandText = "insert into routeNew (changeID, route_id) values (0, '" + this.RouteID + "')";
						ct.ExecuteNonQuery();
					}

					connmssql.Close();
				}
				catch (Exception exc2)
				{
					connmssql.Close();
					log.InfoFormat("MSSQL exception (2nd attempt) - RouteRec.AddRec(): {0}", exc2.Message);
				}
				
			}


		}
	

	}
}
