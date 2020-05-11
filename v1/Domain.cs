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
    public static class Domain
    {
        [FunctionName("DomainsGet")]
        public static async Task<IActionResult> DomainsGet (
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_api/v1/domains")] HttpRequest req,
            [Table(TableNames.Domains)] CloudTable domainTable,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            List<DomainEntity> entities = await DomainEntity.get(domainTable, null);
            if (entities == null) {
                return new NotFoundResult();
            }

            DomainEntity[] filteredEntities = entities.Where(domain => domain.Account == claimsPrincipal.Identity.Name).ToArray();
            if (filteredEntities.Count() == 0) {
                return new NotFoundResult();
            }

            return new OkObjectResult(filteredEntities);

        }


        [FunctionName("DomainGet")]
        public static async Task<IActionResult> DomainGet (
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_api/v1/domain/{key}")] HttpRequest req,
            [Table(TableNames.Domains)] CloudTable domainTable,
            string key,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            List<DomainEntity> entities = await DomainEntity.get(domainTable, key);
            if (entities == null) {
                return new NotFoundResult();
            }

            DomainEntity[] filteredEntities = entities.Where(domain => domain.Account == claimsPrincipal.Identity.Name).ToArray();
            if (filteredEntities.Length == 0) {
                return new NotFoundResult();
            }

            return new OkObjectResult(filteredEntities.First());

        }

        [FunctionName("DomainDelete")]
        public static async Task<IActionResult> DomainDelete (
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "_api/v1/domain/{key}")] HttpRequest req,
            [Table(TableNames.Domains)] CloudTable domainTable,
            string key,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            List<DomainEntity> entities = await DomainEntity.get(domainTable, key);
            if (entities == null) {
                return new NotFoundResult();
            }

            DomainEntity[] filteredEntities = entities.Where(domain => domain.Account == claimsPrincipal.Identity.Name).ToArray();
            if (filteredEntities.Length == 0) {
                return new NotFoundResult();
            }

            bool deleteSuccess = await DomainEntity.delete(domainTable, filteredEntities.First());
            return deleteSuccess ? (IActionResult)new OkObjectResult(filteredEntities.First()) : new BadRequestResult();


        }

        [FunctionName("DomainPost")]
        public static async Task<IActionResult> DomainPost (
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "_api/v1/domain")] HttpRequest req,
            [Table(TableNames.Domains)] CloudTable domainTable,
            ILogger log,
            ExecutionContext context,
            ClaimsPrincipal claimsPrincipal)
        {

            if (!claimsPrincipal.Identity.IsAuthenticated) {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            DomainEntity entity = JsonConvert.DeserializeObject<DomainEntity>(requestBody);

            if (entity.RowKey == null) {
                return new BadRequestObjectResult($"Please specify the RowKey in the request body");
            }

            log.LogInformation($"Getting Domain row for values {claimsPrincipal.Identity.Name} and {entity.RowKey}");
            List<DomainEntity> existingEntities = await DomainEntity.get(domainTable, entity.RowKey);
            if (existingEntities != null) {
                return new BadRequestObjectResult($"Domain with {entity.RowKey} already exists");
            }

            bool success = await DomainEntity.put(domainTable, entity.RowKey, claimsPrincipal.Identity.Name);
            if (!success) {
                return new BadRequestObjectResult($"Error occurred creating {entity.RowKey} already exists for {claimsPrincipal.Identity.Name}");
            }

            return new OkResult();
            
        }

        [FunctionName("DomainPatch")]
        public static async Task<IActionResult> DomainPatch (
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "_api/v1/domain/{key}")] HttpRequest req,
            [Table(TableNames.Domains)] CloudTable domainTable,
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

            log.LogInformation($"Getting Domain row for values {claimsPrincipal.Identity.Name} and {entity.RowKey}");
            List<DomainEntity> entities = await DomainEntity.get(domainTable, key);
            if (entities == null) {
                return new BadRequestObjectResult($"Domain with {key} doesn't exist for {claimsPrincipal.Identity.Name}");
            }

            DomainEntity[] filteredEntities = entities.Where(domain => domain.Account == claimsPrincipal.Identity.Name).ToArray();
            if (filteredEntities.Length == 0) {
                return new UnauthorizedResult();
            }

            filteredEntities[0].Configured = entity.configured ??= filteredEntities[0].Configured;
            filteredEntities[0].SslConfigured = entity.sslConfigured ??= filteredEntities[0].SslConfigured;

            bool success = await DomainEntity.put(domainTable, filteredEntities[0]);
            if (!success) {
                return new BadRequestObjectResult($"Error occurred updating domain {key} for {claimsPrincipal.Identity.Name}");
            }

            return new OkResult();
            
        }

    }

}