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
    /// Summary description for OrderKELAConfirm.
    /// </summary>
    public class PickupConfirm
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PickupConfirm));
        private string kela_id;
        private string tpak_id;
        private string veh_nbr;
        private string sID;
        private int msgCount;
        private SUTI smsg;

        public PickupConfirm(string _kela_id, string _veh_nbr, string _tpak_id, SUTI _smsg, int msgCounter)
        {
            kela_id = _kela_id.Trim();
            veh_nbr = _veh_nbr;
            tpak_id = _tpak_id;
            smsg = _smsg;
            msgCount = msgCounter;
        }

        public void ReplyPickupConfirm()
        {
            
            SUTI rmsg = new SUTI();

            orgType sender = new orgType();
            sender.name = ConfigurationManager.AppSettings.Get("localOrgName"); // "Helsingin Taksi-Data Oy";
            sender.idOrg.id = ConfigurationManager.AppSettings.Get("localOrgID"); // "104:TaxiPak_HTD_002";
            sender.idOrg.src = "SUTI";

            orgType receiver = new orgType();
            receiver.name = ConfigurationManager.AppSettings.Get("remoteOrgName"); // "Taksiliiton Yrityspalvelu Oy";
            receiver.idOrg.id = ConfigurationManager.AppSettings.Get("remoteOrgID"); // "801:Valopilkku_TYP_002";
            receiver.idOrg.src = "SUTI";

            rmsg.orgReceiver = receiver;
            rmsg.orgSender = sender;

            rmsg.msg = new List<SUTIMsg>();

            SUTIMsg msgResponse = new SUTIMsg();
            idType id = new idType();
            id.src = "104:TaxiPak_HTD_002:MSGID";
            id.id = System.DateTime.Now.Ticks.ToString();
            msgResponse.idMsg = id;

            msgResponse.msgName = "PickupConfirmation";
            msgResponse.msgType = "4010";
            msgResponse.referencesTo = new msgReferencesTo();
            //VPU ID
            idType idOrder = new idType();
            idOrder.src = "801:Valopilkku_TYP_002:MISSIONID";
            idOrder.id = this.kela_id;
            //TPAK ID
            idType idTpak = new idType();
            idTpak.src = "104:TaxiPak_HTD_002:BOOKID";
            idTpak.id = this.tpak_id;

            List<idType> idList = new List<idType>();
            idList.Add(idOrder);
            idList.Add(idTpak);

            //idList.Add(smsg.msg[0].idMsg);
            msgResponse.referencesTo.idOrder = idList;
            if (smsg != null)
                msgResponse.referencesTo.idMsg = smsg.msg[0].idMsg;
            else
            {
                idType idMsg = new idType();
                idMsg.src = "901:HUT:MSGID";
                idMsg.id = "12345";
                msgResponse.referencesTo.idMsg = idMsg;
            }

            idType idVehicle = new idType();
            idVehicle.id = this.veh_nbr;
            idVehicle.src = "104:HTD_001:VEHICLEID";
            idVehicle.unique = true;
            msgResponse.referencesTo.idVehicle = idVehicle;

            msgPickupConfirmation mpc = new msgPickupConfirmation();
            
            mpc.eventType = pickupConfirmationEventType.passengerinvehicle;
            node nc = new node();
            nc.nodeType = nodeNodeType.pickup;
            nc.nodeSeqno = "1";
            nodeAddressNode an = new nodeAddressNode();
            //nc.addressNode = an;
            List<timesTypeTime> lt = new List<timesTypeTime>();
            timesTypeTime tt = new timesTypeTime();
            tt.timeZone = "1"; tt.time1 = System.DateTime.Now; tt.timeType = timeTimeType.actual;
            lt.Add(tt);
            nc.timesNode = lt;
            mpc.nodeConfirmed = nc;

            msgResponse.Item = mpc;
            
            rmsg.msg.Add(msgResponse);

            try
            {
                log.InfoFormat("HTD->HUT " + rmsg.Serialize().ToString());
                //WebRequest request = WebRequest.Create("http://10.100.113.33:8202/default.aspx");
                string response = "<SOAP-ENV:Envelope xmlns:SOAP-ENC='http://schemas.xmlsoap.org/soap/encoding/' xmlns:SOAP-ENV='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ZSI='http://www.zolera.com/schemas/ZSI/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'><SOAP-ENV:Header></SOAP-ENV:Header><SOAP-ENV:Body xmlns:ns1='http://tempuri.org/'><ns1:ReceiveSutiMsg><ns1:xmlstring>" +
                    System.Web.HttpUtility.HtmlEncode(rmsg.Serialize().ToString()) +
                    "</ns1:xmlstring></ns1:ReceiveSutiMsg></SOAP-ENV:Body></SOAP-ENV:Envelope>";

                byte[] buffer = Encoding.UTF8.GetBytes(response);

                WebRequest request = WebRequest.Create(ConfigurationManager.AppSettings.Get("VPUendpoint")); //"http://10.190.90.1:7871/SutiService/");
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
                log.InfoFormat("Error with PICKUP CONFIRMATION - {0}", exc.Message);
            }
            catch (ProtocolViolationException exc)
            {
                log.InfoFormat("Error with PICKUP CONFIRMATION - {0}" + exc.Message);
            }
            catch (Exception exc)
            {
                log.InfoFormat("Error with PICKUP CONFIRMATION - {0}" + exc.Message);
            }

            // *** Test Phase *** 
            // ** Cancel Order and Notify *** //

            
            return;


        }
    }
}
