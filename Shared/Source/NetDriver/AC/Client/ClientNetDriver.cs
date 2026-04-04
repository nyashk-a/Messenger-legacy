using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
        public readonly CancellationTokenSource Cts = new();

        public ClientNetDriver(IPAddress IP, int port, Func<Request, Task> Processor)
        {
            socket.Connect(new IPEndPoint(IP, port));
            _backgroundTasks.Add(ListeningSocket(socket, Cts.Token));
            InitalizeNetDriver();
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
