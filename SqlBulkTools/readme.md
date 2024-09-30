# SQL Bulk Tools

This NuGet package provides efficient methods for performing bulk insert and update operations on SQL Server databases. It is designed to significantly improve performance compared to traditional row-by-row methods, especially when dealing with large datasets.

## Key Features:

* Bulk Insert: Quickly and efficiently loads large amounts of data into SQL Server tables.
* Bulk Update: Updates multiple rows in a table based on specified criteria, optimizing performance.
* Customizable: Allows for configuration of options like batch size
* Performance-Optimized: Leverages SQL Server's bulk loading capabilities for maximum speed.
* Easy to Use: Simple API with clear methods and parameters.

## Usage:

```csharp
using BulkOperations;

// Initiate object of SqlBulkTools
var sqlBulkTools = new SqlBulkTools(ConnectionString, "TableName");
var sqlBulkTools = new SqlBulkTools(ConnectionString, "TableName", commitBatchSize:2000);

// Perform a bulk insert
await sqlBulkTools.BulkInsertAsync(data)

// Perform a bulk update
await sqlTools.BulkUpdateAsync(list)