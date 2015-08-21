using System;
using System.Web;
using System.Web.SessionState;
using System.Net;
using System.IO;

namespace SUTI_svc
{
    /// <summary>
    /// Summary description for OrderKELA.
    /// </summary>
    public class OrderMonitor
    {
        public SUTI inSUTImsg;
        public enum CallStatus
        {
            PENDING = 1,
            UNASSIGNED,
            ASSIGNED,
            PICKUP,
            COMPLETE,
            CANCELED,
            NOEXIST
        }
        public CallStatus orderStatus;
        public string kela_id;
        public string tpak_id;
        public string veh_nbr;
        public double due_date_time;
        public bool bSentConfirm;
        public bool bSentAccept;

        public OrderMonitor(SUTI theMsg, string _tpak_id, string _kela_id)
        {
            inSUTImsg = theMsg;
            tpak_id = _tpak_id;
            kela_id = _kela_id;
            orderStatus = CallStatus.NOEXIST;
            veh_nbr = "";
            bSentAccept = false;
            bSentConfirm = false;
            due_date_time = 0;
        }

    }

}