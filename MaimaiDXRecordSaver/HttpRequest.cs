using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MaimaiDXRecordSaver
{
    public class HttpRequest
    {
        public const int MaxUrlLength = 512;
        public const int MaxHeaderLineLength = 512;
        public const int MaxContentLength = 16384;

        public string Method { get; private set; }
        public string URL { get; private set; }
        public string HttpVersion { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public int ContentLength { get; private set; }
        public byte[] ContentBytes { get; private set; }

        public HttpRequest()
        {
            Headers = new Dictionary<string, string>();
        }

        public bool ParseRequestFromStream(Stream conncetionStream)
        {
            byte[] buf = new byte[2048];
            int bufBytesCount = conncetionStream.Read(buf, 0, 2048);
            int headersOffset = ReadFirstLine(buf, 0);
            if (headersOffset == -1) return false;
            int contentOffset = ReadHeaders(buf, headersOffset);
            if (contentOffset == -1) return false;
            if(Headers.ContainsKey("Content-Length") && int.TryParse(Headers["Content-Length"], out int cl))
            {
                if (cl < 0 || cl > MaxContentLength) return false;
                ContentBytes = new byte[cl];
                int bytesInBuf = bufBytesCount - contentOffset;
                int bytesRead = bytesInBuf;
                for(int i = 0; i < bytesInBuf; i++ )
                {
                    ContentBytes[i] = buf[contentOffset + i];
                }
                if(bytesInBuf < cl)
                {
                    bytesRead += conncetionStream.Read(ContentBytes, bytesInBuf, cl - bytesInBuf);
                }
                if (bytesRead != cl) return false;
            }
            return true;
        }

        private int ReadFirstLine(byte[] bytes, int offset)
        {
            int newOffset = ReadSpaceEndedString(bytes, offset, 10, out string method);
            if (newOffset == -1) return -1;
            Method = method;
            newOffset = ReadSpaceEndedString(bytes, newOffset, MaxUrlLength, out string url);
            if (newOffset == -1) return -1;
            URL = url;
            newOffset = ReadCRLFEndedString(bytes, newOffset, 10, out string httpVer);
            if (newOffset == -1) return -1;
            HttpVersion = httpVer;
            return newOffset;
        }

        private int ReadHeaders(byte[] bytes, int offset)
        {
            int newOffset = offset;
            while((newOffset = ReadCRLFEndedString(bytes, newOffset, MaxHeaderLineLength, out string headerLine)) >= 0)
            {
                if (string.IsNullOrEmpty(headerLine))
                {
                    return newOffset;
                }
                else
                {
                    int index = headerLine.IndexOf(':');
                    if (index < 0) return -1;
                    string name = headerLine.Substring(0, index).Trim();
                    string value = headerLine.Substring(index + 1, headerLine.Length - index - 1).Trim();
                    if (Headers.ContainsKey(name)) return -1;
                    Headers.Add(name, value);
                }
            }
            return -1;
        }

        /// <summary>
        /// Read a space-ended string from a byte array
        /// </summary>
        /// <param name="bytes">Byte array</param>
        /// <param name="offset">Offset</param>
        /// <param name="maxLength">Max length of the string</param>
        /// <param name="result">Output string, returns null failed to read</param>
        /// <returns>New offset, returns -1 if failed to read</returns>
        private int ReadSpaceEndedString(byte[] bytes, int offset, int maxLength, out string result)
        {
            int max = Math.Min(offset + maxLength, bytes.Length);
            for(int i = offset; i < max; i++ )
            {
                if(bytes[i] == 0x20)
                {
                    result = Encoding.ASCII.GetString(bytes, offset, i - offset);
                    return i + 1;
                }
            }
            result = null;
            return -1;
        }

        /// <summary>
        /// Read a CRLF-ended string from a byte array
        /// </summary>
        /// <param name="bytes">Byte array</param>
        /// <param name="offset">Offset</param>
        /// <param name="maxLength">Max length of the string</param>
        /// <param name="result">Output string, returns null failed to read</param>
        /// <returns>New offset, returns -1 if failed to read</returns>
        private int ReadCRLFEndedString(byte[] bytes, int offset, int maxLength, out string result)
        {
            int max = Math.Min(offset + maxLength, bytes.Length);
            for (int i = offset; i < max; i++)
            {
                if (bytes[i] == 0x0A && i > offset && bytes[i - 1] == 0x0D)
                {
                    result = Encoding.ASCII.GetString(bytes, offset, i - offset - 1);
                    return i + 1;
                }
            }
            result = null;
            return -1;
        }
    }
}
