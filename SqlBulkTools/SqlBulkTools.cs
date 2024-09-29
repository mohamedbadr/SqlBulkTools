using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using SqlBulkTools.Core;

namespace SqlBulkTools
{
    public class SqlBulkTools
    {
        private string _connectionString = null!;
        private string _tableName = null!;
        private int _commitBatchSize = 1000;

        public void SetConnectionString(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void SetTableName(string tableName)
        {
            _tableName = tableName;
        }

        public void SetCommitBatchSize(int commitBatchSize)
        {
            _commitBatchSize = commitBatchSize;
        }

        public async Task BulkInsertAsync<T>(IList<T> data, string tableName)
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

        private async Task BulkInsert(DataTable dt, SqlConnection connection, IDbTransaction transaction)
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


            await bulkCopy.WriteToServerAsync(dt);
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidEnumArgumentException("Connection string is not set.");

            if (string.IsNullOrWhiteSpace(_tableName))
                throw new InvalidEnumArgumentException("Table name is not set.");
        }
    }
}
