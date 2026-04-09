using Shared.Source.tools;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Source.NetDriver.AC
{
    public class Request
    {
        public Message message;
        public Socket socket;

        public Request(Message msg, Socket sock, TaskCompletionSource<Request>? tcs=null)
        {
            message = msg;
            socket = sock;
            rHook = tcs;
        }
        public Request() { }

        public readonly TaskCompletionSource<Request>? rHook;               // зацепим за функцию отправки сообщения

        public void GetAnswer(Request ans)
        {
            if (rHook != null)
            {
                rHook.TrySetResult(ans);
            }
            else
            {
                DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Error, "attempt to reply to a message that is not a request", "RequestLogs.txt"));
            }
        }
    }
}
