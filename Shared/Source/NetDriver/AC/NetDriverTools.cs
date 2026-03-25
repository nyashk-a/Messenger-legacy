using AVcontrol;
using Shared.Source.tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Shared.Source.NetDriver.AC
{
    public abstract partial class INetdriverCore
    {
        private readonly Channel<Guid> _builderDisposeChannel = Channel.CreateUnbounded<Guid>();


        public async Task SendMassiveMesage(Socket sock, string pathToFile, int part = 1024 * 1024 * 32, IProgress<string> progress=null)
        {
            string fileName = Path.GetFileName(pathToFile);
            FileInfo fileInfo = new FileInfo(pathToFile);
            long fileSize = fileInfo.Length;
            int piceCount = (int)((fileSize + part - 1) / part);
            int complitedCount = 0;


            Guid mg = Guid.NewGuid();
            byte[] mainGuid = mg.ToByteArray();

            var dt = ToBinary.Utf16(fileName);

            var confMessageData = new byte[dt.Length + 16];
            Array.Copy(mainGuid, 0, confMessageData, 0, mainGuid.Length);
            Array.Copy(dt, 0, confMessageData, 16, dt.Length);

            var configMessage = new Message(mg, confMessageData, piceCount);
            

            List<Task<Message?>> sendingData = new();

            var firstAns = await SendReqMessageAsync(sock, configMessage);
            if (firstAns != null && FromBinary.Utf16(firstAns.content) == "ready")
            {
                using (FileStream fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[part];
                    int sn = 0;
                    int bytesRead;

                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        byte[] dataToSend = new byte[bytesRead + 16];
                        Array.Copy(mainGuid, 0, dataToSend, 0, mainGuid.Length);
                        Array.Copy(buffer, 0, dataToSend, 16, bytesRead);

                        var msg = new Message(null, dataToSend, sn);
                        sendingData.Add(SendReqMessageAsync(sock, msg));
                        sn++;
                    } 
                }

                while (sendingData.Any())
                {
                    var completedTask = await Task.WhenAny(sendingData);

                    var res = await completedTask;
                    sendingData.Remove(completedTask);

                    if (FromBinary.Utf16(res?.content) == "11")
                    {
                        complitedCount++;

                        progress?.Report($"{(((float)complitedCount / (float)piceCount) * 100.0f):F1}%");
                    }
                    else
                    {
                        DebugTool.Log(new DebugTool.log(
                        DebugTool.log.Level.Warning,
                        $"broken data send with {FromBinary.Utf16(res?.content)}",
                        LOGFOLDER));
                    }
                }
            }
            else
            {
                DebugTool.Log(new DebugTool.log(
                    DebugTool.log.Level.Warning, 
                    "the other party is not responding", 
                    LOGFOLDER));
            }
        }


        private async Task DisposeBuilderController(CancellationToken cancellationToken = default)
        {
            var reader = _builderDisposeChannel.Reader;

            await foreach (var gd in reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    if (_contentBuilder.TryGetValue(gd, out var pkgBuilder))
                    {
                        pkgBuilder.Dispose();
                        _contentBuilder.TryRemove(gd, out _);
                    }
                }
                catch (Exception ex)
                {
                    DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Error, ex.Message, LOGFOLDER));
                }
            }
        }
        private void ReportClosure(MassiveContentBuilder self)            // только для MassiveContentBuilder!
        {
            _builderDisposeChannel.Writer.TryWrite(self.FileGuid);
        }
    }
}
