using System.Net;

namespace JFrog.Artifactory.Utils.httpClient
{
    /// <summary>
    /// Custom Http response, for upper layers use
    /// <summary>
    class HttpResponse
    {
        public HttpStatusCode _statusCode { set; get; }
  
        public string _message { get; set; }

        public HttpResponse(HttpStatusCode statusCode, string message) 
        {
            _statusCode = statusCode;
            _message = message;
        }
    }
}
