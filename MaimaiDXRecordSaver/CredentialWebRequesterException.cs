using System;

namespace MaimaiDXRecordSaver
{
    public class CredentialWebRequesterException : Exception
    {
        public CredentialWebRequesterException() : base() { }
        public CredentialWebRequesterException(string message) : base(message) { }
        public CredentialWebRequesterException(string message, Exception innerException) : base(message, innerException) { }
    }
}
