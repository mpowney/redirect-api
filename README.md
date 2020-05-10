# Azure Redirect API

This project implements a minimalist Azure based server-side redirect handler.

Implemented as a series of Azure Functions, the project offers:
* an authenticated admin API - which can be used by its related client side static website
* redirect handling - short codes are looked up against the Azure storage table
* click count and geo IP analysis - every redirect handled by the app service is counted, and analysed for its originating location

## Build and release status

[![Build Status](https://dev.azure.com/mpowney/redirect-api/_apis/build/status/mpowney.redirect-api?branchName=master)](https://dev.azure.com/mpowney/redirect-api/_build/latest?definitionId=8&branchName=master) [![Release Status](https://vsrm.dev.azure.com/mpowney/_apis/public/Release/badge/d9a6dc6f-a60e-4088-bcb5-be1c6c48ce28/1/1)](https://dev.azure.com/mpowney/redirect-api/_release?definitionId=1&view=mine&_a=releases)

## Nodes and Master instances

This Function app can be deployed as a single instance in master mode, or for worldwide distributed workloads, as a master / node configuration. When deployed as a Function app service, the configuration of master mode, or node mode, is distinguished by the Function app settings.

### Single-instance mode

No app settings are required for single-instance mode.  Configuration of custom host names is required in the associated content storage table named ```v1Domains```.

### Distributed master mode

When deployed as a Function app service in master mode, the app performs the following:

* On a timer schedule, executes a sync of all redirects and domains, to all registered nodes 

| Setting name | Example value | Description |
|---|---|---|
| NODE_SYNC_SCHEDULE | 0 */1 * * * * | [Cron expression](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer?tabs=csharp#ncrontab-expressions) that determines how often a full sync of all custom domains, and redirects, should be executed. |
| NODE_SYNC_CONN_```node-name``` | DefaultEndpointsProtocol=https;AccountName=```storage-account-name```;AccountKey=```storage-account-key```;EndpointSuffix=core.windows.net | Connection string used to connect to the storage account named by the ```node-name``` expression. ```node-name``` must correspond to a row present in the ```v1Nodes``` table |


The Function app's storage account must have a table named ```v1Nodes```, and the table must have one row per node to synchronise to. For each row, the PartitionKey should be a blank string, and the RowKey should be the name of the node, and the name must match a NODE_SYNC_CONN_```node-name``` configuration setting.

### Distributed node mode

When deployed as a Function app service in node mode, the app performs the following:

* If a requested domain / short code combination doesn't exist in the app's storage tables, perform a lookup against the master node to check if it exists there before returning not found
* Click requests tracked against this app will be sent to the master node's click tracking queue for processing

| Setting name | Example value | Description |
|---|---|---|
| NODE_SYNC_MASTER_HOST | redirect-api-ause.azurewebsites.net | Host name of the master node app service |
| NODE_SYNC_MASTER_CONN | DefaultEndpointsProtocol=https;AccountName=```master-storage-account-name```;AccountKey=```master-storage-account-key```;QueueEndpoint=https://```master-storage-account-name```.queue.core.windows.net/; | Connection string used to connect to the master node's storage account, to push click request queue messages to be processed centrally |

