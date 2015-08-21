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
    public class OrderKelaCancel
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(OrderKELA));
        private string sSrc;
        private string sID;
        private int msgCount;
        private SUTI inSUTI;
        private SUTIMsg inSUTImsg;
        private string _call_number;


        public string CallNbr
        {
            get { return _call_number; }
            set { _call_number = value; }
        }

        public OrderKelaCancel(SUTI from, SUTIMsg msgFrom, string msgID, int msgCounter)
        {
            inSUTI = from;
            inSUTImsg = msgFrom;
            sID = msgID;
            msgCount = msgCounter;

        }

        public void CancelConfirm(SUTIMsg msgFrom)
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
            msgResponse.msgName = "OrderCancellationAccepted";
            msgResponse.msgType = "2011";
            msgResponse.referencesTo = new msgReferencesTo();
            msgResponse.referencesTo.idMsg = this.inSUTImsg.idMsg;
            List<idType> idList = this.inSUTImsg.referencesTo.idOrder;

            SUTI_svc.order theOrder = ((SUTI_svc.order)this.inSUTImsg.Item);

            msgResponse.referencesTo.idOrder = idList;
            cancellationConsequence cc = new cancellationConsequence();
            cc.cancellationAcceptance = true;
            cc.cancellationConsequence1 = true;

            msgResponse.Item = cc;
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

        public string QuickReply()
        {
            String response = "<SOAP-ENV:Envelope xmlns:SOAP-ENC='http://schemas.xmlsoap.org/soap/encoding/' xmlns:SOAP-ENV='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ZSI='http://www.zolera.com/schemas/ZSI/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'><SOAP-ENV:Header></SOAP-ENV:Header><SOAP-ENV:Body xmlns:ns1='http://tempuri.org/'><ns1:ReceiveSutiMsgResponse><ns1:ReceiveSutiMsgResult>" +
                                "1</ns1:ReceiveSutiMsgResult></ns1:ReceiveSutiMsgResponse></SOAP-ENV:Body></SOAP-ENV:Envelope>";

            log.InfoFormat("HTD->HUT " + response);

            return response;
        }
    }
}
