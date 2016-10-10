using System;
using System.Collections;
using System.ComponentModel;
using System.Web;
using System.Web.SessionState;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Data;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.Configuration;
using PI_Lib;
using log4net;
using log4net.Config;
using Newtonsoft.Json.Linq;

[assembly: log4net.Config.XmlConfigurator(ConfigFileExtension="log4net", Watch=true)]
namespace SUTI_svc 
{
	/// <summary>
	/// Summary description for Global.
	/// </summary>
	public class Global : System.Web.HttpApplication
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

        PI_Lib.PIClient myPISocket;

        private static readonly ILog log = LogManager.GetLogger(typeof(Global));

        public static Hashtable CallHashTable;
        public static Hashtable MsgHashTable;
        public static object lockObject = new Object();
        private static bool restart = true;

		public Global()
		{
			InitializeComponent();

		}

        protected void Application_Start(Object sender, EventArgs e)
        {
            Application.Lock();
            Application["msgCount"] = 1;
            CallHashTable = new Hashtable();
            MsgHashTable = new Hashtable();
            Thread thread = new Thread(CallBackgroundWorkThread);

            System.Net.ServicePointManager.DefaultConnectionLimit = 200;
            System.Net.ServicePointManager.MaxServicePointIdleTime = 2000;
            System.Net.ServicePointManager.MaxServicePoints = 1000;
            System.Net.ServicePointManager.SetTcpKeepAlive(false, 0, 0);

            lock (lockObject)
            {
                MsgHashTable.Clear();
                //Initialize based on current contents of Kela time calls
                OdbcConnection connIfx = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));

                DateTime thisTime = DateTime.Now;
                bool isSummertime = TimeZoneInfo.Local.IsDaylightSavingTime(thisTime);
                double currentTime;
                if (isSummertime)
                    currentTime = (DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds + 3600;
                else
                    currentTime = (DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
                try
                {
                    connIfx.Open();

                    try
                    {
                        using (OdbcCommand ct = connIfx.CreateCommand())
                        {
                            ct.CommandType = CommandType.Text;
                            ct.CommandText = "select distinct cl_nbr, cl_status, cl_extended_type, cl_due_date_time, tpak_id, rte_id  from calls, vpu_node where cl_fleet='H' and cl_pri_status = 63 and cl_due_date_time > 0 and cl_drv_id = 0 and cl_status='ODOTTAA' and cl_nbr=tpak_id order by cl_due_date_time";
                            //ct.CommandText = "select distinct cl_nbr, cl_status, cl_extended_type, cl_due_date_time, tpak_id, rte_id  from calls, vpu_node where cl_fleet='H' and cl_due_date_time > " + (currentTime - 10800) + " and cl_nbr=tpak_id order by cl_due_date_time";
                            System.Diagnostics.Debug.WriteLine(ct.CommandText);
                            using (OdbcDataReader rdr = ct.ExecuteReader())
                            {
                                while (rdr.Read()) 
                                {
                                    if (rdr[2].ToString().Contains("KVE"))
                                    {
                                        OrderMonitor om = new OrderMonitor(null, rdr[4].ToString(), rdr[5].ToString());
                                        om.due_date_time = (int)rdr[3];
                                        CallHashTable.Add(om, rdr[4].ToString());
                                    }

                                }
                                
                            }

                        }
                    }
                    catch (OdbcException exc)
                    {
                        System.Diagnostics.Debug.WriteLine(exc.Message);
                    }
                }
                catch (OdbcException exc)
                {
                    System.Diagnostics.Debug.WriteLine(exc.Message);
                }
                connIfx.Close();

            }

            thread.Start();
        }

