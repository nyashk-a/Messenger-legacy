using Shared.Source.tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Source.NetDriver.AC.Server
{
    public class ServerNetDriver : INetdriverCore
    {
        public readonly Socket socket = new(
            AddressFamily.InterNetwork, 
            SocketType.Stream, 
            ProtocolType.Tcp
        );

        public readonly ConcurrentDictionary<Socket, Task> Users = new();
        private readonly CancellationTokenSource _cts = new();
        public ServerNetDriver(Func<Request, Task> Processor, IPEndPoint endPoint)
        {
            processor = Processor;
            InitalizeNetDriver();


            socket.Bind(endPoint);
            socket.Listen();

            _backgroundTasks.Add(AceptingConnections());
        }

        private async Task AceptingConnections()
        {
            while (true)
            {
                var clientConnection = await socket.AcceptAsync(_cts.Token);
                Users.TryAdd(clientConnection, ListeningSocket(clientConnection));
            }
        }

        public override void Shutdown()
        {
            try
            {
                _cts.Cancel();
                foreach (var s in Users)
                {
                    s.Key.Close();
                    s.Key.Dispose();
                }
                socket.Close();
                socket.Dispose();
                _cts.Dispose();
                base.Shutdown();
            }
            catch (Exception e)
            {
                DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Error, e.Message, LOGFOLDER));
            }
        }
    }
}
