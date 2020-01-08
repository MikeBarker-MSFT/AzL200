using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace CustomerApiFunctions
{
    class SqlExecutor
    {
        private const string KEY_SQL_CONNECTION_STRING = "SqlConnectionString";

        private string SqlConnectionString { get; }

        public SqlExecutor(IConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));


            this.SqlConnectionString = config.GetConnectionString(KEY_SQL_CONNECTION_STRING);
        }


        public async Task Execute(string commandText)
        {
            using (var connection = new SqlConnection(this.SqlConnectionString))
            {
                Task openConnectionTask = connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = commandText;

                    await openConnectionTask.ConfigureAwait(false);

                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<T> Execute<T>(string commandText)
        {
            using (var connection = new SqlConnection(this.SqlConnectionString))
            {
                Task openConnectionTask = connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = commandText;

                    await openConnectionTask.ConfigureAwait(false);

                    object result = await command.ExecuteScalarAsync().ConfigureAwait(false);

                    return (T)result;
                }
            }
        }

        public async Task<List<T>> Execute<T>(string commandText, Func<SqlDataReader, Task<List<T>>> readCallbackAsync)
        {
            using (var connection = new SqlConnection(this.SqlConnectionString))
            {
                Task openConnectionTask = connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = commandText;

                    await openConnectionTask.ConfigureAwait(false);

                    var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                    var result = await readCallbackAsync(reader).ConfigureAwait(false);

                    return result;
                }
            }
        }
    }
}
