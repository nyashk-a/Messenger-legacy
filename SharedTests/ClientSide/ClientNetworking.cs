using AVcontrol;
using Shared.Source.NetDriver.AC;
using Shared.Source.NetDriver.AC.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace SharedTests.ClientSide
{
    internal class ClientNetworking
    {
        private readonly ClientNetDriver clin;
        public readonly string Name;

        public ClientNetworking(string name, IPAddress IP)
        {
            Name = name;
            clin = new(IP, 22222, Proc);
            StoryInit().Wait();
        }
        private async Task StoryInit()
        {
            var story = await clin.SendReqMessageAsync(clin.socket, new Message(null, ToBinary.Utf16("000getStory")));
            if (story != null)
            {
                var a = FromBinary.Utf16(story.content);
                var b = a.Split("~:~");
                foreach (var c in b)
                {
                    ConsoleController.AddIncomingMessage(c);
                }
            }
        }
        public void SendMsg(string text)
        {
            clin.SendAnsMessageAsync(clin.socket, new Message(null, ToBinary.Utf16(Name + ": " + text)));

        }
        public void Shutdown()
        {
            clin.SendAnsMessageAsync(clin.socket, new Message(null, ToBinary.Utf16("000kill")));
            clin.Shutdown();
        }
        public async Task Proc(Request req)
        {
            ConsoleController.AddIncomingMessage(FromBinary.Utf16(req.message.content));
        }
    }
}
