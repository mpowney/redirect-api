using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace api.v1
{
    public static class Test
    {
        [FunctionName("TestGet")]
        public static async Task<IActionResult> TestGet (
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_api/v1/test")] HttpRequest req,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            return new OkObjectResult($"Success\n\n {claimsPrincipal.Claims.Select(claim => { return $"{claim.Subject} = {claim.Value}\n"; })}");
            
        }

        [FunctionName("TestPost")]
        public static async Task<IActionResult> TestPost (
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "_api/v1/test")] HttpRequest req,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            return new OkObjectResult($"Success\n\n {claimsPrincipal.Claims.Select(claim => { return $"{claim.Subject} = {claim.Value}\n"; })}");
            
        }

        [FunctionName("TestWithAuthGet")]
        public static async Task<IActionResult> TestWithAuthGet (
            [HttpTrigger(AuthorizationLevel.User, "get", Route = "_api/v1/test-with-auth")] HttpRequest req,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
            
        {

            return new OkObjectResult($"Success\n\n {claimsPrincipal.Claims.Select(claim => { return $"{claim.Subject} = {claim.Value}\n"; })}");
            
        }

        [FunctionName("TestWithAuthPost")]
        public static async Task<IActionResult> TestWithAuthPost (
            [HttpTrigger(AuthorizationLevel.User, "post", Route = "_api/v1/test-with-auth")] HttpRequest req,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            return new OkObjectResult($"Success\n\n {claimsPrincipal.Claims.Select(claim => { return $"{claim.Subject} = {claim.Value}\n"; })}");
            
        }


    }

}