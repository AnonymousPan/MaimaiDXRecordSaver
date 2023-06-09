using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaimaiDXRecordSaver
{
    public class CredentialInvalidException : Exception
    {
        public string SessionID { get; set; }
        public string TValue { get; set; }
        public string FriendCodeList { get; set; }
        public string RequestedURL { get; set; }

        public CredentialInvalidException() { }

        public CredentialInvalidException(string url)
        {
            RequestedURL = url;
        }

        public CredentialInvalidException(string sessionID, string _t, string friendCodeList)
        {
            SessionID = sessionID;
            TValue = _t;
            FriendCodeList = friendCodeList;
        }

        public CredentialInvalidException(string url, string sessionID, string _t, string friendCodeList)
        {
            RequestedURL = url;
            SessionID = sessionID;
            TValue = _t;
            FriendCodeList = friendCodeList;
        }
    }
}
