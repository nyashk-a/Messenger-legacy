using Microsoft.Data.Sqlite;
using Shared.Source.tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedTests
{
    public static class TableController
    {
        private readonly static Dictionary<string, Table> TableList = new();
        private static HashSet<string> _tableList = new();


        public static readonly string PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MessengerDataBase"); // путь к корневой папке базы
        public static readonly string ImageSourcePath = Path.Combine(PATH, "massiveUserData");                                                             // путь к папке с картинками
        public static readonly string DatabasePath = Path.Combine(PATH, "userDataStorage.db");                                                             // путь к бд

        public static async Task InitalizeDatabaseController()
        {
            Console.WriteLine(PATH);
            TableList.Clear();

            await Table.ConfigureDatabaseAsync(DatabasePath);

            using (SqliteConnection connection = new SqliteConnection($"Data Source={DatabasePath}"))
            {
                await connection.OpenAsync();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string tableName = reader.GetString(0);
                        var table = await Table.defineTable(tableName);
                        TableList[tableName] = table;
                    }
                }
            }

            _tableList = new(TableList.Select(t => t.Key));
        }

        public static SqliteConnection CreateConnection()
        {
            return new SqliteConnection($"Data Source={DatabasePath}");
        }

        public static async Task CreateTable(string tableName, Column[] columns, SqliteConnection? sqc = null)
        {
            bool ownsConnection = sqc == null;
            SqliteConnection connection = sqc ?? CreateConnection();

            try
            {
                if (ownsConnection) await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                var sb = new StringBuilder();
                sb.Append($"CREATE TABLE IF NOT EXISTS {tableName} (rID INTEGER PRIMARY KEY");
                foreach (var col in columns)
                {
                    sb.Append($", {col.Name} {col.Type}");
                }
                sb.Append(")");
                cmd.CommandText = sb.ToString();
                await cmd.ExecuteNonQueryAsync();

                var newTable = await Table.defineTable(tableName);
                TableList[tableName] = newTable;
                _tableList.Add(tableName);
            }
            finally
            {
                if (ownsConnection) await connection.DisposeAsync();
            }
        }

        public static async Task RemoveTable(string tableName, SqliteConnection? sqc=null)
        {
            if (!_tableList.Contains(tableName))
            {
                DebugTool.Log(new DebugTool.log(
                            DebugTool.log.Level.Warning,
                            $"table name is so strange! >__<",
                            "TableControllerLogs.txt"
                        ));
                return;
            }

            if (sqc == null)
            {
                using (SqliteConnection connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    await connection.OpenAsync();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            else
            {
                var cmd = sqc.CreateCommand();
                cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public static Table? GetTable(string tableName)
        {
            TableList.TryGetValue(tableName, out var table);
            return table;
        }


        public static async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sql, SqliteConnection? sqc = null)
        {
            var results = new List<Dictionary<string, object>>();
            bool ownsConnection = sqc == null;
            SqliteConnection connection = sqc ?? CreateConnection();

            try
            {
                if (ownsConnection)
                    await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sql;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    results.Add(row);
                }
            }
            catch (Exception ex)
            {
                DebugTool.Log(new DebugTool.log(
                    DebugTool.log.Level.Error,
                    $"Error executing query: {ex.Message}\nSQL: {sql}",
                    "TableControllerLogs.txt"
                ));
                throw;
            }
            finally
            {
                if (ownsConnection)
                    await connection.DisposeAsync();
            }

            return results;
        }
        public static async Task ExecuteNonQueryAsync(string sql, SqliteConnection? sqc = null)
        {
            bool ownsConnection = sqc == null;
            SqliteConnection connection = sqc ?? CreateConnection();

            try
            {
                if (ownsConnection)
                    await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                DebugTool.Log(new DebugTool.log(
                    DebugTool.log.Level.Error,
                    $"Error executing non-query: {ex.Message}\nSQL: {sql}",
                    "TableControllerLogs.txt"
                ));
                throw;
            }
            finally
            {
                if (ownsConnection)
                    await connection.DisposeAsync();
            }
        }
    }
}
