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
	/// Summary description for CallMonitor.
	/// </summary>
	public class CallMonitor
	{
		public int call_nbr;
		public string route_id;
		private static readonly ILog log = LogManager.GetLogger(typeof(CallMonitor));
		private DateTime startTime;
		private string timeout;

		public CallMonitor(int x, string rte)
		{
			call_nbr = x;
			route_id = rte;
			startTime = DateTime.Now;

			// get attributes from trip to determine timeout value
			OdbcConnection connIfx = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsConnect"));
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
				try
				{
					OdbcDataReader dr = ct.ExecuteReader();
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
		


	}
}
