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
    public class OrderKELAReject
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(OrderKELAReject));
        private string kela_id;
        private string tpak_id;
        private SUTI smsg;
        private string sID;
        private int msgCount;



        public OrderKELAReject(string _kela_id, string _tpak_id, SUTI _smsg, int msgCounter)
        {
            kela_id = _kela_id;
            tpak_id = _tpak_id;
            smsg = _smsg;
            msgCount = msgCounter;
        }

        public void ReplyOrderCancel()
        {

            SUTI rmsg = new SUTI();


            orgType sender = new orgType();
            sender.name = "HTD";
            sender.idOrg.id = "104:HTD_001";
            sender.idOrg.src = "SUTI";

            orgType receiver = new orgType();
            receiver.name = "HUT";
            receiver.idOrg.id = "801:HUT_001";
            receiver.idOrg.src = "SUTI";

            rmsg.orgReceiver = receiver;
            rmsg.orgSender = sender;

            rmsg.msg = new List<SUTIMsg>();

            SUTIMsg msgResponse = new SUTIMsg();
            idType id = new idType();
            id.src = "104:HTD_001:MSGID";
            id.id = System.DateTime.Now.Ticks.ToString();
            msgResponse.idMsg = id;
            msgResponse.msgName = "OrderReject";
            msgResponse.msgType = "2002";
            msgResponse.referencesTo = new msgReferencesTo();
            idType idOrder = new idType();
            idOrder.src = "KELA_ROUTEID";
            idOrder.id = this.kela_id;
            List<idType> idList = new List<idType>();
            idList.Add(idOrder);
            
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

            msgResponse.referencesTo.idOrder = idList;

            msgOrderReject or = new msgOrderReject();
            //orderReject or = new orderReject();
            or.resourceReject = new resourceType();
            
            manualDescriptionType md = new manualDescriptionType();
            md.manualText = "";
            md.sendtoInvoice = false;
            md.sendtoVehicle = false;
            md.sendtoOperator = false;
            md.vehicleConfirmation = false;

            or.resourceReject.manualDescriptionResource.Add(md);

            if (smsg != null)
            {
                orderRejectOrderSentBefore osb = new orderRejectOrderSentBefore();
                osb.idMsg = this.smsg.msg[0].idMsg;

                or.orderSentBefore = osb;
            }
            else
            {
                orderRejectOrderSentBefore osb = new orderRejectOrderSentBefore();
                idType idMsg = new idType();
                idMsg.src = "901:HUT:MSGID";
                idMsg.id = "12345";
                osb.idMsg = idMsg;
                or.orderSentBefore = osb;
            }
            
            msgResponse.Item = or;

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
                log.InfoFormat("Error with KEEP ALIVE - {0}", exc.Message);
            }
            catch (ProtocolViolationException exc)
            {
                log.InfoFormat("Error with KEEP ALIVE - {0}" + exc.Message);
            }

            // *** Test Phase *** 
            // ** Cancel Order and Notify *** //


            return;


        }
    

    }

}
