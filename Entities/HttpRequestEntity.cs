using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace api.entities {

    public class HttpRequestEntity {
        public HttpRequestEntity() { }
        public HttpRequestEntity(HttpRequest req) { 
            this.ContentLength = req.ContentLength;
            this.ContentType = req.ContentType;
            this.Headers = (IDictionary<string, StringValues>)req.Headers;
            this.Host = req.Host.Value;
            this.IsHttps = req.IsHttps;
            this.Method = req.Method;
            this.Path = req.Path;
            this.PathBase = req.PathBase;
            this.QueryString = req.QueryString;
            this.RemoteIpAddress = req.HttpContext.Connection.RemoteIpAddress.ToString();
            this.Scheme = req.Scheme;
            this.Referer = req.Headers["Referer"].ToString();
            this.UserAgent = req.Headers["User-Agent"].ToString();
        }

        public long? ContentLength { get; set; }
        public string ContentType { get; set; }
        public IDictionary<string, StringValues> Headers { get; set; }
        public string Host { get; set; }
        public bool IsHttps { get; set; }
        public string Method { get; set; }
        public PathString Path { get; set; }
        public PathString PathBase { get; set; }
        public QueryString QueryString { get; set; }
        public string RemoteIpAddress {get; set; }
        public string Scheme { get; set; }
        public string Referer { get; set; }
        public string UserAgent { get; set; }
    }
    
}