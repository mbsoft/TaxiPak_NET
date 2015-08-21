using System;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Web;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.IO;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data.Odbc;
using log4net;
using log4net.Config;
using PI_Lib;
using Newtonsoft.Json.Linq;

namespace SUTI_svc
{
	/// <summary>
	/// Summary description for WebForm1.
	/// </summary>
	
	public enum SUTI_types
	{
		ORDER = 2000,
		ORDER_CONFIRMATION = 2001,
		ORDER_REJECT = 2002,
		REPORT = 6000,
		KEEPALIVE = 7000,
		KEEPALIVE_CONFIRMATION = 7001,
		SYNTAX_ERROR = 7030
	}

	public class WebForm1 : System.Web.UI.Page
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(WebForm1));
        
        PI_Lib.PIClient myPISocket;

        private string CityTranslate(string AdCity)
        {
            if (AdCity.Equals("HELSINKI"))
                AdCity = "HEL";
            else if (AdCity.Equals("ESPOO"))
                AdCity = "ESP";
            else if (AdCity.Equals("KIRKKONUMMI"))
                AdCity = "KIR";
            else if (AdCity.Equals("VANTAA"))
                AdCity = "VAN";
            else if (AdCity.Equals("NURMIJÄRVI"))
                AdCity = "NUR";
            else if (AdCity.Equals("KERAVA"))
                AdCity = "KER";
            else if (AdCity.Equals("VIHTI"))
                AdCity = "VIH";
            else if (AdCity.Equals("MÄNTSÄLÄ"))
                AdCity = "MÄN";
            else if (AdCity.Equals("JÄRVENPÄÄ"))
                AdCity = "JÄR";
            else if (AdCity.Equals("SIPOO"))
                AdCity = "SIP";
            else if (AdCity.Equals("KAUNIAINEN"))
                AdCity = "KAU";
            else if (AdCity.Equals("SIUNTIO"))
                AdCity = "SIU";
            else if (AdCity.Equals("INKOO"))
                AdCity = "INK";
            else if (AdCity.Equals("HYVINKÄÄ"))
                AdCity = "HYV";
            else if (AdCity.Equals("TUUSULA"))
                AdCity = "TUU";
            else if (AdCity.Equals("PORVOO"))
                AdCity = "PRV";
            else if (AdCity.Equals("ASKOLA"))
                AdCity = "ASK";
            else if (AdCity.Equals("PORNAINEN"))
                AdCity = "POR";
            else if (AdCity.Equals("PERNAJA"))
                AdCity = "PER";
            else if (AdCity.Equals("LOHJA"))
                AdCity = "LOH";
            else if (AdCity.Equals("RIIHIMÄKI"))
                AdCity = "RII";
            else if (AdCity.Equals("LOVIISA"))
                AdCity = "LOV";
            else if (AdCity.Equals("LILJENDAL"))
                AdCity = "LIL";
            else if (AdCity.Equals("LOPPI"))
                AdCity = "LOP";
            else if (AdCity.Equals("HAUSJÄRVI"))
                AdCity = "HAU";
            else if (AdCity.Equals("KARKKILA"))
                AdCity = "KAR";
            else if (AdCity.Equals("KARJAA"))
                AdCity = "KAJ";
            else if (AdCity.Equals("JANAKKALA"))
                AdCity = "JAN";
            else if (AdCity.Equals("NUMMI-PUSULA"))
                AdCity = "NUP";
            else if (AdCity.Equals("KARJALOHJA"))
                AdCity = "KAL";
            else if (AdCity.Equals("SAMMATTI"))
                AdCity = "SAM";
            else if (AdCity.Equals("PUKKILA"))
                AdCity = "PUK";
            else if (AdCity.Equals("TAMMISAARI"))
                AdCity = "TAM";
            else if (AdCity.Equals("POHJA"))
                AdCity = "POH";
            else if (AdCity.Equals("MYRSKYLÄ"))
                AdCity = "MYR";
            else if (AdCity.Equals("KÄRKÖLÄ"))
                AdCity = "KÄR";
            else if (AdCity.Equals("ORIMATTILA"))
                AdCity = "ORI";

            return (AdCity);
        }

        private void Page_Load(object sender, System.EventArgs e)
        {
            string opcode = Request.Params["op"];
            string reqXml = "";
            string sqlConnSQL = ConfigurationSettings.AppSettings.Get("SUTIODBC");
            XmlTextReader xRdr;
            XmlDocument xDoc;
            XmlNode idMsgNode;
            SUTI smsg;
            SUTI rmsg;


            Stream reqStream = Request.InputStream;
            StreamReader rdr = new StreamReader(reqStream, Encoding.GetEncoding("utf-8"));

            XmlSerializer mySerializer = new XmlSerializer(typeof(SUTI));

            try
            {
                XPathNavigator nav;
                XPathDocument docNav;
                string xPath;

                reqXml = rdr.ReadToEnd();
                //log.InfoFormat("SOAP Received: " + reqXml);

                using (StringReader sr = new StringReader(reqXml))
                {
                    docNav = new XPathDocument(sr);
                    nav = docNav.CreateNavigator();
                    xPath = "//*[local-name()='xmlstring']";

                    reqXml = nav.SelectSingleNode(xPath).Value;
                }
                log.InfoFormat("HUT -> HTD: " + reqXml);
                smsg = SUTI.Deserialize(reqXml);

                //SUTIMsg msgResponse = new SUTIMsg();
                //idType id = new idType();
                //id.id = "1234";
                //msgResponse.idMsg = id;
                //msgResponse.msgName = "Keep alive";
                //rmsg = new SUTI();
                //rmsg.msg = new System.Collections.Generic.List<SUTIMsg>();
                //rmsg.msg.Add(msgResponse);
                //log.InfoFormat(rmsg.Serialize().ToString());

            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                log.InfoFormat(exc.Message);
                Response.Redirect(@".\SUTI_2014_MessageXSD.xsd");
                return;
            }

            if (reqXml.Length == 0)
            {
                Session.Clear();
                Session.Abandon();

                Response.Redirect(@".\SUTI_2014_MessageXSD.xsd");
                return;
            }

            if (smsg.msg != null)
            {

                List<SUTIMsg> messagesIn = smsg.msg;
                messagesIn.ForEach(delegate(SUTIMsg theMsg)
                {
                    if (theMsg.msgType.Equals("7000")) // Keep alive
                    {
                        if (theMsg.idMsg != null)
                        {
                            Ping myPing = new Ping(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));
                            Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                            Response.ContentType = "text/xml";
                            //Response.Write(myPing.ReplyPing().ToCharArray(), 0, myPing.ReplyPing().Length);
                            Response.Filter = new HDIResponseFilter(Response.Filter);
                            Response.Write(myPing.QuickReply()); //sends just the ACK back. 

                            lock (Global.lockObject)
                            {
                                Global.MsgHashTable.Add(theMsg.idMsg.id, smsg);
                            }

                        }
                    }
                    if (theMsg.msgType.Equals("5020")) // Location request
                    {
                        Ping myPing = new Ping(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));
                        Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                        Response.ContentType = "text/xml";
                        Response.Filter = new HDIResponseFilter(Response.Filter);
                        Response.Write(myPing.QuickReply()); //sends just the ACK back.

                        // Send to PI handler
                        // Then acknowledge receipt
                        PI_RQST_GPS myRqstGPS = new PI_RQST_GPS();
                        myRqstGPS.veh_nbr = 465;
                        idType idVehicle = theMsg.referencesTo.idVehicle;
                        SUTI_svc.locationRequest theRequest = ((SUTI_svc.locationRequest)theMsg.Item);

                        try
                        {
                            myPISocket = new PI_Lib.PIClient();
                            log.InfoFormat("<-- Successful PI socket connection");
                        }
                        catch (System.Net.Sockets.SocketException ex)
                        {
                            log.InfoFormat("Error on PI socket ({0})", ex.Message);
                            return;
                        }
                        myPISocket.SetType(MessageTypes.PI_RQST_GPS);
                        myPISocket.sendBuf = myRqstGPS.ToByteArray(Int32.Parse(idVehicle.id));
                        //myPISocket.sendBuf = myRqstGPS.ToByteArray(Int32.Parse("9996"));
                        try
                        {
                            log.InfoFormat("<-- PI_RQST_GPS");
                            myPISocket.SendMessage();
                            myPISocket.CloseMe();

                        }
                        catch (Exception exc)
                        {
                            log.InfoFormat("<-- error on PI socket send (PI_RQST_GPS) " + exc.Message);
                            return;
                        }

                        lock (Global.lockObject)
                        {
                            //smsg.timestamp = DateTime.Now;
                            Global.MsgHashTable.Add(DateTime.Now, smsg);
                        }
                    }
                    else if (theMsg.msgType.Equals("7099"))
                    {
                        Ping myPing = new Ping(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));
                        Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                        Response.ContentType = "text/xml";
                        Response.Filter = new HDIResponseFilter(Response.Filter);
                        Response.Write(myPing.QuickReply()); //sends just the ACK back.
                    }
                    else if (theMsg.msgType.Equals("7032"))
                    {
                        // ACK handler - just provide a response
                        OrderKELA myOrderKELA = new OrderKELA(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));
                        Response.Filter = new HDIResponseFilter(Response.Filter);
                        Response.Write(myOrderKELA.QuickReply());

                    }
                    else if (theMsg.msgType.Equals("2003")) // Order Reject Confirm
                    {
                        OrderKelaRejectConfirm myOrderKELARejectConfirm = new OrderKelaRejectConfirm(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));
                        Response.Filter = new HDIResponseFilter(Response.Filter);
                        Response.Write(myOrderKELARejectConfirm.QuickReply());

                    }
                    else if (theMsg.msgType.Equals("2000"))
                    {
                        // Order handler
                        OrderKELA myOrderKELA = new OrderKELA(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));

                        SUTI_svc.order theOrder = ((SUTI_svc.order)theMsg.Item);

                        OdbcConnection connIfx = new OdbcConnection(ConfigurationManager.AppSettings.Get("MadsODBC"));

                        try
                        {
                            connIfx.Open();
                            String rteID = theOrder.idOrder.id;
                            OdbcCommand myCommand = new OdbcCommand("select * from vpu_node where rte_id='" + rteID + "'", connIfx);
                            OdbcDataReader myReader = myCommand.ExecuteReader();
                            try
                            {
                                if (myReader.Read())  // route exists....reject!!
                                {
                                    // SEND ORDER REJECT 2002
                                    OrderKELAReject or = new OrderKELAReject(rteID, "0000", smsg,
                                        Int32.Parse(Application["msgCount"].ToString()));
                                    Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                                    or.ReplyOrderCancel();
                                    myReader.Close();
                                    connIfx.Close();
                                    Response.Filter = new HDIResponseFilter(Response.Filter);
                                    Response.Write(myOrderKELA.QuickReply());
                                    return;
                                }
                                myReader.Close();
                                connIfx.Close();
                            }
                            catch (Exception exc)
                            {
                                connIfx.Close();
                            }

                        }
                        catch (Exception exc)
                        {
                            System.Diagnostics.Debug.WriteLine(exc.Message);
                        }


                        PI_DISPATCH_CALL myCall = new PI_DISPATCH_CALL();
                        myCall.call_type = ConfigurationSettings.AppSettings["CallType"].ToCharArray();
                        myCall.fleet = Convert.ToChar(ConfigurationSettings.AppSettings["FleetID"]);
                        myCall.priority = Convert.ToInt16("25");
                        myCall.number_of_calls = Convert.ToChar("1");

                        List<routeNode> rteList = theOrder.route;
                        resourceType ro = theOrder.resourceOrder;
                        if (ro.vehicle != null)
                        {
                            List<attribute> attrList = ro.vehicle.attributesVehicle;
                            string veh_attr = ConfigurationSettings.AppSettings["Vehicle_Attr"];
                            string drv_attr = ConfigurationSettings.AppSettings["Driver_Attr"];

                            if (attrList.Count > 0)
                            {
                                foreach (var idAttr in attrList)
                                {
                                    if (idAttr.idAttribute.id.Equals("1618"))  //EB
                                    {
                                        veh_attr = "EEEEEEEKEEEEEEEEEEEEEEEEEKEEKEEE";
                                    }
                                    else if (idAttr.idAttribute.id.Equals("1601"))  //EB,FA
                                    {
                                        veh_attr = "EEEKEEEKEEEEEEEEEEEEEEEEEKEEKEEE";
                                    }
                                    else if (idAttr.idAttribute.id.Equals("1619"))  //8H
                                    {
                                        veh_attr = "EKEEEEEEEEEEEEEEEEEEEEEEEKEEKEEE";
                                    }
                                    else if (idAttr.idAttribute.id.Equals("1614"))  //IN
                                    {
                                        veh_attr = "EEEEEEEEEEEEEEEEEEEEEEEEEKEEKEKE";
                                    }
                                    else if (idAttr.idAttribute.id.Equals("1615"))  //IN
                                    {
                                        veh_attr = "EEEEEEEEEEEEEEEEEEEEEEEEEKEEKEKE";
                                    }
                                    else if (idAttr.idAttribute.id.Equals("1613"))  //PA-19
                                    {
                                        veh_attr = "EEEEEEEEEEEEEEEEEEKEEEEEEKEEKEEE";
                                    }

                                }
                            }

                            capacity cap = ro.vehicle.capacity;
                            if (cap != null)
                            {
                                if (cap.seats != null)
                                {
                                    int nbrSeats = Int16.Parse(cap.seats.noOfSeats);
                                    if (nbrSeats == 4 || nbrSeats == 5)
                                        veh_attr = "K" + veh_attr.Substring(1, 31);
                                    else if (nbrSeats > 5)
                                        veh_attr = "EK" + veh_attr.Substring(2, 30);
                                }
                            }
                            manualDescriptionType md = ro.manualDescriptionResource[0];
                            if (md != null)
                            {
                                String sComment = md.manualText.Replace("Ennakkotilaus:", "").ToUpper();
                                if (sComment.Length > 64)
                                {
                                    sComment = sComment.Substring(0, 64);
                                }
                                myCall.call_comment = sComment.ToCharArray();
                            }

                            myCall.car_attrib = veh_attr.ToCharArray();
                            myCall.driver_attrib = drv_attr.ToCharArray();
                            myCall.car_number = Convert.ToInt16(ro.vehicle.idVehicle.id);
                        }

                        int pickupCount = 0;
                        int totalCount = 0;
                        int orderCount = 0;
                        Dictionary<int, string> sm = new Dictionary<int, string>();
                        foreach (var rte in rteList)
                        {
                            totalCount++;
                            System.Diagnostics.Debug.WriteLine(rte.addressNode.street);
                            List<idType> subOrderContent = rte.contents[0].subOrderContent;
                            foreach (var idOrder in subOrderContent)
                            {
                                if (idOrder.src.Contains("SUBORDERID")) //this is booking_ID
                                {
                                    //myCall.to_addr_street = idOrder.id.ToCharArray();
                                    if (!sm.ContainsValue(idOrder.id))
                                    {
                                        ++orderCount;
                                        sm.Add(orderCount, idOrder.id);            
                                    }
                                }
                            }

                            // Translation table for 'community'
                            rte.addressNode.community = CityTranslate(rte.addressNode.community.ToUpper());

                            if (rte.nodeType == nodeNodeType.pickup && pickupCount == 0)
                            {
                                ++pickupCount;
                                if (rte.addressNode.community.Length < 3)
                                {
                                    // attempt to get city from Nominatim server
                                    //String nomcity = NomCity(rte.addressNode.geographicLocation.@long.ToString(),
                                    //    rte.addressNode.geographicLocation.lat.ToString());
                                    //nomcity = CityTranslate(nomcity.ToUpper());
                                    //myCall.from_addr_city = nomcity.ToCharArray();
                                    myCall.from_addr_city = "   ".ToCharArray();
                                }
                                else
                                    myCall.from_addr_city = rte.addressNode.community.ToCharArray();
                                
                                myCall.from_addr_street = rte.addressNode.street.ToUpper().ToCharArray();
                                myCall.from_addr_number = Convert.ToInt32(rte.addressNode.streetNo);

                                // Customer phone number
                                List<contactInfo> contactList = rte.contents[0].contactInfosContent;
                                foreach (var idContact in contactList)
                                {
                                    if (idContact.contactType == contactInfoContactType.phone)
                                    {
                                        String tmpPhone = idContact.contactInfo1;
                                        tmpPhone = tmpPhone.Replace("+358", "0");
                                        myCall.phone = tmpPhone.ToCharArray();
                                    }
                                }

                                // time call or immediate call?
                                List<timesTypeTime> timesList = rte.timesNode;
                                if (timesList.Count > 0)
                                {
                                    myCall.due_date = String.Format("{0}", timesList[0].time1.ToString("ddMMyy")).ToCharArray();
                                    myCall.due_time = String.Format("{0}", timesList[0].time1.ToString("HHmm")).ToCharArray();
                                }
                                // get coordinates and convert...
                                //LLUtm llconverter = new LLUtm();
                                //double utm_east = 0.0;
                                //double utm_north = 0.0;
                                //string utm_zone = "";
                                //llconverter.LLtoUTM(rte.addressNode.geographicLocation.lat,
                                //    rte.addressNode.geographicLocation.@long, out utm_east, out utm_north, out utm_zone);
                                myCall.gpsx = rte.addressNode.geographicLocation.@long.ToString().ToCharArray();
                                myCall.gpsy = rte.addressNode.geographicLocation.lat.ToString().ToCharArray();
                            }
                            else if (rte.nodeType == nodeNodeType.destination)
                            {
                                if (rte.addressNode.community.Length < 3)
                                    myCall.to_addr_city = "   ".ToCharArray();
                                else
                                    myCall.to_addr_city = rte.addressNode.community.ToCharArray();
                                myCall.to_addr_street = rte.addressNode.street.ToUpper().ToCharArray();
                                myCall.to_addr_number = Convert.ToInt32(rte.addressNode.streetNo);
                            }
                        }
                        if (totalCount > 0)
                            //myCall.to_addr_street = "  ".ToCharArray();

                        // Send to PI handler
                        // Then acknowledge receipt
                        try
                        {
                            myPISocket = new PI_Lib.PIClient();
                            log.InfoFormat("<-- Successful PI socket connection");
                        }
                        catch (System.Net.Sockets.SocketException ex)
                        {
                            log.InfoFormat("Error on PI socket ({0})", ex.Message);
                            return;
                        }
                        myPISocket.SetType(MessageTypes.PI_DISPATCH_CALL);
                        myPISocket.sendBuf = myCall.ToByteArray();

                        try
                        {
                            log.InfoFormat("<-- Starting PI Socket SEND");
                            //System.Threading.Thread.Sleep(30000);
                            myPISocket.SendMessage();
                            log.InfoFormat("<-- Done with PI Socket SEND");
                            log.InfoFormat("<-- Starting PI Socket RECV");
                            myPISocket.ReceiveMessage();
                            log.InfoFormat("<-- Done with PI Socket RECV");

                            myCall.Deserialize(myPISocket.recvBuf);
                            myPISocket.CloseMe();
                            log.InfoFormat("<-- success send PI socket");
                        }
                        catch (Exception exc)
                        {
                            log.InfoFormat("<--- error on PI socket send " + exc.Message);
                            return;
                        }

                        myOrderKELA.LoadKelaOrderDB(smsg, theMsg, theMsg.idMsg.id, myCall.call_number);
                        myOrderKELA.CallNbr = myCall.call_number.ToString();
                        lock (Global.lockObject)
                        {

                            SUTI_svc.order od = ((SUTI_svc.order)theMsg.Item);
                            OrderMonitor om = new OrderMonitor(smsg, myOrderKELA.CallNbr, od.idOrder.id);
                            om.due_date_time = myOrderKELA._UnixTime;
                            //log.InfoFormat("OrderMonitor created: {0} - {1}", myOrderKELA.CallNbr, myCall.call_number);
                            om.orderStatus = OrderMonitor.CallStatus.NEW;
                            Global.CallHashTable.Add(om, myOrderKELA.CallNbr);
                        }

                        log.InfoFormat("*** Call {0} ***", myOrderKELA.CallNbr);
                        //Response.Write(myCall.call_number.ToString());
                        Response.Filter = new HDIResponseFilter(Response.Filter);

                        lock (Global.lockObject)
                        {
                            Global.MsgHashTable.Add(theMsg.idMsg.id, smsg);
                        }
                        Response.Write(myOrderKELA.QuickReply());

                    }
                    else if (theMsg.msgType.Equals("2010"))
                    {
                        OrderKelaCancel myOrderCancel = new OrderKelaCancel(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));

                        SUTI_svc.referencesTo refTo = ((SUTI_svc.referencesTo)theMsg.referencesTo);
                        string routeID = refTo.idOrder[0].id;
                        // Lookup tpak_nbr in vpu_node table
                        int tpak_id = 0;
                        OdbcConnection connIfx = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));
                        try
                        {
                            connIfx.Open();
                            OdbcCommand myCommand = new OdbcCommand("select tpak_id from vpu_node where rte_id='" + routeID + "'", connIfx);
                            OdbcDataReader myReader = myCommand.ExecuteReader();
                            try
                            {
                                while (myReader.Read())
                                {
                                    tpak_id = myReader.GetInt32(0);
                                }
                            }
                            catch (Exception exc)
                            {

                            }
                            connIfx.Close();
                        }
                        catch (Exception exc)
                        {

                        }
                        PI_CANCEL_CALL canxCall = new PI_CANCEL_CALL();
                        try
                        {
                            myPISocket = new PI_Lib.PIClient();
                            log.InfoFormat("<-- Successful PI socket connection");
                        }
                        catch (System.Net.Sockets.SocketException ex)
                        {
                            log.InfoFormat("Error on PI socket ({0})", ex.Message);
                            return;
                        }
                        myPISocket.SetType(MessageTypes.PI_CANCEL_CALL);
                        myPISocket.sendBuf = canxCall.ToByteArray(tpak_id);
                        try
                        {
                            log.InfoFormat("Cancelling TaxiPak order {0}", tpak_id.ToString());
                            myPISocket.SendMessage();
                            myPISocket.ReceiveMessage();
                            canxCall.Deserialize(myPISocket.recvBuf);
                            myPISocket.CloseMe();
                        }
                        catch (Exception exc)
                        {
                            log.InfoFormat("<--- error on PI socket send " + exc.Message);
                            return;
                        }
                        lock (Global.lockObject)
                        {
                            Global.MsgHashTable.Add(theMsg.idMsg.id, smsg);
                        }
                        Response.Filter = new HDIResponseFilter(Response.Filter);
                        Response.Write(myOrderCancel.QuickReply());
                    }
                    else if (theMsg.msgType.Equals("5000"))
                    {
                        // Message to Vehicle handler
                        MsgToVehicle myMsgToVehicle = new MsgToVehicle(smsg, theMsg, theMsg.idMsg.id, Int32.Parse(Application["msgCount"].ToString()));

                        SUTI_svc.msgManualDescriptionMsg msgToSend = ((SUTI_svc.msgManualDescriptionMsg)theMsg.Item);

                        // Send to PI handler
                        // Then acknowledge receipt
                        try
                        {
                            myPISocket = new PI_Lib.PIClient();
                            log.InfoFormat("<-- Successful PI socket connection");
                        }
                        catch (System.Net.Sockets.SocketException ex)
                        {
                            log.InfoFormat("Error on PI socket ({0})", ex.Message);
                            return;
                        }

                        myPISocket.SetType(MessageTypes.PI_SEND_MESSAGE);
                        PI_SEND_MESSAGE myMsg = new PI_SEND_MESSAGE();

                        myMsg.Fleet = Convert.ToChar(ConfigurationSettings.AppSettings["FleetID"]);
                        myMsg.ReceiveGroup = 'Q';
                        myMsg.ReceiveID = "3007".ToCharArray();
                        myMsg.MessageText = msgToSend.manualText.ToCharArray();

                        myPISocket.sendBuf = myMsg.ToByteArray();

                        try
                        {
                            log.InfoFormat("<-- Starting PI Socket SEND");
                            myPISocket.SendMessage();
                            log.InfoFormat("<-- Done with PI Socket SEND");
                            log.InfoFormat("<-- Starting PI Socket RECV");
                            myPISocket.ReceiveMessage();
                            log.InfoFormat("<-- Done with PI Socket RECV");

                            myMsg.Deserialize(myPISocket.recvBuf);
                            myPISocket.CloseMe();
                            log.InfoFormat("<-- success send PI socket");
                        }
                        catch
                        {
                            log.InfoFormat("<--- error on PI socket send");
                            return;
                        }
                        myPISocket.Close();

                        Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                        Response.Write(myMsgToVehicle.QuickReply());
                        lock (Global.lockObject)
                        {
                            Global.MsgHashTable.Add(theMsg.idMsg.id, smsg);
                        }
                    }
                });

                Session.Clear();
                Session.Abandon();
                return;
            }
            int endXml = reqXml.LastIndexOf("</msg>");
            string xmlFragment = reqXml.Substring(0, endXml);
            xRdr = new XmlTextReader(reqXml, XmlNodeType.Element, null);
            xDoc = new XmlDocument();
            try
            {
                xDoc.Load(xRdr);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }


            idMsgNode = xDoc.SelectSingleNode("/SUTI/msg");
            if (idMsgNode != null)
            {
                XmlNode idMsg;

                XmlAttributeCollection msgAttr = idMsgNode.Attributes;
                Int32 msgType = Int32.Parse(msgAttr.GetNamedItem("msgType").InnerXml);
                string msgName = (msgAttr.GetNamedItem("msgName").InnerXml);

                idMsg = xDoc.SelectSingleNode("/SUTI/msg/idMsg");

                XmlAttributeCollection idAttr = idMsg.Attributes;
                string sSrc = idAttr.GetNamedItem("src").InnerXml;
                string sID = idAttr.GetNamedItem("id").InnerXml;


                switch (msgType)
                {
                    case 6000: // Report -- DONE!
                        if (idMsg != null)
                        {
                            Report myReport = new Report(idMsg, Int32.Parse(Application["msgCount"].ToString()), xDoc);
                            Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                            Response.Write(myReport.ReportConfirm());
                        }
                        break;

                    case 2002: //Order reject
                        idMsg = xDoc.SelectSingleNode("/SUTI/msg/idMsg");
                        if (idMsg != null)
                        {
                            Reject myReject = new Reject(idMsg, Int32.Parse(Application["msgCount"].ToString()), xDoc);
                            Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                            Response.Write(myReject.ReplyReject());
                        }
                        break;

                    case 2010: // Order cancel
                        idMsg = xDoc.SelectSingleNode("/SUTI/msg/idMsg");
                        if (idMsg != null)
                        {
                            Cancel myCancel = new Cancel(idMsg, Int32.Parse(Application["msgCount"].ToString()), xDoc);
                            Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                            Response.Write(myCancel.ReplyCancel());
                        }
                        break;

                    case 3003: //Dispatch Confirmation - trip is assigned
                        idMsg = xDoc.SelectSingleNode("/SUTI/msg/idMsg");
                        if (idMsg != null)
                        {
                            Confirm myConfirm = new Confirm(idMsg, Int32.Parse(Application["msgCount"].ToString()), xDoc);
                            Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                            Response.Write(myConfirm.ReplyConfirm());
                        }
                        break;

                    case 2000: //ORDER
                        if (idMsgNode != null)
                        {
                            Order myOrder = new Order(reqXml, Int32.Parse(Application["msgCount"].ToString()));
                            TPakTrip theTrip = new TPakTrip();
                            if (theTrip.Fill(myOrder) == false)
                            {
                                SyntaxError mySyntaxError = new SyntaxError(sID, "Unable to process. Comment length exceeded", Int32.Parse(Application["msgCount"].ToString()));
                                Response.Write(mySyntaxError.ReplySyntaxError());
                                Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                            }
                            else
                            {

                                if (theTrip.ValidateVehicleAttr() == false)
                                {
                                    SyntaxError mySyntaxError = new SyntaxError(sID, "Unable to process. Bad vehicle attribute", Int32.Parse(Application["msgCount"].ToString()));
                                    Response.Write(mySyntaxError.ReplySyntaxError());
                                    Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                                }
                                else
                                {
                                    if (((RteNode)(myOrder._route._nodes[0])).Zone != "-1" && ((RteNode)(myOrder._route._nodes[0])).Locality.Length != 0)
                                    {
                                        myOrder.TPakID = theTrip.Dispatch();

                                        if (myOrder.TPakID > 0)
                                            myOrder.UpdateTPakID(myOrder.TPakID);
                                        else
                                            myOrder.UpdateTPakID(32142);

                                        Response.Write(myOrder.ReplyOrder());
                                        Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                                    }
                                    else if (((RteNode)(myOrder._route._nodes[0])).Locality.Length == 0)
                                    {
                                        SyntaxError mySyntaxError = new SyntaxError(sID, "Address error: No CITY", Int32.Parse(Application["msgCount"].ToString()));
                                        Response.Write(mySyntaxError.ReplySyntaxError());
                                        Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                                    }
                                    else  // Return error message
                                    {
                                        SyntaxError mySyntaxError = new SyntaxError(sID, "Zone translation error", Int32.Parse(Application["msgCount"].ToString()));
                                        Response.Write(mySyntaxError.ReplySyntaxError());
                                        Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                                    }
                                }
                            }
                        }
                        else
                        {
                            SyntaxError mySyntaxError = new SyntaxError(sID, "Order error", Int32.Parse(Application["msgCount"].ToString()));
                            Response.Write(mySyntaxError.ReplySyntaxError());
                            Application["msgCount"] = Convert.ToInt32(Application["msgCount"]) + 1;
                        }
                        break;

                    case 2003: // ORDER_REJECT_CONFIRMATION
                        break;
                }
            }
            Session.Clear();
            Session.Abandon();

        }

		#region Web Form Designer generated code
		override protected void OnInit(EventArgs e)
		{
			//
			// CODEGEN: This call is required by the ASP.NET Web Form Designer.
			//
			InitializeComponent();
			base.OnInit(e);
		}
		
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{    
			this.Load += new System.EventHandler(this.Page_Load);
		}
		#endregion
	}
}
