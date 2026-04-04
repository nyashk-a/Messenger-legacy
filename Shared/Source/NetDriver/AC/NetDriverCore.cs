using AVcontrol;
using Shared.Source.tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shared.Source.NetDriver.AC
{
    public abstract partial class INetdriverCore
    {
        protected Func<Request, Task> processor;
        protected readonly ConcurrentDictionary<Guid, Request> _pendingRequests = new();
        protected readonly ConcurrentDictionary<Guid, MassiveContentBuilder> _contentBuilder = new();
        protected readonly Channel<Request> _dispatchChannel = Channel.CreateUnbounded<Request>();
        protected readonly Channel<Request> _incomingChannel = Channel.CreateUnbounded<Request>();
        protected readonly ConcurrentBag<Task> _backgroundTasks = new();
        private readonly CancellationTokenSource _cts = new();                                      // глобальный под шутдаун
        private Task _cleaningTask;
        private volatile bool endWorking = false;

        public readonly string LOGFOLDER = "logs.txt";


        protected void InitalizeNetDriver()
        {
            Console.WriteLine("Внимение!\nВам не следует принудительно закрывать консоль!\nпри принудительном закрытии нарушится логика завершения процессов!");
            _cleaningTask = RemoveCompletedTasksAsync(_cts.Token);
            try
            {
                DebugTool.StartDebugTool();
                _backgroundTasks.Add(DispatchQueueController(_cts.Token));
                _backgroundTasks.Add(IncomingQueueController(_cts.Token));
                _backgroundTasks.Add(DisposeBuilderController(_cts.Token));
            }
            catch (Exception ex)
            {
                DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Error, ex.Message, LOGFOLDER));
            }
        }
        public virtual void Shutdown()
        {
            Console.WriteLine("\nЗапущен процесс закрытия.\nдождитесь его окончания.");
            endWorking = true;
            try
            {
                _cts.Cancel();

                Task.WhenAll(_backgroundTasks).Wait(TimeSpan.FromSeconds(10));

                _dispatchChannel.Writer.TryComplete();
                _incomingChannel.Writer.TryComplete();

                foreach (var builder in _contentBuilder.Values)
                {
                    builder.Dispose();
                }
                _contentBuilder.Clear();

                foreach (var req in _pendingRequests.Values)
                {
                    req.rHook?.TrySetCanceled();
                }
                _pendingRequests.Clear();

                DebugTool.Shutdown().Wait(TimeSpan.FromSeconds(2));
                _cts.Dispose();
                _cleaningTask.Dispose();
            }
            catch (Exception e)
            {
                DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Warning, e.Message, LOGFOLDER));
            }
        }

        protected async Task<Exception?> ListeningSocket(Socket sock, CancellationToken Token)
        {
            while (!endWorking && !Token.IsCancellationRequested)
            {
                try
                {
                    var lenghtBuffer = new byte[12];
                    int read = 0;
                    while (read < lenghtBuffer.Length)
                    {
                        read += await sock.ReceiveAsync(lenghtBuffer.AsMemory(read, 12 - read));
                    }
                    var sc = Message.PartialParse(lenghtBuffer);

                    if (sc.idSize != 16 || sc.contentSize > int.MaxValue)
                    {
                        continue;
                    }


                    var mainBuffer = new byte[sc.size + 4 + 4 + 4];
                    Buffer.BlockCopy(lenghtBuffer, 0, mainBuffer, 0, lenghtBuffer.Length);


                    read = 0;
                    while (read < sc.size)
                    {
                        read += await sock.ReceiveAsync(mainBuffer.AsMemory(12 + read, sc.size - read), Token);
                    }

                    var rq = new Request(new Message(mainBuffer), sock);


                    if (rq.message.serialNumber != -1)
                    {
                        await IncomingMassiveContentHandler(rq, sock);
                        continue;
                    }

                    if (_pendingRequests.TryGetValue(rq.message.msgsuid, out var rqOut))
                    {
                        rqOut.GetAnswer(rq);
                        _pendingRequests.TryRemove(rq.message.msgsuid, out _);
                        continue;
                    }

                    _incomingChannel.Writer.TryWrite(rq);
                }
                catch (Exception ex)
                {
                    DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Error, ex.Message, LOGFOLDER));
                }
            }
            
            return null;
        }


        public async Task<Message?> SendReqMessageAsync(Socket sock, Message msg)                   // ожидаем ответ
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var tcs = new TaskCompletionSource<Request>();
                var rq = new Request(msg, sock, tcs);
                if (!_pendingRequests.TryAdd(rq.message.msgsuid, rq))
                {
                    DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Error, "SendReqMessageAsync: can`t add message to dict", LOGFOLDER));
                }

                _dispatchChannel.Writer.TryWrite(rq);


                using (cts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));
                    if (completedTask == tcs.Task)
                    {
                        _pendingRequests.TryRemove(rq.message.msgsuid, out _);
                        return (await tcs.Task).message;
                    }
                    else
                    {
                        _pendingRequests.TryRemove(rq.message.msgsuid, out _);
                        DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Warning, "Response timeout", LOGFOLDER));
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Error, e.Message, LOGFOLDER));
                return null;
            }
        }

        public void SendAnsMessageAsync(Socket sock, Message msg)                                   // не ожидаем ответа
        {
            var rq = new Request(msg, sock);

            _dispatchChannel.Writer.TryWrite(rq);
        }

        private async Task DispatchQueueController(CancellationToken cancellationToken = default)
        {
            var reader = _dispatchChannel.Reader;

            await foreach (var req in reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await req.socket.SendAsync(req.message.pack, cancellationToken);
                }
                catch (Exception ex)
                {
                    DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Error, ex.Message, LOGFOLDER));
                }
            }
        }

        private async Task IncomingQueueController(CancellationToken cancellationToken = default)
        {
            var reader = _incomingChannel.Reader;

            await foreach (var req in reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    if (processor == null) continue;                                                // процссор должен сам ответить на запрос, если то требуется.
                    _backgroundTasks.Add(processor(req));
                }
                catch (Exception ex)
                {
                    DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Error, ex.Message, LOGFOLDER));
                }
            }
        }

        private async Task IncomingMassiveContentHandler(Request rq, Socket sock)
        {
            try
            {
                Guid mainGuid = new Guid(rq.message.content.AsSpan(0, 16));
                if (_contentBuilder.TryGetValue(mainGuid, out var pkgBuilder))
                {
                    await pkgBuilder.WritePackage(rq.message);
                    SendAnsMessageAsync(sock, new Message(rq.message.msgsuid, ToBinary.Utf16("11")));
                }
                else
                {
                    if (_contentBuilder.TryAdd(mainGuid, new MassiveContentBuilder(
                            ReportClosure,
                            mainGuid,
                            rq.message.serialNumber,
                            FromBinary.Utf16(rq.message.content.AsSpan(16).ToArray()
                        )
                    )))
                    {
                        SendAnsMessageAsync(sock, new Message(rq.message.msgsuid, ToBinary.Utf16("ready")));
                    }
                    else
                    {
                        DebugTool.Log(new DebugTool.log(
                            DebugTool.log.Level.Error, 
                            "ListeningSocket: can`t add message to dict", 
                            LOGFOLDER));
                        SendAnsMessageAsync(sock, new Message(rq.message.msgsuid, ToBinary.Utf16("broke")));
                    }
                }
            }
            catch (Exception e)
            {
                SendAnsMessageAsync(sock, new Message(rq.message.msgsuid, ToBinary.Utf16(e.Message)));
                DebugTool.Log(new DebugTool.log(
                            DebugTool.log.Level.Error,
                            e.Message,
                            LOGFOLDER));
            }
        }

        public async Task RemoveCompletedTasksAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var tasks = _backgroundTasks.ToArray();

                if (tasks.Length > 0)
                {
                    var completedTask = await Task.WhenAny(tasks);

                    var temp = new List<Task>();
                    while (_backgroundTasks.TryTake(out var task))
                    {
                        if (task != completedTask)
                            temp.Add(task);
                    }
                    foreach (var task in temp)
                        _backgroundTasks.Add(task);
                }
                else
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
        }
    }
}
