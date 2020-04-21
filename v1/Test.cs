using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace api.v1
{
    public static class Test
    {
        [FunctionName("Test")]
        public static async Task<IActionResult> TestPost (
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "_api/v1/test")] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {

            return new OkObjectResult("Success");
            
        }
        [FunctionName("TestWithAuthPost")]
        public static async Task<IActionResult> TestWithAuthPost (
            [HttpTrigger(AuthorizationLevel.User, "post", Route = "_api/v1/test-with-auth")] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {

            return new OkObjectResult("Success");
            
        }
    }

}