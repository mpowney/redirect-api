using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace api.entities
{
    public class RedirectEntity : TableEntity
    {
        public RedirectEntity() {}
        public RedirectEntity(string collection, string key, string redirectTo, int clickCount, IDictionary<string, int> geoCount) {
            
            string _geoCount = JsonConvert.SerializeObject(geoCount);

            this.PartitionKey = collection;
            this.RowKey = key;
            this.RedirectTo = redirectTo;
            this.ClickCount = clickCount;
            this.GeoCount = _geoCount;
            
        }
        public string RedirectTo { get; set; }
        public int ClickCount { get; set; }
        public string GeoCount { get; set; }
        public static async Task<RedirectEntity> get(CloudTable redirectTable, string? collection, string key) {

            await redirectTable.CreateIfNotExistsAsync();

            TableQuery<RedirectEntity> rangeQuery = new TableQuery<RedirectEntity>().Where(
                TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, 
                            $"{collection ??= string.Empty}"),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, 
                            $"{key}")));

            var sessionRedirectFound = await redirectTable.ExecuteQuerySegmentedAsync(rangeQuery, null);
            if (sessionRedirectFound.Results.Count > 0) {

                RedirectEntity entity = sessionRedirectFound.Results.ToArray()[0];
                return entity;

            }
            else {

                return null;

            }

        }
        public static async Task<RedirectEntity[]> get(CloudTable redirectTable, string? collection) {

            await redirectTable.CreateIfNotExistsAsync();

            TableQuery<RedirectEntity> rangeQuery = new TableQuery<RedirectEntity>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, 
                        $"{collection ??= string.Empty}"));

            var sessionRedirectFound = await redirectTable.ExecuteQuerySegmentedAsync(rangeQuery, null);
            if (sessionRedirectFound.Results.Count > 0) {

                RedirectEntity[] entities = sessionRedirectFound.Results.ToArray();
                return entities;

            }
            else {

                return null;

            }

        }


        public static async Task<bool> put(CloudTable redirectTable, string collection, string key, string redirectTo, int clickCount, IDictionary<string, int> geoCount) {
     
            await redirectTable.CreateIfNotExistsAsync();
            
            try {

                RedirectEntity newEntity = new RedirectEntity(collection, key, redirectTo, clickCount, geoCount);
                TableOperation insertCacheOperation = TableOperation.InsertOrMerge(newEntity);
                await redirectTable.ExecuteAsync(insertCacheOperation);

            }
            catch {

                return false;
                
            }
            return true;

        }

        public static async Task<bool> put(CloudTable redirectTable, RedirectEntity entity) {
     
            await redirectTable.CreateIfNotExistsAsync();
            
            try {

                TableOperation insertCacheOperation = TableOperation.InsertOrMerge(entity);
                await redirectTable.ExecuteAsync(insertCacheOperation);

            }
            catch {

                return false;
                
            }
            return true;

        }

    }

}
