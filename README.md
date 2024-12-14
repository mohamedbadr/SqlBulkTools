<html>
<head>
<title>SQL Bulk Tools</title>
</head>
<body>

<h1>SQL Bulk Tools</h1>

<p>This NuGet package provides efficient methods for performing bulk insert and update operations on SQL Server databases. It is designed to significantly improve performance compared to traditional row-by-row methods, especially when dealing with large datasets.</p>

<h2>Key Features:</h2>

<ul>
<li>Bulk Insert: Quickly and efficiently loads large amounts of data into SQL Server tables.</li>
<li>Bulk Update: Updates multiple rows in a table based on specified criteria, optimizing performance.</li>
<li>Customizable: Allows for configuration of options like batch size</li>
<li>Performance-Optimized: Leverages SQL Server's bulk loading capabilities for maximum speed.</li>
<li>Easy to Use: Simple API with clear methods and parameters.</li>
</ul>

<h2>Installation:</h2>

<ol>
<li>Open your project in Visual Studio or a similar IDE.</li>
<li>Right-click on the project and select "Manage NuGet Packages."</li>
<li>Search for "Utilities.SqlBulkTools" (.</li>
<li>Install the package.</li>
</ol>

<h2>Usage:</h2>

```csharp
using SqlBulkTools;

// Initiate object of BulkOperations class
var bulkOperations = new BulkOperations(ConnectionString, "TableName");

// or ovedrride the default batch size tomake it faster for large datasets
var bulkOperations = new BulkOperations(ConnectionString, "TableName",100000);

// Perform a bulk insert
await bulkOperations.BulkInsertAsync(data)

// Perform a bulk update
await bulkOperations.BulkUpdateAsync(list)
