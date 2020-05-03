using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
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
            [Table(TableNames.Redirects)] CloudTable redirectTable,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            RedirectEntity[] entities = await RedirectEntity.get(redirectTable, claimsPrincipal.Identity.Name);
            entities = (RedirectEntity[])entities.Where(redirect => redirect.Recycled == false);
            if (entities == null) {
                return new OkObjectResult(new RedirectEntity[] {});
            }

            return new OkObjectResult(entities);

        }

        public static async Task<IActionResult> RecycledRedirectsGet (
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_api/v1/redirects/recycled")] HttpRequest req,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            RedirectEntity[] entities = (await RedirectEntity.get(redirectTable, claimsPrincipal.Identity.Name));
            entities = (RedirectEntity[])entities.Where(redirect => redirect.Recycled == true);
            if (entities == null) {
                return new OkObjectResult(new RedirectEntity[] {});
            }

            return new OkObjectResult(entities);

        }

        [FunctionName("RedirectGet")]
        public static async Task<IActionResult> RedirectGet (
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_api/v1/redirect/{key}")] HttpRequest req,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
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

        [FunctionName("RedirectDelete")]
        public static async Task<IActionResult> RedirectDelete (
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "_api/v1/redirect/{key}")] HttpRequest req,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
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

            entity.Recycled = true;
            await RedirectEntity.put(redirectTable, entity);

            return new OkObjectResult(entity);

        }

        [FunctionName("RedirectPost")]
        public static async Task<IActionResult> RedirectPost (
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "_api/v1/redirect")] HttpRequest req,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            RedirectEntity entity = JsonConvert.DeserializeObject<RedirectEntity>(requestBody);

            if (entity.RowKey == null || entity.RedirectTo == null) {
                return new BadRequestObjectResult($"Please specify the key and redirectTo parameters in the request body");
            }

            log.LogInformation($"Getting Redirect row for values {claimsPrincipal.Identity.Name} and {entity.RowKey}");
            RedirectEntity existingEntity = await RedirectEntity.get(redirectTable, claimsPrincipal.Identity.Name, entity.RowKey);
            if (existingEntity != null) {
                return new BadRequestObjectResult($"Redirect with {entity.RowKey} already exists for {claimsPrincipal.Identity.Name}");
            }

            bool success = await RedirectEntity.put(redirectTable, claimsPrincipal.Identity.Name, entity.RowKey, entity.RedirectTo, 0, new Dictionary<string, int>(), DateTime.Now, false);
            if (!success) {
                return new BadRequestObjectResult($"Error occurred creating {entity.RowKey} already exists for {claimsPrincipal.Identity.Name}");
            }

            return new OkResult();
            
        }

        [FunctionName("RedirectPatch")]
        public static async Task<IActionResult> RedirectPatch (
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "_api/v1/redirect")] HttpRequest req,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            RedirectEntity entity = JsonConvert.DeserializeObject<RedirectEntity>(requestBody);

            if (entity.RowKey == null || entity.RedirectTo == null) {
                return new BadRequestObjectResult($"Please specify the key and redirectTo parameters in the request body");
            }

            log.LogInformation($"Getting Redirect row for values {claimsPrincipal.Identity.Name} and {entity.RowKey}");
            RedirectEntity existingEntity = await RedirectEntity.get(redirectTable, claimsPrincipal.Identity.Name, entity.RowKey);
            if (existingEntity == null) {
                return new BadRequestObjectResult($"Redirect with {entity.RowKey} doesn't exist for {claimsPrincipal.Identity.Name}");
            }
            existingEntity.RedirectTo = entity.RedirectTo;

            bool success = await RedirectEntity.put(redirectTable, existingEntity);
            if (!success) {
                return new BadRequestObjectResult($"Error occurred updating {entity.RowKey} for {claimsPrincipal.Identity.Name}");
            }

            return new OkResult();
            
        }

    }

}