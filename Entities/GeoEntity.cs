using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace api.entities
{

    public class GeoEntity : TableEntity {
        public GeoEntity() { }
        public GeoEntity(string countryCode, string countryName, string regionCode, string city, string timeZone, double latitude, double longitude) {
            this.CountryCode = countryCode;
            this.CountryName = countryName;
            this.RegionCode = regionCode;
            this.City = city;
            this.TimeZone = timeZone;
            this.Latitude = latitude;
            this.Longitude = longitude;

            this.RowKey = $"{this.CountryCode}-{this.RegionCode}-{this.City}-{this.ZipCode}";

        }

        public GeoEntity(dynamic rawObject) {
            this.CountryCode = rawObject.country_code;
            this.CountryName = rawObject.country_name;
            this.RegionCode = rawObject.region_code;
            this.City = rawObject.city;
            this.TimeZone = rawObject.time_zone;
            this.Latitude = rawObject.latitude;
            this.Longitude = rawObject.longitude;

            this.RowKey = $"{this.CountryCode}-{this.RegionCode}-{this.City}-{this.ZipCode}";
        }

        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string RegionCode { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        public string TimeZone {get; set; }
        public double Latitude {get; set; }
        public double Longitude {get; set; }

        public static async Task<GeoEntity> get(CloudTable geoTable, string key) {

            await geoTable.CreateIfNotExistsAsync();

            TableQuery<GeoEntity> rangeQuery = new TableQuery<GeoEntity>().Where(
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, 
                            $"{key}"));

            var sessionRedirectFound = await geoTable.ExecuteQuerySegmentedAsync(rangeQuery, null);
            if (sessionRedirectFound.Results.Count > 0) {

                GeoEntity entity = sessionRedirectFound.Results.ToArray()[0];
                return entity;

            }
            else {

                return null;

            }

        }

        public static async Task<bool> put(CloudTable geoTable, GeoEntity entityToInsert) {
     
            await geoTable.CreateIfNotExistsAsync();
            
            try {

                GeoEntity entity = await GeoEntity.get(geoTable, entityToInsert.RowKey);
                if (entity != null) {
                    return true;
                }

                TableOperation insertGeoOperation = TableOperation.InsertOrMerge(entityToInsert);
                await geoTable.ExecuteAsync(insertGeoOperation);

            }
            catch {

                return false;
                
            }
            return true;

        }

    }
    public class GeoEntityCount : GeoEntity {
        public int ClickCount { get; set; }
    }

}