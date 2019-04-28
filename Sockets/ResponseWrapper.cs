using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Sockets
{
    public class ResponseWrapper
    {
        public string Content { get; private set; }
        public string ErrorMessage { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }
        public byte[] ByteContent { get; private set; }

        public ResponseWrapper(string content, HttpStatusCode statusCode, byte[] byteContent)
        {
            StatusCode = statusCode;
            if (IsSuccessStatusCode())
            {
                ByteContent = byteContent;
                Content = content;
                return;
            }

            ErrorMessage = content;
        }

        public bool IsSuccessStatusCode()
        {
            return (int)StatusCode >= 200 && (int)StatusCode <= 299;
        }
    }
}
