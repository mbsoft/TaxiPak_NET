using System;
using System.Threading;
using System.Configuration;
using System.Text;
using System.Collections;
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
	/// Summary description for RouteMonitor.
	/// </summary>
	public class RouteUpdateMonitor
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(RouteUpdateMonitor));
		public void Monitor(ArrayList updatedRoutes)
		{

#if MSSQL
			string sConn = System.Configuration.ConfigurationSettings.AppSettings["ConnString2"];
			SqlConnection conn = new SqlConnection(sConn); 
#else
			string sConn = System.Configuration.ConfigurationSettings.AppSettings["ConnString_new"];
			SQLiteConnection conn = new SQLiteConnection(sConn);
#endif

			// delete the updated routes from the table
			foreach (string rowid in updatedRoutes)
			{
				string sQuery;
#if MSSQL
				try
				{
					conn.Open();
					using (SqlCommand ct2 = conn.CreateCommand() )
					{
						sQuery = "delete from routeUpdate where changeID='" + rowid + "'";
						ct2.CommandText = sQuery;
						ct2.ExecuteNonQuery();
					}
					conn.Close();
				}
				catch (SqlException exc)
				{
					conn.Close();
					log.InfoFormat("Error accessing DB {0}",exc.Message);
				}
#else
				

				for ( int i = 0; i < 4; i++ ) // 4 retries
				{
					try
					{
						conn.Open();
						using (SQLiteCommand ct2 = conn.CreateCommand() )
						{
							sQuery = "delete from routeUpdate where changeID='" + rowid + "'";
							ct2.CommandText = sQuery;
							ct2.ExecuteNonQuery();
						}
						conn.Close();
						conn.Dispose();
						break;
					}
					catch (SQLiteException exc)
					{
						conn.Close();
						conn.Dispose();
						log.InfoFormat("Error accessing DB {0}-{1}",exc.Message, i);
						Thread.Sleep(1000); //wait 1 sec before next retry
						continue;
					}
				}
#endif
				
				
				#region MainRouteUpdate
#if MSSQL
				try
				{
					conn.Open();
					using (SqlCommand ct2 = conn.CreateCommand() )
					{
						sQuery = "select * from route,stop,passenger,vehicle where route.route_id='" + rowid.ToString() + "' and route.route_id=stop.route_id and route.route_id=vehicle.route_id and passenger.pickup=stop.stop_id order by stop.sequence_nbr";
						ct2.CommandText = sQuery;
						SqlDataReader rdr = ct2.ExecuteReader();

						// will only return results if a taxi has accepted the route
						if ( rdr.Read() )
						{
							Vehicle theVeh = new Vehicle(Convert.ToInt32(rdr["veh_id"].ToString()));
							theVeh.GetVehInfo();

							log.Info(String.Format("Route {0} version {1} taxi {2}",rdr["route_id"].ToString(), rdr["version"].ToString(), rdr["veh_id"].ToString()));
							// Acknowledge with an <route_accept>
							RouteAccept rteAccept = new RouteAccept();
							if ( theVeh.Wheelchair == true )
								rteAccept.VehWheels = "1";
							else
								rteAccept.VehWheels = "0";
							rteAccept.VehPax = theVeh.PassCapacity.ToString();

							rteAccept.Accept = "yes";
							rteAccept.CompanyID = "17";
							rteAccept.RouteID = rdr["route_id"].ToString();
							rteAccept.PriceGroup = rdr["price_group"].ToString();
							rteAccept.Version = rdr["version"].ToString();
							rteAccept.VehicleID = rdr["veh_id"].ToString();
							rteAccept.Send();

								
							theVeh.SendVehNextStop("==REITTI ON MUUTTUNUT==%R");
						}
						else //not assigned to a vehicle yet, just send <route_accept> stub
						{	
							rdr.Close();
							sQuery = "select * from route where route_id='" + rowid + "'";
							ct2.CommandText = sQuery;
							rdr = ct2.ExecuteReader();
							if ( rdr.Read() )
							{
								log.Info(String.Format("Route {0} version {1} taxi unassigned",rdr["route_id"].ToString(), rdr["version"].ToString()));
								RouteAccept rteAccept = new RouteAccept();
								rteAccept.Accept = "yes";
								rteAccept.CompanyID = "17";
								rteAccept.RouteID = rdr["route_id"].ToString();
								rteAccept.PriceGroup = rdr["price_group"].ToString();
								rteAccept.Version = rdr["version"].ToString();
								rteAccept.Send();
							}
						}

						rdr.Close();
					}
					conn.Close();
				}
				catch (SqlException exc)
				{
					conn.Close();
					log.InfoFormat("Error accessing DB {0}",exc.Message);
				}
#else
				for ( int i = 0; i < 4; i++ ) // 4 retries
				{
					try
					{
						conn.Open();
						using (SQLiteCommand ct2 = conn.CreateCommand() )
						{
							sQuery = "select * from route,stop,passenger,vehicle where route.route_id='" + rowid.ToString() + "' and route.route_id=stop.route_id and route.route_id=vehicle.route_id and passenger.pickup=stop.stop_id order by stop.rowid";
							ct2.CommandText = sQuery;
							SQLiteDataReader rdr = (SQLiteDataReader)ct2.ExecuteReader();

							// will only return results if a taxi has accepted the route
							if ( rdr.Read() )
							{
								Vehicle theVeh = new Vehicle(Convert.ToInt32(rdr["veh_id"].ToString()));
								theVeh.GetVehInfo();

								log.Info(String.Format("Route {0} version {1} taxi {2}",rdr["route_id"].ToString(), rdr["version"].ToString(), rdr["veh_id"].ToString()));
								// Acknowledge with an <route_accept>
								RouteAccept rteAccept = new RouteAccept();
								if ( theVeh.Wheelchair == true )
									rteAccept.VehWheels = "1";
								else
									rteAccept.VehWheels = "0";
								rteAccept.VehPax = theVeh.PassCapacity.ToString();

								rteAccept.Accept = "yes";
								rteAccept.CompanyID = "17";
								rteAccept.RouteID = rdr["route_id"].ToString();
								rteAccept.PriceGroup = rdr["price_group"].ToString();
								rteAccept.Version = rdr["version"].ToString();
								rteAccept.VehicleID = rdr["veh_id"].ToString();
								rteAccept.Send();

								
								theVeh.SendVehNextStop("==REITTI ON MUUTTUNUT==%R");
							}
							else //not assigned to a vehicle yet, just send <route_accept> stub
							{	
								rdr.Close();
								sQuery = "select * from route where route_id='" + rowid + "'";
								ct2.CommandText = sQuery;
								rdr = ct2.ExecuteReader();
								if ( rdr.Read() )
								{
									log.Info(String.Format("Route {0} version {1} taxi unassigned",rdr["route_id"].ToString(), rdr["version"].ToString()));
									RouteAccept rteAccept = new RouteAccept();
									rteAccept.Accept = "yes";
									rteAccept.CompanyID = "17";
									rteAccept.RouteID = rdr["route_id"].ToString();
									rteAccept.PriceGroup = rdr["price_group"].ToString();
									rteAccept.Version = rdr["version"].ToString();
									rteAccept.Send();
								}
							}

							rdr.Close();
							rdr.Dispose();
						}
						conn.Close();
						conn.Dispose();
						break;
					}
					catch (SQLiteException exc)
					{
						conn.Close();
						conn.Dispose();
						log.InfoFormat("Error accessing DB {0}-{1}",exc.Message, i);
						Thread.Sleep(1000); //wait 1 sec before next retry
						continue;
					}

				} // end for
				
				conn.Close();
				conn.Dispose();
#endif
				#endregion
			}
			updatedRoutes.Clear();


		}

		public RouteUpdateMonitor()
		{

		}
	}
}
