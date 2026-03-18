using AVcontrol;
using Shared.Source.tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Source.NetDriver.AC
{
    public abstract partial class INetdriverCore
    {
        public async Task SendMassiveMesage(Socket sock, string pathToFile)
        {
            string fileName = Path.GetFileName(pathToFile);

            FileInfo fileInfo = new FileInfo(pathToFile);
            long fileSize = fileInfo.Length;

            int part = 1024 * 1024 * 32;                                          // размер одного пакета в среднем

            int piceCount = (int)((fileSize + part - 1) / part);

            var configMessage = new Message(null, ToBinary.Utf16(fileName), piceCount);

            Guid mainGuid = configMessage.msgsuid;

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
                        byte[] dataToSend;
                        if (bytesRead == buffer.Length)
                        {
                            dataToSend = buffer;
                        }
                        else
                        {
                            dataToSend = new byte[bytesRead];
                            Array.Copy(buffer, 0, dataToSend, 0, bytesRead);
                        }


                        var msg = new Message(mainGuid, dataToSend, sn);
                        SendAnsMessageAsync(sock, msg);
                        sn++;
                    }
                }
            }
            else
            {
                DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Warning, "the other party is not responding", LOGFOLDER));
            }

        }          
    }
}
