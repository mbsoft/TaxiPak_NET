﻿using System;
using System.IO;
using System.Web;
using System.Web.SessionState;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using log4net;
using log4net.Config;

namespace SUTI_svc
{
    /// <summary>
    /// Summary description for MsgToVehicle.
    /// </summary>
    public class MsgToVehicle
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MsgToVehicle));
        private string sSrc;
        private string sID;
        private int msgCount;
        private SUTI inSUTI;
        private SUTIMsg inSUTImsg;



        public MsgToVehicle(SUTI from, SUTIMsg msgFrom, string msgID, int msgCounter)
        {
            inSUTI = from;
            inSUTImsg = msgFrom;
            sID = msgID;
            msgCount = msgCounter;
        }

        public string SendMsgToVehicle()
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

        public string QuickReply()
        {
            String response = "<SOAP-ENV:Envelope xmlns:SOAP-ENC='http://schemas.xmlsoap.org/soap/encoding/' xmlns:SOAP-ENV='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ZSI='http://www.zolera.com/schemas/ZSI/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'><SOAP-ENV:Header></SOAP-ENV:Header><SOAP-ENV:Body xmlns:ns1='http://tempuri.org/'><ns1:ReceiveSutiMsgResponse><ns1:ReceiveSutiMsgResult>" +
                                "1</ns1:ReceiveSutiMsgResult></ns1:ReceiveSutiMsgResponse></SOAP-ENV:Body></SOAP-ENV:Envelope>";

            log.InfoFormat("HTD->HUT " + response);

            return response;
        }

        public void ReplyMsgToVehicle()
        {
            SUTI rmsg = new SUTI();
            SUTIMsg msgResponse = new SUTIMsg();
            SUTIMsg msgReceived = this.inSUTImsg;

            orgType sender = this.inSUTI.orgReceiver;
            orgType receiver = this.inSUTI.orgSender;

            rmsg.orgReceiver = receiver;
            rmsg.orgSender = sender;

            rmsg.msg = new System.Collections.Generic.List<SUTIMsg>();

            idType id = new idType();
            id.src = "902:HTD_KELA_SVC";
            id.id = System.DateTime.Now.Ticks.ToString();
            msgResponse.idMsg = id;
            msgResponse.msgName = "Confirmation Message to Vehicle";
            msgResponse.msgType = "5001";
            msgResponse.referencesTo = new msgReferencesTo();
            msgResponse.referencesTo.idMsg = this.inSUTImsg.idMsg;

            rmsg.msg.Add(msgResponse);
            System.Diagnostics.Debug.WriteLine(rmsg.Serialize().ToString());

            try
            {
                //WebRequest request = WebRequest.Create("http://10.100.113.33:8202/default.aspx");
                String response = "<SOAP-ENV:Envelope xmlns:SOAP-ENC='http://schemas.xmlsoap.org/soap/encoding/' xmlns:SOAP-ENV='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ZSI='http://www.zolera.com/schemas/ZSI/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'><SOAP-ENV:Header></SOAP-ENV:Header><SOAP-ENV:Body xmlns:ns1='http://tempuri.org/'><ns1:ReceiveSutiMsg><ns1:xmlstring>" +
                    System.Web.HttpUtility.HtmlEncode(rmsg.Serialize().ToString()) +
                    "</ns1:xmlstring></ns1:ReceiveSutiMsg></SOAP-ENV:Body></SOAP-ENV:Envelope>";

                byte[] buffer = Encoding.UTF8.GetBytes(response);

                WebRequest request = WebRequest.Create("http://10.190.90.1:7871/SutiService/");
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
                log.InfoFormat("Error with MESSAGE_TO_VEHICLE - {0}", exc.Message);
            }
            catch (ProtocolViolationException exc)
            {
                log.InfoFormat("Error with MESSAGE_TO_VEHICLE - {0}" + exc.Message);
            }
            
            return;

        }
    }
}
