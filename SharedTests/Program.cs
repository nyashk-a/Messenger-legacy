using AVcontrol;
using Shared.Source.NetDriver.AC;
using Shared.Source.NetDriver.AC.Client;
using Shared.Source.NetDriver.AC.Server;
using SharedTests.ClientSide;
using SharedTests.ServerSide;
using System.Net;
using System.Net.Sockets;

namespace SharedTests //                          DEMO
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            Console.Write("Chose youre role (server (s) jr user (u)): ");
            var ch = Console.ReadLine();

            switch (ch)
            {
                case "s":
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    var serv = new ServerNetworking();
                    Console.ReadKey();
                    serv.Dispose();
                    break;
                case "u":
                    Console.Write("enter youre name: ");
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    var clin = new ClientNetworking(Console.ReadLine());

                    ConsoleController.NetworkAcept = clin.SendMsg;
                    ConsoleController.kill = clin.Shutdown;
                    
                    ConsoleController.Run();
                    break;
            }
        }
    }
}