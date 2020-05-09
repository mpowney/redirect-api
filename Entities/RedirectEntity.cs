using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace api.entities
{
    public class RedirectEntity : TableEntity
    {
        public RedirectEntity() {}
        public RedirectEntity(string collection, string key, string redirectTo, int clickCount, IDictionary<string, int> geoCount, DateTime created, bool recycled) {
            
            string _geoCount = JsonConvert.SerializeObject(geoCount);

            this.PartitionKey = collection;
            this.RowKey = key;
            this.RedirectTo = redirectTo;
            this.ClickCount = clickCount;
            this.GeoCount = _geoCount;
            this.Created = created;
            this.Recycled = recycled;
            
        }
        public string RedirectTo { get; set; }
        public int ClickCount { get; set; }
        public string GeoCount { get; set; }
        public DateTime Created { get; set; }
        public bool Recycled { get; set; }

        public async Task<Dictionary<string, GeoEntityCount>> GetGeoCount(CloudTable geoTable) {

            Dictionary<string, int> _geoCount = JsonConvert.DeserializeObject<Dictionary<string, int>>(this.GeoCount ??= "{}");;
            Dictionary<string, GeoEntityCount> response = new Dictionary<string, GeoEntityCount>();
            foreach (string key in _geoCount.Keys) {
                response.Add(key, (GeoEntityCount)(await GeoEntity.get(geoTable, key)));
            }

            return response;
        }

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
        public static async Task<List<RedirectEntity>> get(CloudTable redirectTable, string? collection) {

            await redirectTable.CreateIfNotExistsAsync();

            TableQuery<RedirectEntity> rangeQuery = new TableQuery<RedirectEntity>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, 
                        $"{collection ??= string.Empty}"));

            var sessionRedirectFound = await redirectTable.ExecuteQuerySegmentedAsync(rangeQuery, null);
            if (sessionRedirectFound.Results.Count > 0) {

                return sessionRedirectFound.Results;

            }
            else {

                return null;

            }

        }


        public static async Task<bool> put(CloudTable redirectTable, string collection, string key, string redirectTo, int clickCount, IDictionary<string, int> geoCount, DateTime created, bool recycled) {
     
            await redirectTable.CreateIfNotExistsAsync();
            
            try {

                RedirectEntity newEntity = new RedirectEntity(collection, key, redirectTo, clickCount, geoCount, created, recycled);
                TableOperation insertEntityOperation = TableOperation.InsertOrMerge(newEntity);
                await redirectTable.ExecuteAsync(insertEntityOperation);

            }
            catch {

                return false;
                
            }
            return true;

        }

        public static async Task<bool> put(CloudTable redirectTable, RedirectEntity entity) {
     
            await redirectTable.CreateIfNotExistsAsync();
            
            try {

                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
                await redirectTable.ExecuteAsync(insertOrMergeOperation);

            }
            catch {

                return false;
                
            }
            return true;

        }

        public static async Task<bool> delete(CloudTable redirectTable, RedirectEntity entity) {
     
            await redirectTable.CreateIfNotExistsAsync();
            
            try {

                TableOperation deleteOperation = TableOperation.Delete(entity);
                await redirectTable.ExecuteAsync(deleteOperation);

            }
            catch {

                return false;
                
            }
            return true;

        }

    }

}
