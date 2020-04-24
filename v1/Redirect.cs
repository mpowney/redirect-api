using System.IO;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using api.entities;

namespace api.v1
{
    public static class Redirect
    {
        [FunctionName("RedirectsGet")]
        public static async Task<IActionResult> RedirectsGet (
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_api/v1/redirects")] HttpRequest req,
            [Table(TableNames.Refirects)] CloudTable redirectTable,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            RedirectEntity[] entities = await RedirectEntity.get(redirectTable, claimsPrincipal.Identity.Name);
            if (entities == null) {
                return new OkObjectResult(new RedirectEntity[] {});
            }

            return new OkObjectResult(entities);

        }

        [FunctionName("RedirectGet")]
        public static async Task<IActionResult> RedirectGet (
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_api/v1/redirect/{key}")] HttpRequest req,
            [Table(TableNames.Refirects)] CloudTable redirectTable,
            string key,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            RedirectEntity entity = await RedirectEntity.get(redirectTable, claimsPrincipal.Identity.Name, key);
            if (entity == null) {
                return new NotFoundResult();
            }

            return new OkObjectResult(entity);

        }

        [FunctionName("RedirectPost")]
        public static async Task<IActionResult> RedirectPost (
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "_api/v1/redirect")] HttpRequest req,
            [Table(TableNames.Refirects)] CloudTable redirectTable,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            if (data?.rowKey == null || data?.redirectTo == null) {
                return new BadRequestObjectResult($"Please specify the key and redirectTo parameters in the request body");
            }

            RedirectEntity existingEntity = await RedirectEntity.get(redirectTable, claimsPrincipal.Identity.Name, data.key);
            if (existingEntity != null) {
                return new BadRequestObjectResult($"Redirect with ${data.key} already exists for ${claimsPrincipal.Identity.Name}");
            }

            bool success = await RedirectEntity.put(redirectTable, claimsPrincipal.Identity.Name, data.rowKey, data.redirectTo, 0, null);
            if (!success) {
                return new BadRequestObjectResult($"Error occurred creating ${data.key} already exists for ${claimsPrincipal.Identity.Name}");
            }

            return new OkResult();
            
        }
    }

}