using SqlBulkTools.Core;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace SqlBulkTools
{
    public class BulkOperations
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly int _commitBatchSize = 1000;
        private const string TempTableName = "#TempTable";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <param name="tableName">Table name to be inserted</param>
        /// <param name="commitBatchSize">batch size for every page to be inserted</param>
        public BulkOperations(string connectionString, string tableName, int commitBatchSize)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _commitBatchSize = commitBatchSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <param name="tableName">Table name to be inserted</param>
        public BulkOperations(string connectionString, string tableName)
        {
            _connectionString = connectionString;
            _tableName = tableName;
        }

        public async Task BulkInsertAsync<T>(IList<T> data)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var pages = (data.Count / _commitBatchSize) + (data.Count % _commitBatchSize == 0 ? 0 : 1);
                for (var page = 0; page < pages; page++)
                {
                    var dt = data.Skip(page * _commitBatchSize).Take(_commitBatchSize).ToDataTable();
                    await BulkInsert(dt, connection, transaction);
                }

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await connection.CloseAsync();

                throw;
            }
        }

        public async Task BulkUpdateAsync<T>(IList<T> data)
        {
            var dt = data.ToDataTable();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var transaction = (await conn.BeginTransactionAsync()) as SqlTransaction;

            await using var command = new SqlCommand("", conn);
            command.Transaction = transaction;


            try
            {


                //Creating temp table on database
                command.CommandText = GenerateCreateTableScript<T>();
                await command.ExecuteNonQueryAsync();

                //Bulk insert into temp table
                using (var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.BulkCopyTimeout = 660;
                    bulkCopy.DestinationTableName = TempTableName;
                    await bulkCopy.WriteToServerAsync(dt);
                    bulkCopy.Close();
                }

                // Updating destination table, and dropping temp table
                command.CommandTimeout = 300;
                command.CommandText = GenerateUpdateScript<T>(_tableName);
                await command.ExecuteNonQueryAsync();

            }
            catch (Exception ex)
            {
                await transaction?.RollbackAsync()!;
                throw;
            }
            finally
            {
                await transaction?.CommitAsync()!;
                conn.Close();
            }
        }

        private Task BulkInsert(DataTable dt, SqlConnection connection, IDbTransaction transaction)
        {
            var bulkCopy =
                new SqlBulkCopy
                (
                    connection,
                    SqlBulkCopyOptions.TableLock |
                    SqlBulkCopyOptions.FireTriggers,
                    transaction as SqlTransaction
                )
                {
                    DestinationTableName = _tableName
                };


            return bulkCopy.WriteToServerAsync(dt);
        }

        private static string GenerateCreateTableScript<T>()
        {
            var properties = typeof(T).GetProperties();
            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE {TempTableName} (");

            foreach (var property in properties)
            {
                var sqlType = MapToSqlType(property.PropertyType);
                sb.Append($"[{property.Name}] {sqlType},");
            }

            sb.Remove(sb.Length - 1, 1);
            sb.Append(");");

            return sb.ToString();
        }

        private static string MapToSqlType(Type type)
        {
            // This is a simplified mapping. Consider more complex scenarios.
            return type.Name switch
            {
                "String" => "nvarchar(MAX)",
                "Int32" => "int",
                "Int64" => "bigint",
                "Boolean" => "bit",
                "DateTime" => "datetime2",
                "Decimal" => "decimal(18, 3)",
                "Double" => "float",
                _ => "nvarchar(MAX)"
            };
        }

        private static string GenerateUpdateScript<T>(string tableName)
        {
            var properties = typeof(T).GetProperties();
            var primaryKey = properties.FirstOrDefault(p => p.GetCustomAttributes(typeof(KeyAttribute), false).Any());

            if (primaryKey is null)
                throw new ArgumentException($"No primary key property fount in the object type {typeof(T).Name}, please set a primary key using 'Key' attribute");

            var sb = new StringBuilder();
            sb.Append($"UPDATE {tableName} SET ");

            foreach (var property in properties)
            {
                if (property == primaryKey)
                    continue;

                sb.Append($"[{property.Name}] = Temp.[{property.Name}],");
            }

            sb.Remove(sb.Length - 1, 1);

            //todo:replace...  with actual join condition, which is the primary key of the table
            sb.Append($" FROM {tableName} T INNER JOIN {TempTableName} Temp ON T.{primaryKey.Name} = Temp.{primaryKey.Name}; DROP TABLE {TempTableName};");

            return sb.ToString();
        }
    }
}