        private void CallBackgroundWorkThread()
        {
            while (true)
            {
    
                List<DictionaryEntry> removeList = new List<DictionaryEntry>();
                List<DictionaryEntry> msgRemoveList = new List<DictionaryEntry>();
                
                
                lock (lockObject)
                {
                    
                    foreach (DictionaryEntry de in MsgHashTable)
                    {
                        SUTI smsg = (SUTI)de.Value;
                        
                        List<SUTIMsg> messagesIn = smsg.msg;
                        SUTIMsg theMsg = messagesIn[0];

                        if (theMsg.msgType.Equals("7000")) // Keep Alive
                        {
                            msgRemoveList.Add(de);
                            Ping daPing = new Ping(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));
                            daPing.ReplyPing();
                        }
                        else if (theMsg.msgType.Equals("5000")) // Message to Vehicle
                        {
                            msgRemoveList.Add(de);
                            MsgToVehicle daMsgToVehicle = new MsgToVehicle(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));
                            daMsgToVehicle.ReplyMsgToVehicle();
                        }
                        else if (theMsg.msgType.Equals("2000")) // Order
                        {
                            msgRemoveList.Add(de);
                            OrderKELA daOrder = new OrderKELA(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));
                            order theOrderDetails = (order)theMsg.Item;
                            //Find the TaxiPak ID for the order
                            String tpakID = "";                          
                            foreach (DictionaryEntry de2 in CallHashTable)
                            {
                                OrderMonitor om = (OrderMonitor)de2.Key;
                                if (om.kela_id.Equals(theOrderDetails.idOrder.id))
                                {
                                    tpakID = om.tpak_id;
                                    om.orderStatus = OrderMonitor.CallStatus.ORDERCONFIRMED;
                                    break;
                                }
                            }
                            daOrder.ReplyOrderKELA(tpakID);
                            
                        }
                        else if (theMsg.msgType.Equals("2010")) // Order cancel
                        {
                            msgRemoveList.Add(de);
                            OrderKelaCancel daCancel = new OrderKelaCancel(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));
                            daCancel.CancelConfirm(theMsg);
                        }
                        else if (theMsg.msgType.Equals("5020")) // Location request
                        {
                            msgRemoveList.Add(de);
                            //DateTime requestTime = (DateTime)de.Key;  // time that location request received
                            //DateTime thisTime = DateTime.Now;
                            //bool isSummertime = TimeZoneInfo.Local.IsDaylightSavingTime(thisTime);
                            //double currentTime;
                            //if (isSummertime)
                            //    currentTime = (DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds + 3600;
                            //else
                            //    currentTime = (DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
                            
                            //System.Diagnostics.Debug.WriteLine("Time difference: " + ((thisTime - requestTime).TotalSeconds).ToString());
                            //Not operational message
                            //_7031Response resp = new _7031Response(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));
                            //resp.ReplyNotOperational(theMsg);

                            
                            idType idVehicle = theMsg.referencesTo.idVehicle;
                            float utmNorthing = 0.0F;
                            float utmEasting = 0.0F;
                            float gpsLat = 0.0F;
                            float gpsLon = 0.0F;
                            int gpsTime = 0;
                            if (idVehicle != null)
                            {
                                //Retrieve last GPS position from this vehicle
                                VehicleLastGPS(idVehicle.id, out utmNorthing, out utmEasting, out gpsTime); 
                                System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(10800);
                                //if (isSummertime)
                                //    dtDateTime = dtDateTime.AddSeconds(gpsTime) + 3600;
                                //else
                                    dtDateTime = dtDateTime.AddSeconds(gpsTime);
                            
                                //System.Diagnostics.Debug.WriteLine("Now: " + thisTime.ToString() + " Vehicle: " + gpsTime);
                                //if ((thisTime - dtDateTime).TotalSeconds < 60)
                                //{
                                    LLUtm llutm = new LLUtm();
                                    llutm.UTMtoLL(0, (double)utmNorthing, (double)utmEasting, out gpsLat, out gpsLon);
                                    LocationResponse locationResponse = new LocationResponse(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));
                                    locationResponse.ReplyLocation(theMsg, gpsLat, gpsLon);
                                //}
                                msgRemoveList.Add(de);
                            }
                            
                        }
                    }

                    foreach (DictionaryEntry de in msgRemoveList)
                        MsgHashTable.Remove(de.Key);

                    msgRemoveList.Clear();

                    
                    // Check status of all Calls in List
                    removeList.Clear();
                    int count = 0;
                    int countChecked = 0;
                    foreach (DictionaryEntry de in CallHashTable)
                    {
                        DateTime thisTime = DateTime.Now;
                        bool isSummertime = TimeZoneInfo.Local.IsDaylightSavingTime(thisTime);
                        double currentTime;
                        if (isSummertime)
                            currentTime = (DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds + 3600;
                        else
                            currentTime = (DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;

                        OrderMonitor om = (OrderMonitor)de.Key;
                        
                        try
                        {
                            //if (om.due_date_time - currentTime < 2100)  // 35 minutes?
                            //{
                                CheckOrderStatus(om);
                                ++countChecked;
                                log.InfoFormat("Checking order " + om.tpak_id + " status " + om.orderStatus + " veh_nbr " + om.veh_nbr);
                                if (currentTime - om.due_date_time > 10800) // more than 3 hours old
                                    removeList.Add(de);
                            //}
                            ++count;
                            //log.InfoFormat("Checking order " + om.tpak_id + " status " + om.orderStatus + " veh_nbr " + om.veh_nbr);

                            if (om.orderStatus == OrderMonitor.CallStatus.CANCELED)
                            {
                                // SEND ORDER REJECT REQUEST (New message Jan 13 2016 2005
                                OrderKELAReject or = new OrderKELAReject(om.kela_id, om.tpak_id, om.inSUTImsg,
                                    Int32.Parse(Application["msgCount"].ToString()));
                                or.ReplyOrderRequestCancel();
                                removeList.Add(de);
                            }
                            else if ((om.orderStatus == OrderMonitor.CallStatus.PICKUP) && (!om.bSentPickup))
                            {
                                PickupConfirm pc = new PickupConfirm(om.kela_id, om.veh_nbr, om.tpak_id,om.inSUTImsg,
                                    Int32.Parse(Application["msgCount"].ToString()));
                                om.bSentPickup = true;
                                if (!restart)
                                    pc.ReplyPickupConfirm();

                            }
                            else if ((om.orderStatus == OrderMonitor.CallStatus.ASSIGNED || om.orderStatus == OrderMonitor.CallStatus.PICKUP) &&
                                (!om.bSentAccept))
                            {
                                // SEND DISPATCH CONFIRMATION 3003

                                DispatchConfirm dc = new DispatchConfirm(om.kela_id, om.veh_nbr,
                                    om.inSUTImsg,
                                    Int32.Parse(Application["msgCount"].ToString()));
                                om.bSentAccept = true;
                                if (!restart)
                                    dc.ReplyDispatchConfirm();
                                //okr.SendOrderKELAReject(this.inSUTImsg);
                            }
                            else if ((om.orderStatus == OrderMonitor.CallStatus.COMPLETE))
                            {
                                // ORDER REPORT 6001    
                                DispatchReport dr = new DispatchReport(om.kela_id, om.veh_nbr, om.tpak_id, om.inSUTImsg, Int32.Parse(Application["msgCount"].ToString()));
                                dr.ReplyDispatchReport();
                                removeList.Add(de);
                            }
                        }
                        catch (Exception exc)
                        {
                            log.InfoFormat(exc.Message);
                        }

                              
                    }
                    log.Info("Total VPU orders  " + count + " Orders checked " + countChecked);
                    restart = false;
                    foreach (DictionaryEntry de in removeList)
                        CallHashTable.Remove(de.Key);

                    removeList.Clear();
                }
            
                Thread.Sleep(5000);
            }

        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

		protected void Session_Start(Object sender, EventArgs e)
		{
			Console.WriteLine("Session started...");
		}

		protected void Application_BeginRequest(Object sender, EventArgs e)
		{

		}

		protected void Application_EndRequest(Object sender, EventArgs e)
		{


		}

		protected void Application_AuthenticateRequest(Object sender, EventArgs e)
		{

		}

		protected void Application_Error(Object sender, EventArgs e)
		{

		}

		protected void Session_End(Object sender, EventArgs e)
		{


		}

		protected void Application_End(Object sender, EventArgs e)
		{


		}

        private void VehicleLastGPS(string vehicleID, out float gps_lat, out float gps_lon, out int gps_time)
        {
            gps_lat = 0.0F; gps_lon = 0.0F; gps_time = 0;
            OdbcConnection connIfx = new OdbcConnection(ConfigurationManager.AppSettings.Get("MadsODBC"));
            try
            {
                connIfx.Open();
                OdbcCommand myCommand = new OdbcCommand("select tl_lat_y, tl_long_x, tl_date_time from tlayer where tl_fleet='H' and tl_veh_nbr=" + vehicleID + " order by tl_date_time desc", connIfx);
                OdbcDataReader myReader = myCommand.ExecuteReader();
                try
                {
                    if (myReader.Read())
                    {
                        gps_lat = myReader.GetFloat(0);
                        gps_lon = myReader.GetFloat(1);
                        gps_time = myReader.GetInt32(2);
                    }
                    myReader.Close();
                    myCommand.Dispose();
                    connIfx.Close();
                }
                catch
                {
                    connIfx.Close();
                }
            }
            catch
            {
                connIfx.Close();
            }
        }
        private void CheckOrderStatusDB(OrderMonitor om)
        {
            string callNbr = om.tpak_id;
            OdbcConnection connIfx = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC_prod"));
            try
            {
                connIfx.Open();
                OdbcCommand myCommand = new OdbcCommand("select cl_status, cl_veh_nbr from calls where cl_nbr=" + callNbr + " ", connIfx);
                OdbcDataReader myReader = myCommand.ExecuteReader();
                try
                {
                    if (myReader.Read())  
                    {
                        String status = myReader.GetString(0).TrimEnd();
                        if (status.Equals("PERUTTU"))
                            om.orderStatus = OrderMonitor.CallStatus.CANCELED;
                        else if (status.Equals("VALMIS"))
                            om.orderStatus = OrderMonitor.CallStatus.COMPLETE;
                        else if (status.Equals("AVOIN"))
                            om.orderStatus = OrderMonitor.CallStatus.UNASSIGNED;
                        else if (status.Equals("ODOTTAA"))
                            om.orderStatus = OrderMonitor.CallStatus.PENDING;
                        else if (status.Equals("NOUTO"))
                            om.orderStatus = OrderMonitor.CallStatus.PICKUP;
                        else if (status.Equals("VLITETTY"))
                            om.orderStatus = OrderMonitor.CallStatus.ASSIGNED;
                        om.veh_nbr = myReader.GetString(1);
                    }
                    myReader.Close();
                    myCommand.Dispose();
                    connIfx.Close();
                }
                catch
                {
                    connIfx.Close();
                }
                connIfx.Close();
            }
            catch
            {

            }
        }

	    private void CheckOrderStatus(OrderMonitor om)
        {
            string callNbr = om.tpak_id;
            PI_GET_CALL checkCall = new PI_GET_CALL();
            PI_DISPATCH_CALL returnedCall = new PI_DISPATCH_CALL();

            checkCall.call_number = Int32.Parse(callNbr);

            // Send to PI handler
            try
            {
                myPISocket = new PI_Lib.PIClient();
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                log.InfoFormat("Error on PI socket ({0})", ex.Message);
                return;
            }
            myPISocket.SetType(MessageTypes.PI_GET_CALL);
            myPISocket.sendBuf = checkCall.ToByteArray();

            try
            {
                //log.InfoFormat("<-- Starting PI Socket SEND");
                myPISocket.SendMessage();
                //log.InfoFormat("<-- Done with PI Socket SEND");
                //log.InfoFormat("<-- Starting PI Socket RECV");
                myPISocket.ReceiveMessage();
                //log.InfoFormat("<-- Done with PI Socket RECV");

                //#define PENDING                 1       /* The call is a future-call (waiting)          */
                //#define UNASSIGNED              2       /* The call is not connected to a taxi yet      */
                //#define ASSIGNED                3       /* The taxi is driver to the customer           */
                //#define PICKUP                  4       /* The taxi-trip is going on                    */
                //#define COMPLETE                5       /* The taxi-trip has ended                      */
                //#define CANCELLED               6       /* The call has been cancelled                  */
                //#define NOEXIST                 7       /* The call does not exist                      */

                checkCall.Deserialize(ref returnedCall, myPISocket.recvBuf);
                myPISocket.CloseMe();
                //log.InfoFormat("<-- success send PI socket");
                // ahead of ourselves. We'll get the order confirm first and pick this up on the next cycle
                if ((OrderMonitor.CallStatus)(returnedCall.call_status) == OrderMonitor.CallStatus.ASSIGNED &&
                    om.orderStatus == OrderMonitor.CallStatus.NEW)
                    return; 
                om.orderStatus = (OrderMonitor.CallStatus)(returnedCall.call_status);
                om.veh_nbr = returnedCall.car_number.ToString();
                return;
            }
            catch (Exception exc)
            {
                log.InfoFormat("<--- error on PI socket send " + exc.Message);
                return;
            }

        }
		#region Web Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{    
			this.components = new System.ComponentModel.Container();
		}
		#endregion
	}
}

