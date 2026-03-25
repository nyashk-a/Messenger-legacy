using Microsoft.Data.Sqlite;
using Shared.Source.tools;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Channels;
using static MessengerServer.DataBaseControl.SQLiteHandlerVersions.AA.SQLiteHandler;

namespace MessengerServer
{
    internal class Table : IDisposable
    {
        public readonly string TableName;
        public readonly string DatabasePath;
        public readonly List<Column> Columns = new();

        private readonly Channel<Func<Task>> bgTasks = Channel.CreateUnbounded<Func<Task>>();                         // для действий, которые ничего не возвращают (они же "SET_. . .")
        private readonly Task Executor;
        private readonly CancellationTokenSource _cts = new();

        public Table(string tName, string dbPath)
        {
            TableName = tName;
            DatabasePath = dbPath;
            Executor = Exec(_cts.Token);
            bgTasks.Writer.TryWrite(async () =>
            {
                using (SqliteConnection connection = new($"Data Source={DatabasePath}"))
                {
                    await connection.OpenAsync();
                    try
                    {
                        var cmdCheck = connection.CreateCommand();
                        cmdCheck.CommandText = "SELECT EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @tableName);";
                        cmdCheck.Parameters.AddWithValue("@tableName", TableName);

                        var result = await cmdCheck.ExecuteScalarAsync();

                        if (Convert.ToInt32(result) == 0)
                        {
                            Dispose();
                            return;
                        }



                        var cmdToGetColumnsList = connection.CreateCommand();
                        cmdToGetColumnsList.CommandText = $"PRAGMA table_info(\"{TableName}\");";
                        var reader = await cmdToGetColumnsList.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            var n = reader.GetString(reader.GetOrdinal("name"));
                            var t = reader.GetString(reader.GetOrdinal("type"));
                            var c = ParseColumnType(n, t);
                            Columns.Add(c);
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugTool.Log(new DebugTool.log(
                            DebugTool.log.Level.Error,
                            $"initalize bd: {ex}",
                            TableName
                        ));
                    }
                }
            });                                                                 // инициализация таблицы
        }


        public async Task<int?> GS_InsertRow()                                                                       // у нас железокаменно существует айдишник строки. всегда. и он всегда называется "rID"
        {
            using (SqliteConnection connection = new($"Data Source={DatabasePath}"))
            {
                await connection.OpenAsync();
                try
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = $"INSERT INTO {TableName} DEFAULT VALUES; SELECT last_insert_rowid();";
                    var newId = await cmd.ExecuteScalarAsync();
                    await connection.CloseAsync();
                    return newId != null ? Convert.ToInt32(newId) : null;
                }
                catch (Exception ex)
                {
                    DebugTool.Log(new DebugTool.log(
                        DebugTool.log.Level.Warning,
                        $"cant insert row: {ex}",
                        TableName
                    ));
                    return null;
                }
            }
        }

        public void S_setValue<T>(int rID, string columnName, T vlaue)
        {
            bgTasks.Writer.TryWrite(async () =>
            {

            });
        }

        private static Column ParseColumnType(string name, string typeString)
        {
            typeString = typeString.Trim().ToUpperInvariant();

            if (typeString.StartsWith("VARCHAR"))
            {
                int start = typeString.IndexOf('(');
                int end = typeString.IndexOf(')');
                if (start > 0 && end > start)
                {
                    string lengthStr = typeString.Substring(start + 1, end - start - 1);
                    if (int.TryParse(lengthStr, out int length))
                    {
                        return new Column(name, Column.Types.VARCHAR, length);
                    }
                }
                return new Column(name, Column.Types.VARCHAR);
            }

            return typeString switch
            {
                "TEXT" => new Column(name, Column.Types.TEXT),
                "INTEGER" => new Column(name, Column.Types.INTEGER),
                "FLOAT" or "REAL" => new Column(name, Column.Types.FLOAT),
                "DATE" => new Column(name, Column.Types.DATE),
                "TIME" => new Column(name, Column.Types.TIME),
                "DATETIME" => new Column(name, Column.Types.DATETIME),
                _ => new Column(name, Column.Types.TEXT)
            };
        }

        private async Task Exec(CancellationToken cts)
        {
            var reader = bgTasks.Reader;
            await foreach (var tsk in reader.ReadAllAsync(cts))
            {
                try
                {
                    await tsk();
                }
                catch (Exception ex)
                {
                    DebugTool.Log(new DebugTool.log(
                        DebugTool.log.Level.Warning,
                        $"broken exec of {ex}",
                        TableName
                    ));
                }
            }
        }

        public async void Dispose()
        {
            await _cts.CancelAsync();
            await Executor.WaitAsync(TimeSpan.FromSeconds(5));
            _cts.Dispose();
        }
    }
}
