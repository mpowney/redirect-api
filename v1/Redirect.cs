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
using Microsoft.Extensions.Configuration;
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

            List<RedirectEntity> entities = await RedirectEntity.get(redirectTable, claimsPrincipal.Identity.Name);
            if (entities == null) {
                return new NotFoundResult();
            }

            RedirectEntity[] filteredEntities = entities.Where(redirect => redirect.Recycled == false).ToArray();
            if (filteredEntities == null || filteredEntities.Length == 0) {
                return new OkObjectResult(new RedirectEntity[] {});
            }

            return new OkObjectResult(filteredEntities);

        }

        [FunctionName("RecycledRedirectsGet")]
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

            List<RedirectEntity> entities = (await RedirectEntity.get(redirectTable, claimsPrincipal.Identity.Name));
            if (entities == null) {
                return new NotFoundResult();
            }


            RedirectEntity[] filteredEntities = entities.Where(redirect => redirect.Recycled == true).ToArray();
            if (filteredEntities == null || filteredEntities.Length == 0) {
                return new OkObjectResult(new RedirectEntity[] {});
            }

            return new OkObjectResult(filteredEntities);

        }

        [FunctionName("RedirectsDashboardGet")]
        public static async Task<IActionResult> RedirectsDashboardGet (
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_api/v1/redirects/dashboard")] HttpRequest req,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            List<RedirectEntity> entities = (await RedirectEntity.get(redirectTable, claimsPrincipal.Identity.Name));
            if (entities == null) {
                return new NotFoundResult();
            }

            RedirectEntity[] filteredEntities = entities.Where(redirect => redirect.Recycled == true).ToArray();
            if (filteredEntities == null || filteredEntities.Length == 0) {
                return new OkObjectResult(new RedirectEntity[] {});
            }

            return new OkObjectResult(filteredEntities);

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

        [FunctionName("RedirectGetGeoCounts")]
        public static async Task<IActionResult> RedirectGetGeoCounts (
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_api/v1/redirect/{key}/geo")] HttpRequest req,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
            [Table(TableNames.Geos)] CloudTable geoTable,
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

            return new OkObjectResult(await entity.GetGeoCount(geoTable));

        }

        [FunctionName("RedirectAndHostGet")]
        public static async Task<IActionResult> RedirectAndHostGet (
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_api/v1/host/redirect/{host}/{key}")] HttpRequest req,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
            [Table(TableNames.Domains)] CloudTable domainTable,
            string host,
            string key,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            List<DomainEntity> domainEntity = await DomainEntity.get(domainTable, host);
            if (domainEntity == null) {
                return new NotFoundResult();
            }

            RedirectEntity entity = await RedirectEntity.get(redirectTable, domainEntity.First().Account, key);
            if (entity == null) {
                return new NotFoundResult();
            }

            if (entity.Recycled) {
                return new NotFoundResult();
            }

            return new OkObjectResult(new RedirectEntity(entity.PartitionKey, entity.RowKey, entity.RedirectTo, 0, new Dictionary<string, int>(), DateTime.Now, false));

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

            if (entity.Recycled) {
                bool deleteSuccess = await RedirectEntity.delete(redirectTable, entity);
                return deleteSuccess ? (IActionResult)new OkObjectResult(entity) : new BadRequestResult();
            }

            entity.Recycled = true;
            bool success = await RedirectEntity.put(redirectTable, entity);
            return success ? (IActionResult)new OkObjectResult(entity) : new BadRequestResult();

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
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "_api/v1/redirect/{key}")] HttpRequest req,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
            string key,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic entity = JsonConvert.DeserializeObject<dynamic>(requestBody);

            log.LogInformation($"Getting Redirect row for values {claimsPrincipal.Identity.Name} and {entity.RowKey}");
            RedirectEntity existingEntity = await RedirectEntity.get(redirectTable, claimsPrincipal.Identity.Name, key);
            if (existingEntity == null) {
                return new BadRequestObjectResult($"Redirect with {key} doesn't exist for {claimsPrincipal.Identity.Name}");
            }

            existingEntity.RedirectTo = entity.redirectTo ??= existingEntity.RedirectTo;
            existingEntity.Recycled = entity.recycled ??= existingEntity.Recycled;

            bool success = await RedirectEntity.put(redirectTable, existingEntity);
            if (!success) {
                return new BadRequestObjectResult($"Error occurred updating {key} for {claimsPrincipal.Identity.Name}");
            }

            return new OkResult();
            
        }

    }

}