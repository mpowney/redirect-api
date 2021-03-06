using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace api.entities
{
    public class DomainEntity : TableEntity
    {
        public DomainEntity() {}
        public DomainEntity(string key, string account) {
            
            this.PartitionKey = string.Empty;
            this.RowKey = key;
            this.Account = account;
            
        }
        public string Account { get; set; }
        public bool Configured { get; set; }
        public bool SslConfigured { get; set; }
        public static async Task<List<DomainEntity>> get(CloudTable domainTable, string? key) {

            await domainTable.CreateIfNotExistsAsync();

            TableQuery<DomainEntity> rangeQuery = key != null ? new TableQuery<DomainEntity>().Where(
                    TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, 
                                $"{string.Empty}"),
                            TableOperators.And,
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, 
                                $"{key}"))) :
                new TableQuery<DomainEntity>().Where(
                            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, 
                                $"{string.Empty}"));

            var domainsFound = await domainTable.ExecuteQuerySegmentedAsync(rangeQuery, null);
            if (domainsFound.Results.Count > 0) {

                List<DomainEntity> entities = domainsFound.Results;
                return entities;

            }
            else {

                return null;

            }

        }

        public static async Task<bool> put(CloudTable domainTable, string key, string account) {
     
            await domainTable.CreateIfNotExistsAsync();
            
            try {

                DomainEntity newEntity = new DomainEntity(key, account);
                TableOperation insertEntityOperation = TableOperation.InsertOrMerge(newEntity);
                await domainTable.ExecuteAsync(insertEntityOperation);

            }
            catch {

                return false;
                
            }
            return true;

        }

        public static async Task<bool> put(CloudTable domainTable, DomainEntity entity) {
     
            await domainTable.CreateIfNotExistsAsync();
            
            try {

                TableOperation insertCacheOperation = TableOperation.InsertOrMerge(entity);
                await domainTable.ExecuteAsync(insertCacheOperation);

            }
            catch {

                return false;
                
            }
            return true;

        }

        public static async Task<bool> delete(CloudTable domainTable, DomainEntity entity) {
     
            await domainTable.CreateIfNotExistsAsync();
            
            try {

                TableOperation deleteOperation = TableOperation.Delete(entity);
                await domainTable.ExecuteAsync(deleteOperation);

            }
            catch {

                return false;
                
            }
            return true;

        }


    }

}
