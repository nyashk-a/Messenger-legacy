using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Text;
using AVcontrol;
using Shared.Source.NetDriver.AC;
using Shared.Source.NetDriver.AC.Server;

namespace SharedTests.ServerSide
{
    internal class ServerNetworking : IDisposable
    {
        private readonly ServerNetDriver srvr;
        private readonly List<string> chatStory = new();

        public ServerNetworking()
        {
            srvr = new ServerNetDriver(Proc, new IPEndPoint(IPAddress.Any, 22222));
        }

        public void Dispose()
        {
            srvr.Shutdown();
        }

        public async Task Proc(Request req)
        {
            string text = FromBinary.Utf16(req.message.content);
            switch (text)
            {
                case "000getStory":
                    string ans = "";
                    foreach (var a in chatStory)
                    {
                        ans += a;
                        ans += Program.SEPARATOR;
                    }

                    ans = ans.Length >= 3 ? ans[0..^Program.SEPARATOR.Length] : ans;

                    Console.WriteLine(ans);

                    srvr.SendAnsMessageAsync(req.socket, new Message(req.message.msgsuid, ToBinary.Utf16(ans)));
                    return;

                case "000kill":
                    srvr.Users[req.socket].Cts.Cancel();
                    srvr.Users.Remove(req.socket, out _);
                    return;

                default:
                    chatStory.Add(text);
                    foreach (var a in srvr.Users.Keys)
                    {
                        srvr.SendAnsMessageAsync(a, new Message(null, ToBinary.Utf16(text)));
                    }
                    return;
            }
        }
    }
}
