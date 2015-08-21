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
    public class OrderKelaRejectConfirm
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(OrderKelaRejectConfirm));
        private string kela_id;
        private string veh_nbr;
        private string sID;
        private int msgCount;
        private SUTI smsg;

        public OrderKelaRejectConfirm(SUTI from, SUTIMsg msgFrom, string msgID, int msgCounter)
        {
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
 
	

    }
}
