using AVcontrol;
using Shared.Source.NetDriver.AC;
using Shared.Source.NetDriver.AC.Client;
using Shared.Source.NetDriver.AC.Server;
using System.Net;

namespace JabServer //                          DEMO
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var a = new Server();
            var b = new Client();


            //await b.clin.SendMassiveMesage(b.clin.socket, "C:\\Users\\suzi\\Downloads\\Telegram Desktop.zip");
            await b.clin.SendMassiveMesage(b.clin.socket, "C:\\Users\\suzi\\Documents\\projects\\csP\\Messenger\\SharedTests\\IMG_4003.PNG");
            //                                                      EXAMPLE to simple send
            //while (true)
            //{
            //Console.Write("client message >");
            //string inp = Console.ReadLine();
            //if (inp == "/q") break;

            //Message answer = await b.clin.SendReqMessageAsync(b.clin.socket, new Message(Guid.NewGuid(), ToBinary.Utf16(inp)));
            //if (answer != null)
            //    Console.WriteLine($"server answer: {FromBinary.Utf16(answer.content)}");
            //}
            a.Dispose();
            b.Dispose();
        }
    }



   
    internal class Server
    {
        private readonly ServerNetDriver serv;
        public Server()
        {
            serv = new ServerNetDriver(Proc, new IPEndPoint(IPAddress.Any, 22222));
        }
        public async Task Proc(Request req)
        {
            //                                                      EXAMPLE to simple send

            //Console.WriteLine($"client doble message: {FromBinary.Utf16(req.message.content)}");
            //Console.Write("server answer >");
            //string answer = Console.ReadLine();
            //serv.SendAnsMessageAsync(req.socket, new Message(req.message.msgsuid, ToBinary.Utf16(answer)));


            //                                  EXAMPLE how to do
            //switch (req.message.content[0])
            //{
            //    case 0:
            //        Console.WriteLine($"client single message: {FromBinary.Utf16(req.message.content)}");
            //        break;
            //    case 1:
            //        Console.WriteLine($"client doble message: {FromBinary.Utf16(req.message.content)}");
            //        Console.Write("server answer >");
            //        string answer = Console.ReadLine();
            //        serv.SendAnsMessageAsync(req.socket, new Message(req.message.msgsuid, ToBinary.Utf16(answer)));
            //        break;
            //}
        }
        public void Dispose()
        {
            serv.Shutdown();
        }
    }



    internal class Client
    {
        public readonly ClientNetDriver clin;

        public Client()
        {
            clin = new ClientNetDriver(IPAddress.Parse("127.0.0.1"), 22222, Proc);
        }
        public async Task Proc(Request req)
        {
            //                                                      EXAMPLE to simple send
            //Console.Write("message by server: ");
            //Console.WriteLine(FromBinary.Utf16(req.message.content));
        }
        public void Dispose()
        {
            clin.Shutdown();
        }
    }
}