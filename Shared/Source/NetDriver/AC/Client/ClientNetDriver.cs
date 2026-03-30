using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Source.NetDriver.AC.Client
{
    public class ClientNetDriver : INetdriverCore
    {
        public readonly Socket socket = new(
            AddressFamily.InterNetwork, 
            SocketType.Stream, 
            ProtocolType.Tcp
        );

        public ClientNetDriver(IPAddress domain, int port, Func<Request, Task> Processor) 
        {
            socket.ConnectAsync(new IPEndPoint(domain, port));
            InitalizeNetDriver();
            _backgroundTasks.Add(ListeningSocket(socket));
            processor = Processor;
        }

        public override void Shutdown()
        {
            socket.Close();
            socket.Dispose();
            base.Shutdown();
        }
    }
}
