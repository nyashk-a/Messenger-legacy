using Shared.Source.NetDriver.AC.Server;
using System.Net;
using System.Reflection.Emit;
namespace MessengerServer
{
    internal class Program
    {
        //public static readonly string PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MessengerDataBase"); // путь к корневой папке базы
        //public static readonly string ImageSourcePATH = Path.Combine(PATH, "massiveUserData");                                                             // путь к папке с картинками
        //public static readonly string DataBasePATH = Path.Combine(PATH, "userDataStorage.db");


        
        /*  Per user store data [IN RAM]:
         *           getReKey(IBinaryKey)            ключ шифрования для получения данных от клиента
         *          sendReKey(IBinaryKey)            ключ шифрования для отправки данных клиенту
         */



        public static async Task Main(string[] args)
        {
            var tskHndl = new TaskHandler();

            var Server = new ServerNetDriver(tskHndl.ProcessedTasks, new IPEndPoint(IPAddress.Any, 22222));
        }
    }
}