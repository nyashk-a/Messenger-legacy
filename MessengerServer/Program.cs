using Microsoft.Data.Sqlite;
using SQLitePCL;
using Shared.Source.NetDriver.AC.Server;

namespace MessengerServer
{
    internal class Program
    {
        public static readonly string PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MessengerDataBase"); // путь к корневой папке базы
        public static readonly string ImageSourcePATH = Path.Combine(PATH, "massiveUserData");                                                             // путь к папке с картинками
        public static readonly string DataBasePATH = Path.Combine(PATH, "userDataStorage.db");
        public static async Task Main(string[] args)
        {
            Batteries.Init();
            //using (SqliteConnection connection = new($"Data Source={DataBasePATH}"))
            //{
            //    await connection.OpenAsync();
            //    var Cmd = connection.CreateCommand();
            //    Cmd.CommandText = $"CREATE TABLE users (name TEXT, time TIME )";

            //    try
            //    {
            //        await Cmd.ExecuteNonQueryAsync();
            //        await connection.CloseAsync();
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex);
            //        await connection.CloseAsync();
            //    }
            //}


            var tbl = new Table("users", DataBasePATH);


            //var tskHndl = new TaskHandler();

            //var Server = new ServerNetDriver(tskHndl.ProcessedTasks, new IPEndPoint(IPAddress.Any, 22222));
        }
    }
}