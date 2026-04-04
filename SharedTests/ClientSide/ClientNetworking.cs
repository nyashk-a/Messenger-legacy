using AVcontrol;
using JabrAPI;
using Shared.Source.NetDriver.AC;
using Shared.Source.NetDriver.AC.Client;
using System;
using System.Collections.Generic;
using System.Net;
using static System.Net.Mime.MediaTypeNames;


namespace SharedTests.ClientSide
{
    internal class ClientNetworking
    {
        private ClientNetDriver clin;
        public readonly string Name;
        private readonly JabrAPI.RE5.EncryptionKey reKey = new("8:Я4YZ`}1316:8шщ26ы9Юcх7цbъ5a162:иJж&фБcCНiHS@а2qnfГя1ЯтЦэв}ЕЫ(?'ёTgKW0дrOXFk`йeХго*ЬъЖ~<5уv/{,ЩЭG$ьр4ч7ЗДФсР\"ШТ3BbЁQ8нda№YюУП#ЪЮRлЧVыВпЛ:>s^Mек)[И\\UILбyhP _шОщNцСмmt%Z;]Axz|=D6зwАluEjЙК+o!.p-Мх98:нBPwДVЧе1,1,6,2,1,2,5,5,2,0,4,0,2,6,3,5,3,2,6,2,3,4,5,6,3,2,1,1,0,3,2,4,4,3,4,5,6,4,2,1,6,6,2,5,1,5,1,2,0,4,6,3,1,0,5,6,0,6,0,6,2,0,3,6,0,2,2,4,4,1,4,1,1,3,0,3,5,0,2,5,4,4,2,0,2,2,1,6,2,3,4,6,1,2,0,0,3,2,3,4,5,1,0,3,5,3,3,0,4,3,4,3,6,6,1,1,5,0,5,3,6,5,1,2,6,6,4,3,6,0,1,0,5,1,6,4,3,0,1,6,2,5,4,0,0");

        public ClientNetworking(string name, IPAddress IP)
        {
            reKey.Noisifier.settings.OutputLength = 215;
            clin = new(IP, 22222, Proc);
            Name = name;
            StoryInit().Wait();
        }
        private async Task StoryInit()
        {
            var story = await clin.SendReqMessageAsync(clin.socket, new Message(null, ToBinary.Utf16("000getStory")));
            if (story != null)
            {
                var a = FromBinary.Utf16(story.content);
                var b = a.Split(Program.SEPARATOR);
                string content;
                foreach (var c in b)
                {
                    if (c == "") continue;
                    content = JabrAPI.RE5.Decrypt.WithNoise.Text(c, reKey);
                    ConsoleController.AddIncomingMessage(content);
                }
            }
        }
        public void SendMsg(string text)
        {
            string msg = JabrAPI.RE5.Encrypt.WithNoise.Text(Name + ": " + text, reKey, true);
            clin.SendAnsMessageAsync(clin.socket, new Message(null, ToBinary.Utf16(msg)));
        }
        public void Shutdown()
        {
            string msg = JabrAPI.RE5.Encrypt.WithNoise.Text($"--- {Name} вышел ---", reKey, true);
            clin.SendAnsMessageAsync(clin.socket, new Message(null, ToBinary.Utf16(msg)));

            clin.SendAnsMessageAsync(clin.socket, new Message(null, ToBinary.Utf16("000kill")));
            clin.Shutdown();
        }
        public async Task Proc(Request req)
        {
            string content = JabrAPI.RE5.Decrypt.WithNoise.Text(FromBinary.Utf16(req.message.content), reKey, true);
            if (content != "")
                ConsoleController.AddIncomingMessage(content);
        }
    }
}
