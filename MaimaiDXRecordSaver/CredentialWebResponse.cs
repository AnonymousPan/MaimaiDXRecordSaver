using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaimaiDXRecordSaver
{
    public class CredentialWebResponse
    {
        public int StatusCode { get; private set; }
        public byte[] ContentBytes { get; private set; }
        public string ContentType { get; private set; }

        public string Location { get; set; }

        public bool CredentialChanged { get; private set; } = false;
        public string NewUserID { get; private set; }
        public string NewTValue { get; private set; }
        public string NewFriendCodeList { get; private set; }

        public bool Failed { get; private set; } = false;
        public Exception Exception { get; private set; }

        /// <summary>
        /// Create an response object with exception info, called when request failed.
        /// </summary>
        /// <param name="err">Exception thrown while doing request</param>
        public CredentialWebResponse(Exception err)
        {
            Failed = true;
            Exception = err;
        }

        /// <summary>
        /// Create an response object with content, called when request completed.
        /// </summary>
        /// <param name="contentBytes">Content bytes</param>
        /// <param name="contentType">Content-Type string</param>
        public CredentialWebResponse(int statusCode, byte[] contentBytes, string contentType)
        {
            Failed = false;
            StatusCode = statusCode;
            ContentBytes = contentBytes;
            ContentType = contentType;
        }

        /// <summary>
        /// Set the new credential info of the response object if credential is changed by the request
        /// </summary>
        /// <param name="userId">New userId value</param>
        /// <param name="tValue">New _t value</param>
        /// <param name="friendCodeList">New friendCodeList</param>
        public void SetCredentialInfo(string userId, string tValue, string friendCodeList)
        {
            CredentialChanged = true;
            NewUserID = userId;
            NewTValue = tValue;
            NewFriendCodeList = friendCodeList;
        }

        /// <summary>
        /// Decode the content bytes as UTF-8 string.
        /// </summary>
        /// <returns>String result</returns>
        public string GetContentAsUTF8String()
        {
            if(!Failed && ContentBytes != null)
            {
                return Encoding.UTF8.GetString(ContentBytes);
            }
            else
            {
                return null;
            }
        }
    }
}
