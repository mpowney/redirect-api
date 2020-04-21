using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace api.v1
{
    public static class Url
    {
        [FunctionName("UrlPost")]
        public static async Task<IActionResult> UrlPost (
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "_api/v1/url")] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {

            return new OkObjectResult("Success");
            
        }
    }

}