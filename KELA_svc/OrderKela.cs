using System;
using System.Web;
using System.Web.SessionState;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using log4net;
using log4net.Config;
using System.Data;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.Configuration;

namespace SUTI_svc
{
    /// <summary>
    /// Summary description for OrderKELA.
    /// </summary>
    public class OrderKELA
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(OrderKELA));
        private string sSrc;
        private string sID;
        private int msgCount;
        private SUTI inSUTI;
        private SUTIMsg inSUTImsg;
        private string _call_number;

        public double _UnixTime;
        public string CallNbr
        {
            get { return _call_number; }
            set { _call_number = value; }
        }

        public OrderKELA(SUTI from, SUTIMsg msgFrom, string msgID, int msgCounter)
        {
            inSUTI = from;
            inSUTImsg = msgFrom;
            sID = msgID;
            msgCount = msgCounter;

        }

        public string QuickReply()
        {
            String response = "<SOAP-ENV:Envelope xmlns:SOAP-ENC='http://schemas.xmlsoap.org/soap/encoding/' xmlns:SOAP-ENV='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ZSI='http://www.zolera.com/schemas/ZSI/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'><SOAP-ENV:Header></SOAP-ENV:Header><SOAP-ENV:Body xmlns:ns1='http://tempuri.org/'><ns1:ReceiveSutiMsgResponse><ns1:ReceiveSutiMsgResult>" +
                                "1</ns1:ReceiveSutiMsgResult></ns1:ReceiveSutiMsgResponse></SOAP-ENV:Body></SOAP-ENV:Envelope>";

            log.InfoFormat("HTD->HUT " + response);

            return response;
        }

        public static string Kela_CalcChecksum(string aKelaIdBody)
        {
            int pMultiplier = 1;
            int pCumulativeSum = 0;
            int pSingleMultiply;

            for (int i = aKelaIdBody.Length - 1; i >= 0; i--)
            {
                if (pMultiplier == 1) { pMultiplier = 2; } else { pMultiplier = 1; };

                pSingleMultiply = pMultiplier * Convert.ToInt32(aKelaIdBody.Substring(i, 1));
                if (pSingleMultiply > 9) { pSingleMultiply -= 9; }

                pCumulativeSum += pSingleMultiply;
            }

            int pCheckSum = 10 - (pCumulativeSum % 10);
            if (pCheckSum == 10) { pCheckSum = 0; }


            return Convert.ToString(pCheckSum);
        }

        public static string Kela_AddChecksumsToKelaIdBody(string aKelaIdBody)
        {
            string pRetval = "";
            aKelaIdBody += Kela_CalcChecksum(aKelaIdBody);
            aKelaIdBody += Kela_CalcChecksum(aKelaIdBody);
            pRetval = aKelaIdBody;
            return pRetval;
        }

        public static string Kela_IsKelaIdOk(string aFullKelaId)
        {
            aFullKelaId = Kela_AddChecksumsToKelaIdBody(aFullKelaId.Substring(aFullKelaId.Length-4, 4));

            return aFullKelaId;        
        }

        public void UpdateKelaOrderDB(SUTI from, SUTIMsg msgFrom, string msgID, int tpakCallNbr)
        {
            inSUTI = from;
            inSUTImsg = msgFrom;
            sID = msgID;

            OdbcConnection connIfx = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));
            try
            {
                connIfx.Open();

                // increment version number in all prior nodes
                OdbcCommand myCommand = new OdbcCommand("update kela_node set version=(version+1) where tpak_id=" + tpakCallNbr, connIfx);
                myCommand.ExecuteNonQuery();
                
                SUTI_svc.order theOrder = ((SUTI_svc.order)msgFrom.Item);

                List<routeNode> rteList = theOrder.route;
                String passenger = "";
                String datetime = String.Empty;
                String phonenbr = String.Empty;
                String booking_id = String.Empty;
                String short_booking_id = String.Empty;
                String last_booking_id = String.Empty;
                String manual_text = String.Empty;
                int sequence_nbr = 1;

                foreach (var rte in rteList)
                {

                    List<content> nodeContent = rte.contents;

                    if (nodeContent.Count > 0)
                    {
                        passenger = nodeContent[0].name;  //taking the first listed passenger
                        contentIdContent bdayContent = nodeContent[0].idContent;
                        if (bdayContent.id.Length >= 6)
                        {
                            if (Int16.Parse(bdayContent.id.Substring(4, 2)) <= 15)
                                passenger = passenger + "/20" + bdayContent.id.Substring(4, 2);
                            else
                                passenger = passenger + "/19" + bdayContent.id.Substring(4, 2);
                        }


                        List<contactInfo> contactContent = nodeContent[0].contactInfosContent;
                        if (contactContent.Count > 0)
                            phonenbr = contactContent[0].contactInfo1;

                        List<idType> subOrderContent = nodeContent[0].subOrderContent;
                        foreach (var idOrder in subOrderContent)
                        {
                            if (idOrder.src.Equals("KELA_TRIP_ID")) //this is booking_ID
                            {
                                //shorten to 6 chars according to Kela shortening algo
                                booking_id = idOrder.id;

                                short_booking_id = Kela_IsKelaIdOk(booking_id);
                            }
                        }
                        List<manualDescriptionType> mda1 = nodeContent[0].manualDescriptionContent;
                        if (mda1.Count > 0)
                            manual_text = mda1[0].manualText;
                    }

                    List<timesTypeTime> timeContent = rte.timesNode;

                    //double UnixTime = 0;
                    int nodeSeqno = Int16.Parse(rte.nodeSeqno);
                    if (timeContent.Count > 0)
                    {
                        datetime = timeContent[0].time1.ToString("yyyy-MM-ddTHH:mm:ss");
                        if (nodeSeqno == 1)
                        {
                            double nowtime = (DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
                            _UnixTime = (timeContent[0].time1.ToLocalTime() - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds - 7200;
                        }
                    }


                    if (rte.addressNode.streetNo == null)
                        rte.addressNode.streetNo = "0";

                    List<manualDescriptionType> mda = rte.addressNode.manualDescriptionAddress;
                    if (mda.Count > 0)
                        manual_text += " " + mda[0].manualText;


                    try
                    {
                        using (OdbcCommand ct = connIfx.CreateCommand())
                        {
                            ct.CommandType = CommandType.Text;

                            ct.CommandText = String.Format(new System.Globalization.CultureInfo("en-US"), "insert into kela_node values ('{0}',{1},{2},'{3}','{4}', {5}, '{6}', '{7}', '{8}', '{9}', '{10}', {11}, {12},'{13}','{14}','{15}','{16}','{17}','{18}',{19})",
                                theOrder.idOrder.id,
                                nodeSeqno,
                                tpakCallNbr,
                                (rte.nodeType.ToString() == "pickup" ? "P" : "D"),
                                rte.addressNode.street.Replace("'", "''"),
                                Int32.Parse(rte.addressNode.streetNo),
                                rte.addressNode.streetNoLetter,
                                rte.addressNode.community,
                                datetime,
                                (passenger.Length > 0 ? passenger : ""),
                                (phonenbr.Length > 0 ? phonenbr : ""),
                                rte.addressNode.geographicLocation.lat,
                                rte.addressNode.geographicLocation.@long,
                                "",
                                "",
                                "",
                                short_booking_id, booking_id, manual_text, 0
                                );

                            log.InfoFormat(ct.CommandText);
                            ct.ExecuteNonQuery();
                            //connIfx.Close();
                        }
                    }
                    catch (Exception exc)
                    {
                        using (OdbcCommand ct = connIfx.CreateCommand())
                        {
                            ct.CommandType = CommandType.Text;
                            ct.CommandText = String.Format(new System.Globalization.CultureInfo("en-US"),
                                "update kela_node set tpak_id='" + tpakCallNbr.ToString() + "' where rte_id='" +
                                theOrder.idOrder.id + "'");
                            ct.ExecuteNonQuery();
                            //connIfx.Close();
                        }
                    }
                    //}


                }

                connIfx.Close();

            }
            catch (Exception exc)
            {
                log.Error(String.Format("Error ]inserting Informix database (reattempt as UPDATE): {0}", exc.Message));
                // Try UPDATE instead


            }
            return;

        }

        public void LoadKelaOrderDB(SUTI from, SUTIMsg msgFrom, string msgID, int tpakCallNbr)
        {
            inSUTI = from;
            inSUTImsg = msgFrom;
            sID = msgID;
            

            OdbcConnection connIfx = new OdbcConnection(ConfigurationSettings.AppSettings.Get("MadsODBC"));
            try
            {
                connIfx.Open();

                SUTI_svc.order theOrder = ((SUTI_svc.order)msgFrom.Item);

                List<routeNode> rteList = theOrder.route;
                String passenger = "";
                String datetime = String.Empty;
                String phonenbr = String.Empty;
                String booking_id = String.Empty;
                String short_booking_id = String.Empty;
                String last_booking_id = String.Empty;
                String manual_text = String.Empty;
                int sequence_nbr = 1;

                foreach (var rte in rteList)
                {

                    List<content> nodeContent = rte.contents;

                    if (nodeContent.Count > 0)
                    {
                        passenger = nodeContent[0].name;  //taking the first listed passenger
                        contentIdContent bdayContent = nodeContent[0].idContent;
                        if (bdayContent.id.Length >= 6)
                        {
                            if (Int16.Parse(bdayContent.id.Substring(4,2)) <= 15)
                                passenger = passenger + "/20" + bdayContent.id.Substring(4, 2);
                            else
                                passenger = passenger + "/19" + bdayContent.id.Substring(4, 2);
                        }


                        List<contactInfo> contactContent = nodeContent[0].contactInfosContent;
                        if (contactContent.Count > 0)
                            phonenbr = contactContent[0].contactInfo1;
                        
                        List<idType> subOrderContent = nodeContent[0].subOrderContent;
                        foreach (var idOrder in subOrderContent)
                        {
                            if (idOrder.src.Equals("KELA_TRIP_ID")) //this is booking_ID
                            {
                                //shorten to 6 chars according to Kela shortening algo
                                booking_id = idOrder.id;

                                short_booking_id = Kela_IsKelaIdOk(booking_id);
                            }
                        }
                        List<manualDescriptionType> mda1 = nodeContent[0].manualDescriptionContent;
                        if (mda1.Count > 0)
                            manual_text = mda1[0].manualText;
                    }

                    List<timesTypeTime> timeContent = rte.timesNode;
                    
                    //double UnixTime = 0;
                    int nodeSeqno = Int16.Parse(rte.nodeSeqno);
                    if (timeContent.Count > 0)
                    {
                        datetime = timeContent[0].time1.ToString("yyyy-MM-ddTHH:mm:ss");
                        if (nodeSeqno == 1)
                        {
                            double nowtime = (DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
                            _UnixTime = (timeContent[0].time1.ToLocalTime() - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds - 7200;
                        }
                     }
                    

                    if (rte.addressNode.streetNo == null)
                        rte.addressNode.streetNo = "0";

                    List<manualDescriptionType> mda = rte.addressNode.manualDescriptionAddress;
                    if (mda.Count > 0)
                        manual_text += " " + mda[0].manualText;

                    
                    //if (last_booking_id != booking_id || last_booking_id.Equals(String.Empty))
                    //{
                    //    last_booking_id = booking_id;
                    
                        try
                        {
                            using (OdbcCommand ct = connIfx.CreateCommand())
                            {
                                ct.CommandType = CommandType.Text;

                                ct.CommandText = String.Format(new System.Globalization.CultureInfo("en-US"), "insert into kela_node values ('{0}',{1},{2},'{3}','{4}', {5}, '{6}', '{7}', '{8}', '{9}', '{10}', {11}, {12},'{13}','{14}','{15}','{16}','{17}','{18}',{19})",
                                    theOrder.idOrder.id,
                                    nodeSeqno,
                                    tpakCallNbr,
                                    (rte.nodeType.ToString() == "pickup" ? "P" : "D"),
                                    rte.addressNode.street.Replace("'","''"),
                                    Int32.Parse(rte.addressNode.streetNo),
                                    rte.addressNode.streetNoLetter,
                                    rte.addressNode.community,
                                    datetime,
                                    (passenger.Length > 0 ? passenger : ""),
                                    (phonenbr.Length > 0 ? phonenbr : ""),
                                    rte.addressNode.geographicLocation.lat,
                                    rte.addressNode.geographicLocation.@long,
                                    "",
                                    "",
                                    "",
                                    short_booking_id, booking_id, manual_text, 0
                                    );

                                log.InfoFormat(ct.CommandText);
                                ct.ExecuteNonQuery();
                                //connIfx.Close();
                            }
                        }
                        catch (Exception exc)
                        {
                            using (OdbcCommand ct = connIfx.CreateCommand())
                            {
                                ct.CommandType = CommandType.Text;
                                ct.CommandText = String.Format(new System.Globalization.CultureInfo("en-US"),
                                    "update kela_node set tpak_id='" + tpakCallNbr.ToString() + "' where rte_id='" +
                                    theOrder.idOrder.id + "'");
                                ct.ExecuteNonQuery();
                                //connIfx.Close();
                            }
                        }
                    //}
                    

                }

                connIfx.Close();
                        
            }
            catch (Exception exc)
            {
                log.Error(String.Format("Error ]inserting Informix database (reattempt as UPDATE): {0}", exc.Message));
                // Try UPDATE instead
                
               
            }
            return;
        }

        public string SendOrderKELA()
        {
            SUTI smsg = new SUTI();
            SUTIMsg msgSend = new SUTIMsg();

            orgType sender = new orgType();
            orgType receiver = new orgType();

            sender.name = "HTD";
            sender.idOrg.id = "902:HTD_KELA_SVC";
            sender.idOrg.src = "SUTI";
            sender.idOrg.unique = true;

            receiver.name = "Testiyhtio 1";
            receiver.idOrg.id = "901:Systemsupplier1_System_owner1_001";
            receiver.idOrg.src = "SUTI";
            receiver.idOrg.unique = true;

            smsg.orgSender = sender;
            smsg.orgReceiver = receiver;
            smsg.msg = new System.Collections.Generic.List<SUTIMsg>();

            idType id = new idType();
            id.src = "902:HTD_KELA_SVC";
            id.id = System.DateTime.Now.Ticks.ToString();
            id.unique = true;
            msgSend.msgName = "Keep alive";
            msgSend.msgType = "5000";
            msgSend.idMsg = id;
            smsg.msg.Add(msgSend);
            System.Diagnostics.Debug.WriteLine(smsg.Serialize().ToString());

            return smsg.Serialize();
        }

        public void ReplyOrderKELA(string sTpakID)
        {
            SUTI rmsg = new SUTI();
            SUTIMsg msgResponse = new SUTIMsg();
            SUTIMsg msgReceived = this.inSUTImsg;

            orgType sender = this.inSUTI.orgReceiver;
            orgType receiver = this.inSUTI.orgSender;

            rmsg.orgReceiver = receiver;
            rmsg.orgSender = sender;

            rmsg.msg = new List<SUTIMsg>();

            idType id = new idType();
            id.src = "104:HTD_001";
            id.id = System.DateTime.Now.Ticks.ToString();
            msgResponse.idMsg = id;
            msgResponse.msgName = "OrderConfirmation";
            msgResponse.msgType = "2001";
            msgResponse.referencesTo = new msgReferencesTo();
            msgResponse.referencesTo.idMsg = this.inSUTImsg.idMsg;
            List<idType> idList = this.inSUTImsg.referencesTo.idOrder;

            SUTI_svc.order theOrder = ((SUTI_svc.order)this.inSUTImsg.Item);
            
            idType tpakID = new idType();
            tpakID.src = "104:HTD_001:ROUTEID";
            tpakID.id = sTpakID;
            tpakID.unique = true;
            idList.Add(theOrder.idOrder);
            idList.Add(tpakID);

            msgResponse.referencesTo.idOrder = idList;

            rmsg.msg.Add(msgResponse);

            try
            {
                log.InfoFormat("HTD->HUT " + rmsg.Serialize().ToString());
                //WebRequest request = WebRequest.Create("http://10.100.113.33:8202/default.aspx");
                string response = "<SOAP-ENV:Envelope xmlns:SOAP-ENC='http://schemas.xmlsoap.org/soap/encoding/' xmlns:SOAP-ENV='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ZSI='http://www.zolera.com/schemas/ZSI/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'><SOAP-ENV:Header></SOAP-ENV:Header><SOAP-ENV:Body xmlns:ns1='http://tempuri.org/'><ns1:ReceiveSutiMsg><ns1:xmlstring>" +
                    System.Web.HttpUtility.HtmlEncode(rmsg.Serialize().ToString()) +
                    "</ns1:xmlstring></ns1:ReceiveSutiMsg></SOAP-ENV:Body></SOAP-ENV:Envelope>";

                byte[] buffer = Encoding.UTF8.GetBytes(response);

                WebRequest request = WebRequest.Create("http://192.168.222.11:7202/SUTI");
                request.Credentials = CredentialCache.DefaultCredentials;
                ((HttpWebRequest)request).UserAgent = "ASP.NET from HTD KELA SVC";
                ((HttpWebRequest)request).KeepAlive = false;
                ((HttpWebRequest)request).Timeout = System.Threading.Timeout.Infinite;
                ((HttpWebRequest)request).ReadWriteTimeout = System.Threading.Timeout.Infinite;
                ((HttpWebRequest)request).ProtocolVersion = HttpVersion.Version10;
                ((HttpWebRequest)request).AllowWriteStreamBuffering = false;
                ((HttpWebRequest)request).ContentLength = buffer.Length;

                request.Method = "POST";
                request.ContentType = "application/xml";
                Stream writer = request.GetRequestStream();

                log.InfoFormat("HTD->HUT " + response);
                writer.Write(buffer, 0, buffer.Length);
                writer.Close();

                // Response
                WebResponse resp = request.GetResponse();
                writer = resp.GetResponseStream();
                StreamReader rdr = new StreamReader(writer);
                log.InfoFormat("HUT->HTD " + rdr.ReadToEnd());
                rdr.Close();
                writer.Close();
                resp.Close();

            }
            catch (WebException exc)
            {
                log.InfoFormat("Error with ORDER CONFIRMATION - {0}", exc.Message);
            }
            catch (ProtocolViolationException exc)
            {
                log.InfoFormat("Error with ORDER CONFIRMATION - {0}" + exc.Message);
            }

            // Reject orders during TEST 
            //OrderKELAReject okr = new OrderKELAReject(this.inSUTI, this.inSUTImsg, sID, msgCount);
            //okr.SendOrderKELAReject(this.inSUTImsg);

            return;


        }
    }
}
