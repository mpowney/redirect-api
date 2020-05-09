using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using api.entities;

namespace api.v1
{
    public static class Synchronise
    {

        [FunctionName("TriggerRedirectsSync")]
        public static async void TriggerRedirectsSync(
            [TimerTrigger("%NODE_SYNC_SCHEDULE%")]TimerInfo myTimer, 
            [Table(TableNames.Nodes)] CloudTable nodeTable,
            [Queue(QueueNames.SynchroniseRedirects)] ICollector<string> syncRedirectsQueue)
        {

            List<NodeEntity> nodes = await NodeEntity.get(nodeTable);
            if (nodes != null) {
                foreach (NodeEntity node in nodes) {
                    syncRedirectsQueue.Add(node.RowKey);
                }
            }

        }


        [FunctionName("ProcessRedirectsSync")]
        public static async void ProcessRedirectsSync(
            [QueueTrigger(QueueNames.SynchroniseRedirects)] string node,
            [Table(TableNames.Redirects)] CloudTable redirectTable,
            [Table(TableNames.Domains)] CloudTable domainTable,
            [Queue(QueueNames.ProcessClicksGeo), StorageAccount("AzureWebJobsStorage")] ICollector<HttpRequestEntity> processClicksGeoQueue,
            ILogger log,
            ExecutionContext context)
        {

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            string connectionString = config[$"NODE_SYNC_CONNECTION_{node}"];

            if (connectionString == null) {
                throw new Exception($"No connection string found for node [{node}]. Aborting.");
            }

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable destinationRedirectTable = tableClient.GetTableReference(TableNames.Redirects);
            CloudTable destinationDomainTable = tableClient.GetTableReference(TableNames.Domains);

            await destinationRedirectTable.CreateIfNotExistsAsync();
            await destinationDomainTable.CreateIfNotExistsAsync();

            List<DomainEntity> domains = await DomainEntity.get(domainTable, null);
            List<string> uniqueAccounts = new List<string>();

            foreach (DomainEntity domain in domains) {
                await DomainEntity.put(destinationDomainTable, domain);

                if (uniqueAccounts.FindIndex(checkAccount => checkAccount == domain.Account) == -1) {
                    uniqueAccounts.Add(domain.Account);
                } 
            }

            List<DomainEntity> destinationDomains = await DomainEntity.get(destinationDomainTable, null);
            foreach(DomainEntity destinationDomain in destinationDomains) {
                if (domains.FindIndex(checkDomain => checkDomain.RowKey == destinationDomain.RowKey) == -1) {
                    await DomainEntity.delete(destinationRedirectTable, destinationDomain);
                }
            }


            foreach (string account in uniqueAccounts) {

                List<RedirectEntity> redirects = await RedirectEntity.get(redirectTable, account);

                foreach (RedirectEntity redirect in redirects) {
                    await RedirectEntity.put(destinationRedirectTable, redirect);
                }

                List<RedirectEntity> destinationRedirects = await RedirectEntity.get(destinationRedirectTable, account);
                foreach(RedirectEntity destinationRedirect in destinationRedirects) {
                    if (redirects.FindIndex(checkRedirect => checkRedirect.RowKey == destinationRedirect.RowKey) == -1) {
                        await RedirectEntity.delete(destinationRedirectTable, destinationRedirect);
                    }
                }

            }

        }

    }
}

