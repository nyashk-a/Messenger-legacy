using Microsoft.Data.Sqlite;
using static JabNet.USC;

namespace MessengerServer
{
    internal static class SQLiteHandler
    {
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

        public struct Table(string tableName, Column[] columnsList)
        {
            public readonly string name = tableName;
            public readonly Column[] columns = columnsList;
        }

        public static readonly string PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MessengerDataBase"); // путь к корневой папке базы
        public static readonly string ImageSourcePATH = Path.Combine(PATH, "massiveUserData");                                                             // путь к папке с картинками
        public static readonly string DataBasePATH = Path.Combine(PATH, "userDataStorage.db");                                                             // путь к бд

        private static readonly List<Table> _tableList = new();
        public static List<Table> TableList { get => _tableList.Where(n => !n.name.StartsWith("sqlite_")).ToList(); }

        public static async Task InitAsync()
        {
            Directory.CreateDirectory(PATH);
            Directory.CreateDirectory(ImageSourcePATH);

            using (SqliteConnection connection = new($"Data Source={DataBasePATH}"))
            {
                await connection.OpenAsync();
                try
                {
                    var cmdTables = connection.CreateCommand();
                    cmdTables.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
                    using var readerTables = await cmdTables.ExecuteReaderAsync();

                    while (await readerTables.ReadAsync())
                    {
                        string tableName = readerTables.GetString(0);

                        if (tableName.StartsWith("sqlite_"))
                            continue;

                        var cmdPragma = connection.CreateCommand();
                        cmdPragma.CommandText = $"PRAGMA table_info(\"{tableName}\");";
                        using var readerColumns = await cmdPragma.ExecuteReaderAsync();

                        var columns = new List<Column>();

                        while (await readerColumns.ReadAsync())
                        {
                            if (readerColumns.GetInt32(readerColumns.GetOrdinal("pk")) == 1)
                                continue;

                            string colName = readerColumns.GetString(readerColumns.GetOrdinal("name"));
                            string colType = readerColumns.GetString(readerColumns.GetOrdinal("type"));

                            var column = ParseColumnType(colName, colType);
                            columns.Add(column);
                        }

                        _tableList.Add(new Table(tableName, columns.ToArray()));
                    }
                }
                finally
                {
                    await connection.CloseAsync();
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

        public static async Task<bool> RemoveTableAsync(string name)
        {
            using (SqliteConnection connection = new($"Data Source={DataBasePATH}"))
            {
                await connection.OpenAsync();
                var Cmd = connection.CreateCommand();
                Cmd.CommandText = $"DROP TABLE {name}";
                try
                {
                    await Cmd.ExecuteNonQueryAsync();
                    _tableList.RemoveAll(t => t.name == name);
                    await connection.CloseAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await connection.CloseAsync();
                    return false;
                }
            }
        }

        public static async Task<bool> CreateTableAsync(string tableName, Column[] columns)
        {
            using (SqliteConnection connection = new($"Data Source={DataBasePATH}"))
            {
                await connection.OpenAsync();
                if (columns.Length == 0) return false;

                string args = "ID INTEGER PRIMARY KEY AUTOINCREMENT, ";
                foreach (var arg in columns)
                {
                    if (arg.Type != Column.Types.VARCHAR)
                    {
                        args += $"{arg.Name} {arg.Type.ToString()}, ";
                    }
                    else
                    {
                        args += $"{arg.Name} {arg.Type.ToString()}({arg.Lenght}), ";
                    }
                }
                args = args[..^2];

                var Cmd = connection.CreateCommand();
                Cmd.CommandText = $"CREATE TABLE {tableName} ({args})";

                try
                {
                    await Cmd.ExecuteNonQueryAsync();
                    _tableList.Add(new Table(tableName, columns));
                    await connection.CloseAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await connection.CloseAsync();
                    return false;
                }
            }
        }

        public static async Task<bool> RewriteValueAsync<T>(string tableName, string column, T val, UInt32 stringID)
        {
            using (SqliteConnection connection = new($"Data Source={DataBasePATH}"))
            {
                await connection.OpenAsync();
                var cmd = connection.CreateCommand();
                cmd.CommandText = $"UPDATE {tableName} SET {column} = @value WHERE ID = @id";
                cmd.Parameters.AddWithValue("@value", val);
                cmd.Parameters.AddWithValue("@id", stringID);

                try
                {
                    await cmd.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await connection.CloseAsync();
                    return false;
                }
            }
        }

        public static async Task<UInt32?> InsertRowAsync(string tableName)
        {
            using (SqliteConnection connection = new($"Data Source={DataBasePATH}"))
            {
                await connection.OpenAsync();
                var cmd = connection.CreateCommand();
                cmd.CommandText = $"INSERT INTO {tableName} DEFAULT VALUES; SELECT last_insert_rowid();";
                var newId = await cmd.ExecuteScalarAsync();
                await connection.CloseAsync();
                return newId != null ? Convert.ToUInt32(newId) : (UInt32?)null;
            }
        }

        public static async Task RemoveRowAsync(string tableName, UInt32 id)
        {
            using (SqliteConnection connection = new($"Data Source={DataBasePATH}"))
            {
                await connection.OpenAsync();
                var cmd = connection.CreateCommand();
                cmd.CommandText = $"DELETE FROM {tableName} WHERE id = {id};";
                await cmd.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
        }

        public static async Task<uint?> SearchIdByValueAsync<T>(string tableName, string columnName, T value)
        {
            using (SqliteConnection connection = new($"Data Source={DataBasePATH}"))
            {
                await connection.OpenAsync();
                var cmd = connection.CreateCommand();
                cmd.CommandText = $"SELECT ID FROM {tableName} WHERE {columnName} = @value";
                cmd.Parameters.AddWithValue("@value", value);
                var result = await cmd.ExecuteScalarAsync();
                await connection.CloseAsync();
                return result != null ? Convert.ToUInt32(result) : (uint?)null;
            }
        }

        public static async Task<byte[]> ReadMassiveObjData(string pathToData)
        {
            return await File.ReadAllBytesAsync(pathToData);
        }

        public static async Task<bool> WriteMassiveObjData(string pathToData, byte[] data)
        {
            try
            {
                await File.WriteAllBytesAsync(pathToData, data);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static async Task DebugPrintAsync()
        {
            using (SqliteConnection connection = new($"Data Source={DataBasePATH}"))
            {
                await connection.OpenAsync();
                var cmdTables = connection.CreateCommand();
                cmdTables.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";

                using (var readerTables = await cmdTables.ExecuteReaderAsync())
                {
                    while (await readerTables.ReadAsync())
                    {
                        string tableName = readerTables.GetString(0);
                        Console.WriteLine($"\n=== Таблица: {tableName} ===");

                        var cmdPragma = connection.CreateCommand();
                        cmdPragma.CommandText = $"PRAGMA table_info(\"{tableName}\");";

                        using (var readerColumns = await cmdPragma.ExecuteReaderAsync())
                        {
                            Console.WriteLine("Колонки:");
                            while (await readerColumns.ReadAsync())
                            {
                                string columnName = readerColumns.GetString(readerColumns.GetOrdinal("name"));
                                string columnType = readerColumns.GetString(readerColumns.GetOrdinal("type"));
                                Console.WriteLine($"  - {columnName} ({columnType})");
                            }
                        }
                        var cmdData = connection.CreateCommand();
                        cmdData.CommandText = $"SELECT * FROM \"{tableName}\" LIMIT 50;";

                        using (var readerData = await cmdData.ExecuteReaderAsync())
                        {
                            Console.WriteLine("Содержимое (первые 50 строк):");
                            for (int i = 0; i < readerData.FieldCount; i++)
                            {
                                Console.Write(readerData.GetName(i) + "\t");
                            }
                            Console.WriteLine();

                            while (await readerData.ReadAsync())
                            {
                                for (int i = 0; i < readerData.FieldCount; i++)
                                {
                                    Console.Write(readerData.GetValue(i)?.ToString() + "\t");
                                }
                                Console.WriteLine();
                            }
                        }
                    }
                }
                await connection.CloseAsync();
            }
        }
    }
}