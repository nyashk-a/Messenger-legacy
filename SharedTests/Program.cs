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
            Console.Write("Chose youre role (server (s) or user (u)): ");
            var ch = Console.ReadLine();

            switch (ch)
            {
                case "s":
                    var serv = new ServerNetworking();
                    Console.ReadKey();
                    serv.Dispose();
                    break;
                case "u":
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.Write("enter youre name: ");
                    string name = Console.ReadLine();

                    Console.Write("chose connect type (local (l) or global (g)):");
                    IPAddress ip = IPAddress.Parse("127.0.0.1");
                    switch (Console.ReadLine())
                    {
                        case "l":
                            ip = IPAddress.Parse("127.0.0.1");
                            break;
                        case "g":
                            Console.WriteLine((await Dns.GetHostAddressesAsync("jabnet.mooo.com"))[0].ToString());
                            ip = (await Dns.GetHostAddressesAsync("jabnet.mooo.com"))[0];
                            break;
                    }
                    var clin = new ClientNetworking(name, ip);


                    ConsoleController.NetworkAcept = clin.SendMsg;
                    ConsoleController.kill = clin.Shutdown;
                    
                    ConsoleController.Run();
                    break;
            }

            Console.BackgroundColor = ConsoleColor.Black;
        }
    }
}