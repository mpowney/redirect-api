using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text.RegularExpressions;
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
    public static class Clicks
    {

        [FunctionName("ShortNameGet")]
        public static async Task<IActionResult> ShortNameGet (
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{shortName}")] HttpRequest req,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
            [Table(TableNames.Domains)] CloudTable domainTable,
            [Queue(QueueNames.ProcessClicks), StorageAccount("AzureWebJobsStorage")] ICollector<HttpRequestEntity> processClicksQueue,
            string shortName,
            ILogger log,
            ExecutionContext context)
        {

            DomainEntity domain = await DomainEntity.get(domainTable, req.Host.Value);
            if (domain == null) {
                return new NotFoundResult();
            }

            RedirectEntity redirect = await RedirectEntity.get(redirectTable, domain.Account, shortName);

            if (redirect != null) {

                if (redirect.Recycled) {
                    return new NotFoundResult();
                }

                req.HttpContext.Response.Headers.Add("Cache-Control", "private,max-age=300");
                req.HttpContext.Response.Headers.Add("Expires", DateTime.Now.AddMinutes(5).ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss G\\MT"));
                req.HttpContext.Response.Headers.Add("Vary", "Origin");

                processClicksQueue.Add(new HttpRequestEntity(req));
                return new RedirectResult(redirect.RedirectTo, true);

            }

            return new NotFoundResult();

        }


        [FunctionName("ProcessClicks")]
        public static async void ProcessClicks(
            [QueueTrigger(QueueNames.ProcessClicks)] string queuedHttpRequestString,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
            [Table(TableNames.Domains)] CloudTable domainTable,
            [Queue(QueueNames.ProcessClicksGeo), StorageAccount("AzureWebJobsStorage")] ICollector<HttpRequestEntity> processClicksGeoQueue,
            ILogger log,
            ExecutionContext context)
        {

            HttpRequestEntity queuedHttpRequest = JsonConvert.DeserializeObject<HttpRequestEntity>(queuedHttpRequestString);

            DomainEntity domain = await DomainEntity.get(domainTable, queuedHttpRequest.Host);

            if (domain == null) {
                throw new Exception($"Unable to process Geo lookup - domain {queuedHttpRequest.Host} wasn't found");
            }

            string path = queuedHttpRequest.Path.Value.Substring(1);

            RedirectEntity redirect = await RedirectEntity.get(redirectTable, domain.Account, path);

            if (redirect != null) {

                redirect.ClickCount++;
                await RedirectEntity.put(redirectTable, redirect);
                processClicksGeoQueue.Add(queuedHttpRequest);
    
                log.LogInformation($"Successfully processed click for redirect query {queuedHttpRequest.Path} from {queuedHttpRequest.RemoteIpAddress}");

                return;

            }

            log.LogError($"Http request {queuedHttpRequest.Path} for click handling doesn't match handled hosts");
            throw new System.Exception($"Http request {queuedHttpRequest.Path} for click handling doesn't match handled hosts");

        }

        [FunctionName("ProcessClicksGeo")]
        public static async void ProcessClicksForGeo(
            [QueueTrigger(QueueNames.ProcessClicksGeo)] string queuedHttpRequestString,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
            [Table(TableNames.Domains)] CloudTable domainTable,
            [Table(TableNames.Geos)] CloudTable geoTable,
            ILogger log,
            ExecutionContext context)
        {

            HttpRequestEntity queuedHttpRequest = JsonConvert.DeserializeObject<HttpRequestEntity>(queuedHttpRequestString);

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            string ipLookupUrl = $"{config["FREEGEOIP_HOST"] ??= "https://freegeoip.app"}/json/{queuedHttpRequest.RemoteIpAddress}";

            log.LogInformation($"Looking up freegeoip: {ipLookupUrl}.");


            var client = new HttpClient();
            var getResponse = await client.GetAsync(ipLookupUrl);
            if (getResponse.StatusCode == HttpStatusCode.OK) {

                DomainEntity domain = await DomainEntity.get(domainTable, queuedHttpRequest.Host);

                if (domain == null) {
                    throw new Exception($"Unable to process Geo lookup - domain {queuedHttpRequest.Host} wasn't found");
                }

                string path = queuedHttpRequest.Path.Value.Substring(1);

                RedirectEntity redirect = await RedirectEntity.get(redirectTable, domain.Account, path);

                if (redirect != null) {

                    string ipResponseString = await getResponse.Content.ReadAsStringAsync();
                    dynamic ipResponse = JsonConvert.DeserializeObject<dynamic>(ipResponseString);
                    GeoEntity geoEntity = new GeoEntity(ipResponse);
                    await GeoEntity.put(geoTable, geoEntity);

                    Dictionary<string, int> _geoCount = JsonConvert.DeserializeObject<Dictionary<string, int>>(redirect.GeoCount ??= "{}");
                    if (_geoCount.ContainsKey(geoEntity.RowKey)) {
                        log.LogInformation($"Incrementing GeoCount for redirect entity {queuedHttpRequest.Path}");
                        
                        _geoCount[geoEntity.RowKey] = _geoCount[geoEntity.RowKey] + 1;
                    }
                    else {
                        log.LogInformation($"Creating GeoCount for redirect entity {queuedHttpRequest.Path}");
                        _geoCount.Add(geoEntity.RowKey, 1);
                    }

                    redirect.GeoCount = JsonConvert.SerializeObject(_geoCount);
                    await RedirectEntity.put(redirectTable, redirect);
                    return;

                }


                log.LogError($"Http request {queuedHttpRequest.Path} for click handling doesn't match handled path");
                throw new System.Exception($"Http request {queuedHttpRequest.Path} for click handling doesn't match handled path");
            
            }

            log.LogError($"Free geo ip lookup for IP {queuedHttpRequest.RemoteIpAddress} failed with status code {getResponse.StatusCode}");
            throw new System.Exception($"Free geo ip lookup for IP {queuedHttpRequest.RemoteIpAddress} failed with status code {getResponse.StatusCode}");

        }
    
    }

}