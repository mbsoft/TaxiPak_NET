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
	public class RouteMonitor
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(RouteMonitor));
		public void Monitor(ArrayList newRoutes, ref ArrayList callList)
		{

			string sConn = System.Configuration.ConfigurationSettings.AppSettings["ConnString2"];
			SqlConnection conn = new SqlConnection(sConn);

			// delete the newroutes from the table

			foreach (string routeID in newRoutes)
			{

				string sQuery;

				try
				{
					conn.Open();
				}
				catch (SqlException exc)
				{
					conn.Close();
					log.InfoFormat("Error accessing DB {0}", exc.Message);
					return;
				}

				try
				{
					using (SqlCommand ct2 = conn.CreateCommand())
					{
						sQuery = "select * from route,stop,passenger where route.route_id='" + routeID + "' and route.route_id=stop.route_id and passenger.pickup=stop.stop_id order by sequence_nbr";
						ct2.CommandText = sQuery;
						SqlDataReader rdr = ct2.ExecuteReader();
						if ( rdr.Read() )
						{
							log.InfoFormat("Route {0} version {1}", rdr["route_id"].ToString(), rdr["version"].ToString());
							RouteAccept rteAccept = new RouteAccept();
							rteAccept.Accept = "yes";
							rteAccept.CompanyID = "17";
							rteAccept.RouteID = rdr["route_id"].ToString();
							rteAccept.PriceGroup = rdr["price_group"].ToString();
							rteAccept.Version = rdr["version"].ToString();
							rteAccept.Send();
							
							log.InfoFormat("First stop: {0} {1} {2}", rdr["ad_str_name"].ToString(), rdr["ad_city"].ToString(), rdr["name"].ToString());
							if ( ( rdr["route_id"].ToString().Length > 0 ) &&
								( Convert.ToInt32(rdr["tpak_id"].ToString()) > 0 ) )
							{
								CallMonitor cm = new CallMonitor(Convert.ToInt32(rdr["tpak_id"].ToString()), rdr["route_id"].ToString());
								callList.Add(cm);
								log.InfoFormat("Monitoring call {0} route {1}",
									cm.call_nbr, cm.route_id);

							}
							
						}
						else
							log.ErrorFormat("Route query failed {0}", sQuery);

						if ( Convert.ToInt32(rdr["tpak_id"].ToString()) > 0 )
						{
							rdr.Close();
							sQuery = "delete from routeNew where route_id='" + routeID + "'";
							ct2.CommandText = sQuery;
							ct2.ExecuteNonQuery();
						}
						else
							rdr.Close();
						
					}
				}
				catch (SqlException exc)
				{
					log.InfoFormat("Error accessing DB {0}", exc.Message);
				}


				conn.Close();
				
			}
			newRoutes.Clear();
		}

		public RouteMonitor()
		{
			string sConn = System.Configuration.ConfigurationSettings.AppSettings["ConnString2"];
			SqlConnection conn = new SqlConnection(sConn);
			string sQuery;
			try
			{
				conn.Open();
				using (SqlCommand ct2 = conn.CreateCommand())
				{
					sQuery = "delete from routeNew";
					ct2.CommandText = sQuery;
					ct2.ExecuteNonQuery();
				}
			}
			catch (SqlException exc)
			{
				conn.Close();
				log.InfoFormat("Error accessing DB {0}", exc.Message);
			}
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
}
