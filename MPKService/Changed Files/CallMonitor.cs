using System;
using System.Configuration;
using System.Text;
using System.Collections;
using System.Net;
using System.Threading;
using System.Data;
using System.Data.SqlClient;
using System.Data.Odbc;
using PI_Lib;
using IBM.Data.Informix;
using log4net;
using log4net.Config;

namespace MPKService
{
	/// <summary>
	/// Summary description for CallMonitor.
	/// </summary>
	public class CallMonitor : IDisposable
	{
		public int call_nbr;
		private int veh_nbr;
		private string cl_status;
		public string route_id;
		private static readonly ILog log = LogManager.GetLogger(typeof(CallMonitor));
		private PIClient myPISocket;
		private DateTime startTime;
		private string timeout;


		public CallMonitor(int x, string rte)
		{
			call_nbr = x;
			route_id = rte;
			veh_nbr = 0;
			startTime = DateTime.Now;

			// get attributes from trip to determine timeout value
			OdbcConnection connIfx = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));
			try
			{
				connIfx.Open();
			}
			catch (Exception exc)
			{
				log.Error(String.Format("Error opening Informix database: {0}", exc.Message));
				return;
			}
			using (OdbcCommand ct = connIfx.CreateCommand())
			{
				string sqlQuery = "select cl_veh_attr from calls where cl_nbr=" + call_nbr.ToString();
				ct.CommandText = sqlQuery;
				ct.CommandTimeout = 5;
				try
				{
					OdbcDataReader dr = ct.ExecuteReader(CommandBehavior.SingleResult);
					if ( dr.Read() )
					{
						if ( dr["cl_veh_attr"].ToString().Substring(30,1).Equals("K") )
							timeout = ConfigurationSettings.AppSettings.Get("TimeOut_wheelchair");
						else
							timeout = ConfigurationSettings.AppSettings.Get("TimeOut_default");
					}
					else
						log.Error(String.Format("Call #{0} not found in TaxiPak", call_nbr.ToString()));
					dr.Close();
				}
				catch
				{
					log.InfoFormat("Error on IFX query call #{0}", call_nbr.ToString());
					timeout = ConfigurationSettings.AppSettings.Get("TimeOut_default");
				}
			}
			connIfx.Close();


		}
		
		~CallMonitor()
		{
			Dispose(false);
		}

		public bool Monitor()
		{
			DateTime interimTime = DateTime.Now;
			TimeSpan ts = interimTime.Subtract(startTime); // Used to track how long we've been monitoring

			//connIfx = new IfxConnection(ConfigurationSettings.AppSettings.Get("MadsConnect"));
			OdbcConnection odbcIfx = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));

			try
			{
				odbcIfx.Open();
			}
			catch (OdbcException exc)
			{
				log.InfoFormat("ODBC exception {0}", exc.Message);
			}

			while (true)
			{
				interimTime = DateTime.Now;
				ts = interimTime.Subtract(startTime);

				#region main TRY block
				try
				{
					System.Threading.Thread.Sleep(10000);
					using (OdbcCommand ct = odbcIfx.CreateCommand())
					{
						string sqlQuery = "select cl_veh_nbr, cl_status from calls where cl_nbr=" + call_nbr.ToString();
						ct.CommandText = sqlQuery;
						ct.CommandTimeout = 5;
						//log.InfoFormat("Querying IFX vehicle table {0}", sqlQuery);
						try
						{
							OdbcDataReader dr = ct.ExecuteReader(CommandBehavior.SingleResult);
							if ( dr == null )
							{
								log.InfoFormat("Error with IFX reader call #{0}", call_nbr.ToString());
								odbcIfx.Close();
								return false;
							}
							if ( dr.Read() )
							{
								log.InfoFormat("Found call  {0}  Vehicle {1} Status {2}",
									call_nbr.ToString(), Convert.ToInt32(dr["cl_veh_nbr"]), dr["cl_status"].ToString());

								cl_status = dr["cl_status"].ToString().Trim();

								if ( Int32.Parse(dr["cl_veh_nbr"].ToString()) > 0 )
								{
									//log.InfoFormat("Call {0} accepted by vehicle {1}", call_nbr.ToString(), Convert.ToInt32(dr["cl_veh_nbr"]));
									veh_nbr = Convert.ToInt32(dr["cl_veh_nbr"]);
									break;
									//break so we continue vehicle acceptance processing below
								}
								else if ( cl_status.Equals("PERUTTU") ) // canceled on TaxiPak side
								{
									log.InfoFormat("Trip cancelled {0}", this.call_nbr.ToString());
									// Send new RouteAccept message
									RouteAccept rteAccept = new RouteAccept();
									rteAccept.Accept = "no";
									rteAccept.RouteID = this.route_id;
									rteAccept.Send();
									veh_nbr = 0;
									break; // return true so that this element is removed from call monitor list
								}
								else  // Check if we should cancel the trip because of duration
								{
									// Check how long we've been monitoring this trip
									if ( ts.Minutes >= Convert.ToInt32(timeout) )
									{
										log.Info(String.Format("Stop monitoring and cancel trip #{0}", this.call_nbr.ToString()));
										//Console.WriteLine("Stop Monitoring and cancel trip #{0}", this.call_nbr.ToString());
										try
										{
											myPISocket = new PIClient();
										}
										catch (System.Net.Sockets.SocketException ex)
										{
											log.Info(String.Format("Error connecting to TaxiPak for cancel: {0}", ex.Message));
											//Console.WriteLine("Error connecting to TaxiPak ({0})", ex.Message);
											break;
										}
										catch (Exception ex)
										{
											log.InfoFormat("Generic error connecting to TaxiPak for cancel: {0}", ex.Message);
											break;
										}

										myPISocket.SetType(MessageTypes.PI_CANCEL_CALL);
										PI_Lib.PI_CANCEL_CALL myCancelCall = new PI_CANCEL_CALL();
										myPISocket.sendBuf = myCancelCall.ToByteArray(Convert.ToInt32(call_nbr));
										try
										{
											myPISocket.SendMessage();
											//myPISocket.ReceiveMessage();

											//PI_CANCEL_CALL.Deserialize(myPISocket.recvBuf);
											myPISocket.CloseMe();
										}
										catch (Exception ex)
										{
											log.InfoFormat("Error cancelling trip in TaxiPak {0} {1}", call_nbr.ToString(), ex.Message);
											break;
										}
										log.InfoFormat("Trip cancelled {0}", this.call_nbr.ToString());
										// Send new RouteAccept message
										RouteAccept rteAccept = new RouteAccept();
										rteAccept.Accept = "no";
										rteAccept.RouteID = this.route_id;
										rteAccept.Send();
										break; // return true so that this element is removed from call monitor list
									}
								}
							}
							else
								log.Error(String.Format("Call #{0} not found in TaxiPak", call_nbr.ToString()));
							dr.Close();
						}
						catch (OdbcException exc)
						{
							log.InfoFormat("IFX exception on read {0}", exc.Message);
							odbcIfx.Close();
							break;
						}
					}
				}
				catch (Exception exc)
				{
					log.InfoFormat("Error on query call #{0} {1}", call_nbr.ToString(), exc.Message);
					veh_nbr = 0;
					odbcIfx.Close();
					break;
				}
				#endregion

			} // end while

			// close off the previous connection
			try
			{
				//log.InfoFormat("Closing IFX connections");
				odbcIfx.Close();
			}
			catch (Exception exc)
			{
				log.InfoFormat("Error with IFX {0}", exc.Message);
				veh_nbr=0;
				return false;
			}
		

			if ( veh_nbr > 0 ) // trip has been accepted
			{
#region vehicle accepted trip
				log.Info(String.Format("Trip #{0} accepted by taxi #{1}", call_nbr.ToString(),veh_nbr.ToString()));
				// Add a new record to vehicle table for this route
				VehicleRec acceptVehicle = new VehicleRec(veh_nbr, this.route_id);

				SqlConnection conn = new SqlConnection(ConfigurationSettings.AppSettings.Get("ConnString2"));

				try
				{
					conn.Open();
					using (SqlCommand ct = conn.CreateCommand())
					{
						ct.CommandType = CommandType.StoredProcedure;
						ct.CommandText = "UpdateRouteVehicle";
						ct.Parameters.Add("@aRouteID", route_id);
						ct.Parameters.Add("@aVehicleID", veh_nbr);
						ct.Parameters.Add("@aLocX", Convert.ToInt32(acceptVehicle.LocX));
						ct.Parameters.Add("@aLocY", Convert.ToInt32(acceptVehicle.LocY));
						ct.Parameters.Add("@aMobilePhone", acceptVehicle.MobilePhone);
						ct.Parameters.Add("@aLicense", acceptVehicle.License);
						
						ct.ExecuteNonQuery();

					}
				}
				catch (SqlException exc)
				{
					conn.Close();
					log.InfoFormat("Error accessing DB {0}", exc.Message);
				}
				catch (Exception exc)
				{
					conn.Close();
					log.InfoFormat("Error accessing DB {0}", exc.Message);
				}

				conn.Close();
				

				// Send new RouteAccept message
				RouteAccept rteAccept = new RouteAccept();
				rteAccept.VehPax = acceptVehicle.PassCapacity.ToString();
				if ( acceptVehicle.Wheelchair == true )
					rteAccept.VehWheels = "1";
				else
					rteAccept.VehWheels = "0";


				try
				{
					conn.Open();
					using (SqlCommand ct = conn.CreateCommand())
					{
						string sqlQuery = "select * from route, vehicle where route.route_id='" + this.route_id + "' and vehicle.route_id=route.route_id";
						ct.CommandText = sqlQuery;
						SqlDataReader rdr1 = ct.ExecuteReader();
						if ( rdr1.Read() )
						{
							rteAccept.RouteID = this.route_id;
							rteAccept.VehicleID = rdr1["veh_id"].ToString();
							rteAccept.Version = rdr1["version"].ToString();
							rteAccept.PriceGroup = "1";
							rteAccept.CompanyID = "17";
							rteAccept.Accept = "yes";
							rteAccept.TPakID = rdr1["tpak_id"].ToString();
							rteAccept.Send();
						}
						rdr1.Close();
					}
				}
				catch (SqlException exc)
				{
					conn.Close();
					log.InfoFormat("Error accessing DB {0}",exc.Message);
				}
				catch (Exception exc)
				{
					conn.Close();
					log.InfoFormat("Error accessing DB {0}",exc.Message);
				}

				conn.Close();
					

				//Setup the Vehicle record with the first and next STOP
				acceptVehicle.SetFirstStop(this.route_id);

				// Send the first stop to the taxi automatically
				//acceptVehicle.SendFirstStop(this.route_id);
				acceptVehicle.GetAllStops();

				return true;
#endregion
			}

			return true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if ( disposing )
			{
			}
		}

	
	}
}
