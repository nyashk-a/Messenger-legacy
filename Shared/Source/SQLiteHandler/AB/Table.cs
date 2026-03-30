using Microsoft.Data.Sqlite;
using Shared.Source.tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Source.SQLiteHandler.AB
{
    public class Table
    {
        public readonly string TableName;
        public static string DatabasePath;
        public IReadOnlyList<Column> Columns;

        private HashSet<string> _columnNames;


        private Table(string tName)
        {
            TableName = tName;
        }

        public static async Task<Table?> defineTable(string tName)
        {
            try
            {
                Table t = new(tName);
                await t.InitializeAsync();
                return t;
            }
            catch (Exception ex)
            {
                DebugTool.Log(new DebugTool.log(
                DebugTool.log.Level.Error,
                $"can't find database: {ex}",
                "global_database_controller_logs.txt"
            ));
                return null;
            }
        }

        public static async Task ConfigureDatabaseAsync(string dbPath)
        {
            DatabasePath = dbPath;
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000; PRAGMA synchronous = NORMAL;";
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InitializeAsync()
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
                        throw new Exception("Table is no exist");
                    }



                    var cmdToGetColumnsList = connection.CreateCommand();
                    cmdToGetColumnsList.CommandText = $"PRAGMA table_info(\"{TableName}\");";
                    var reader = await cmdToGetColumnsList.ExecuteReaderAsync();
                    List<Column> columns = new();
                    while (await reader.ReadAsync())
                    {
                        var n = reader.GetString(reader.GetOrdinal("name"));
                        var t = reader.GetString(reader.GetOrdinal("type"));
                        var c = ParseColumnType(n, t);
                        columns.Add(c);
                    }

                    Columns = columns.AsReadOnly();
                    _columnNames = new(columns.Select(c => c.Name));
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
        }



        // у нас железокаменно существует айдишник строки. всегда. и он всегда называется "rID"
        public async Task<int?> InsertRow(SqliteConnection? sqc=null)
        {
            if (sqc == null)
            {
                using (SqliteConnection connection = new($"Data Source={DatabasePath}"))
                {
                    await connection.OpenAsync();
                    try
                    {
                        var cmd = connection.CreateCommand();
                        cmd.CommandText = $"INSERT INTO {TableName} DEFAULT VALUES; SELECT last_insert_rowid();";
                        var newId = await cmd.ExecuteScalarAsync();
                        return newId != null ? Convert.ToInt32(newId) : null;
                    }
                    catch (Exception ex)
                    {
                        DebugTool.Log(new DebugTool.log(
                            DebugTool.log.Level.Warning,
                            $"can`t insert row: {ex}",
                            TableName
                        ));
                        return null;
                    }
                }
            }
            else
            {
                try
                {
                    var cmd = sqc.CreateCommand();
                    cmd.CommandText = $"INSERT INTO {TableName} DEFAULT VALUES; SELECT last_insert_rowid();";
                    var newId = await cmd.ExecuteScalarAsync();
                    return newId != null ? Convert.ToInt32(newId) : null;
                }
                catch (Exception ex)
                {
                    DebugTool.Log(new DebugTool.log(
                        DebugTool.log.Level.Warning,
                        $"can`t insert row: {ex}",
                        TableName
                    ));
                    return null;
                }
            }
        }

        public async Task RemoveRow(int rID, SqliteConnection? sqc=null)
        {
            if (sqc == null)
            {
                using (SqliteConnection connection = new($"Data Source={DatabasePath}"))
                {
                    await connection.OpenAsync();
                    try
                    {
                        var cmd = connection.CreateCommand();
                        cmd.CommandText = $"DELETE FROM {TableName} WHERE rID=@rid";

                        cmd.Parameters.AddWithValue("@rid", rID);

                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        DebugTool.Log(new DebugTool.log(
                        DebugTool.log.Level.Warning,
                        $"can't remove row: {ex}",
                        TableName
                    ));
                    }
                }
            }
            else
            {
                try
                {
                    var cmd = sqc.CreateCommand();
                    cmd.CommandText = $"DELETE FROM {TableName} WHERE rID=@rid";

                    cmd.Parameters.AddWithValue("@rid", rID);

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    DebugTool.Log(new DebugTool.log(
                    DebugTool.log.Level.Warning,
                    $"can't remove row: {ex}",
                    TableName
                ));
                }
            }
        }

        public async Task SetValue<T>(int rID, string columnName, T value, SqliteConnection? sqc = null)
        {
            if (!_columnNames.Contains(columnName))
            {
                DebugTool.Log(new DebugTool.log(
                        DebugTool.log.Level.Warning,
                        $"column name is so strange! >__<",
                        TableName
                    ));
                return;
            }

            if (sqc == null)
            {
                using (SqliteConnection connection = new($"Data Source={DatabasePath}"))
                {
                    await connection.OpenAsync();
                    try
                    {
                        var cmd = connection.CreateCommand();
                        cmd.CommandText = $"UPDATE {TableName} SET \"{columnName}\" = @value WHERE rID = @rID";

                        cmd.Parameters.AddWithValue("@value", value);
                        cmd.Parameters.AddWithValue("@rID", rID);

                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        DebugTool.Log(new DebugTool.log(
                        DebugTool.log.Level.Warning,
                        $"can't update row: {ex}",
                        TableName
                    ));
                    }
                }
            }
            else
            {
                try
                {
                    var cmd = sqc.CreateCommand();
                    cmd.CommandText = $"UPDATE {TableName} SET \"{columnName}\" = @value WHERE rID = @rID";

                    cmd.Parameters.AddWithValue("@value", value);
                    cmd.Parameters.AddWithValue("@rID", rID);

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    DebugTool.Log(new DebugTool.log(
                    DebugTool.log.Level.Warning,
                    $"can't update row: {ex}",
                    TableName
                ));
                }
            }
        }

        public async Task<T?> GetValue<T>(int rID, string columnName, SqliteConnection? sqc=null)
        {
            if (!_columnNames.Contains(columnName))
            {
                DebugTool.Log(new DebugTool.log(
                        DebugTool.log.Level.Warning,
                        $"column name is so strange! >__<",
                        TableName
                    ));
                return default;
            }


            if (sqc == null)
            {
                using (SqliteConnection connection = new($"Data Source={DatabasePath}"))
                {
                    await connection.OpenAsync();
                    try
                    {
                        var cmd = connection.CreateCommand();
                        cmd.CommandText = $"SELECT \"{columnName}\" FROM {TableName} WHERE rID=@rid";

                        cmd.Parameters.AddWithValue("@rid", rID);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                object value = reader[0];

                                if (value == DBNull.Value)
                                    return default;

                                return (T)Convert.ChangeType(value, typeof(T));
                            }
                            else
                            {
                                return default;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugTool.Log(new DebugTool.log(
                            DebugTool.log.Level.Warning,
                            $"some err in get val: {ex}",
                            TableName
                        ));
                        return default;
                    }
                }
            }
            else
            {
                try
                {
                    var cmd = sqc.CreateCommand();
                    cmd.CommandText = $"SELECT \"{columnName}\" FROM {TableName} WHERE rID=@rid";

                    cmd.Parameters.AddWithValue("@rid", rID);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            object value = reader[0];

                            if (value == DBNull.Value)
                                return default;

                            return (T)Convert.ChangeType(value, typeof(T));
                        }
                        else
                        {
                            return default;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugTool.Log(new DebugTool.log(
                        DebugTool.log.Level.Warning,
                        $"some err in get val: {ex}",
                        TableName
                    ));
                    return default;
                }
            }
        }

        public async Task<List<object>?> GetValue(int rID, string[] columnNames, SqliteConnection? sqc=null)
        {
            foreach (var cl in columnNames)
            {
                if (!_columnNames.Contains(cl))
                {
                    DebugTool.Log(new DebugTool.log(
                            DebugTool.log.Level.Warning,
                            $"column name is so strange! >__<",
                            TableName
                        ));
                    return null;
                }
            }

            if (sqc == null)
            {
                using (SqliteConnection connection = new($"Data Source={DatabasePath}"))
                {
                    await connection.OpenAsync();
                    try
                    {
                        var cmd = connection.CreateCommand();

                        string clmn = string.Join(", ", columnNames);

                        cmd.CommandText = $"SELECT {clmn} FROM {TableName} WHERE rID=@rid";

                        cmd.Parameters.AddWithValue("@rid", rID);

                        List<object> ans = new List<object>();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var values = new object[reader.FieldCount];
                                reader.GetValues(values);
                                ans.AddRange(values);
                            }
                            return ans;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugTool.Log(new DebugTool.log(
                            DebugTool.log.Level.Warning,
                            $"some err in get val: {ex}",
                            TableName
                        ));
                        return null;
                    }
                }
            }
            else
            {
                try
                {
                    var cmd = sqc.CreateCommand();
                    var sb = new StringBuilder();
                    foreach (var cl in columnNames)
                    {
                        if (sb.Length > 0) sb.Append(", ");
                        sb.Append($"\"{cl}\"");
                    }
                    string clmn = sb.ToString();
                    cmd.CommandText = $"SELECT {clmn} FROM {TableName} WHERE rID=@rid";

                    cmd.Parameters.AddWithValue("@rid", rID);

                    List<object> ans = new List<object>();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var values = new object[reader.FieldCount];
                            reader.GetValues(values);
                            ans.AddRange(values);
                        }
                        return ans;
                    }
                }
                catch (Exception ex)
                {
                    DebugTool.Log(new DebugTool.log(
                        DebugTool.log.Level.Warning,
                        $"some err in get val: {ex}",
                        TableName
                    ));
                    return null;
                }
            }
        }

        public async Task<int?> Get_rID_byColumnVal<T>(string columnName, T columnVal, SqliteConnection? sqc = null)
        {
            if (!_columnNames.Contains(columnName))
            {
                DebugTool.Log(new DebugTool.log(
                        DebugTool.log.Level.Warning,
                        $"column name is so strange! >__<",
                        TableName
                    ));
                return null;
            }

            if (sqc == null)
            {
                using (SqliteConnection connection = new($"Data Source={DatabasePath}"))
                {
                    await connection.OpenAsync();
                    try
                    {
                        var cmd = connection.CreateCommand();
                        cmd.CommandText = $"SELECT rID FROM {TableName} WHERE \"{columnName}\"=@columnVal ";

                        cmd.Parameters.AddWithValue("@columnVal", columnVal);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                object value = reader[0];

                                if (value == DBNull.Value)
                                    return default;

                                return (int)value;
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugTool.Log(new DebugTool.log(
                            DebugTool.log.Level.Warning,
                            $"some err in get rID by val: {ex}",
                            TableName
                        ));
                        return null;
                    }
                }
            }
            else
            {
                try
                {
                    var cmd = sqc.CreateCommand();
                    cmd.CommandText = $"SELECT rID FROM {TableName} WHERE \"{columnName}\"=@columnVal ";

                    cmd.Parameters.AddWithValue("@columnVal", columnVal);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            object value = reader[0];

                            if (value == DBNull.Value)
                                return default;

                            return (int)value;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugTool.Log(new DebugTool.log(
                        DebugTool.log.Level.Warning,
                        $"some err in get rID by val: {ex}",
                        TableName
                    ));
                    return null;
                }
            }
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
    }

    public class Column
    {
        public Column(string name, Column.Types type, int? lenght = null)
        {
            Name = name;
            Type = type == Types.VARCHAR && lenght == null ? Types.TEXT : type;
            Lenght = lenght;
        }
        public readonly string Name;
        public readonly Types Type;
        public readonly int? Lenght;

        public enum Types
        {
            TEXT,
            INTEGER,
            FLOAT,
            VARCHAR,
            DATE,
            TIME,
            DATETIME,
        }
    }
}
