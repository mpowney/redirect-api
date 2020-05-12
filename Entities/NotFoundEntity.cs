using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace api.entities {

    public class NotFoundEntity: HttpRequestEntity {
        public NotFoundEntity() { }
        public NotFoundEntity(HttpRequest req, string reason) { 
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
            this.Reason = reason;

        }

        public string Reason { get; set; }

    }

}
