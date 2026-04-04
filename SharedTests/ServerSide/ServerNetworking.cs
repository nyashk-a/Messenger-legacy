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
        //private readonly List<string> globalStory = new();
        private Table? globalStoryTable;

        public ServerNetworking()
        {
            Console.WriteLine("Initalizing NetDriver");
            srvr = new ServerNetDriver(Proc, new IPEndPoint(IPAddress.Any, 22222));
            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            await TableController.InitalizeDatabaseController();
            if (TableController.GetTable("chatStory") == null)
            {
                Console.WriteLine("create new table");
                await TableController.CreateTable("chatStory", new Column[]
                {
                    new Column("content", Column.Types.TEXT)
                });
            }
            globalStoryTable = TableController.GetTable("chatStory");
            Console.WriteLine("ready to work");
        }

        public void Dispose()
        {
            srvr.Shutdown();
        }

        public async Task Proc(Request req)
        {
            string text = FromBinary.Utf16(req.message.content);
            Console.WriteLine(text);


            switch (text)
            {
                case "000getStory":
                    string ans = "";

                    string sql = "SELECT content FROM chatStory";
                    List<Dictionary<string, object>> allRows = await TableController.ExecuteQueryAsync(sql);

                    Console.WriteLine("command completed");

                    foreach (var row in allRows)
                    {
                        if (row.TryGetValue("content", out object? content) && content != null)
                        {
                            ans += content.ToString();
                            ans += "~:~";
                        }
                    }

                    ans = ans.Length >= 3 ? ans[0..^3] : ans;

                    Console.WriteLine(ans);
                    srvr.SendAnsMessageAsync(req.socket, new Message(req.message.msgsuid, ToBinary.Utf16(ans)));
                    return;

                case "000kill":
                    srvr.Users[req.socket].Dispose();
                    srvr.Users.Remove(req.socket, out _);
                    return;

                default:
                    //globalStory.Add(text);
                    var rowID = await globalStoryTable.InsertRow();
                    await globalStoryTable.SetValue(rowID.Value, "content", text);

                    foreach (var a in srvr.Users.Keys)
                    {
                        srvr.SendAnsMessageAsync(a, new Message(null, ToBinary.Utf16(text)));
                    }
                    return;
            }
        }
    }
}
