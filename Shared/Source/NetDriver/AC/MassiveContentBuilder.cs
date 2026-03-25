using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Shared.Source.NetDriver.AC
{
    public class MassiveContentBuilder : IDisposable
    {
        private static readonly string swapDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads");
        private readonly ConcurrentDictionary<int, Message> _hash = new();
        private readonly Channel<Message> _queueToWrite;
        private readonly int expectedQuantity;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _bgTask;
        private bool _disposed = false;
        private readonly FileStream _filewriter;
        private volatile int actualNumder = 0;
        private readonly Action<MassiveContentBuilder> _disposeSelf;

        public readonly string pathToFolder;
        public readonly Guid FileGuid;



        public MassiveContentBuilder(Action<MassiveContentBuilder> disposeSelf, Guid fileGuid, int expectedQuantity, string fileName)
        {
            _disposeSelf = disposeSelf;

            this.expectedQuantity = expectedQuantity;
            FileGuid = fileGuid;
            pathToFolder = Path.Combine(swapDir, fileName);
            _queueToWrite = Channel.CreateBounded<Message>(expectedQuantity);

            _bgTask = WritingContent(_cts.Token);

            Directory.CreateDirectory(swapDir);
            _filewriter = File.Create(pathToFolder);
        }
        public async Task WritePackage(Message msg)
        {
            if (_disposed) return;

            int current = Interlocked.CompareExchange(ref actualNumder, 0, 0);
            if (msg.serialNumber >= current)
            {
                _queueToWrite.Writer.TryWrite(msg);
            }
            _hash.TryAdd(msg.serialNumber, msg);
        }

        private async Task WritingContent(CancellationToken token)
        {
            var reader = _queueToWrite.Reader;
            try
            {
                await foreach (var msg in reader.ReadAllAsync(token))
                {
                    if (msg.serialNumber == actualNumder)
                    {
                        byte[] cnt = new byte[msg.content.Length - 16];
                        Array.Copy(msg.content, 16, cnt, 0, cnt.Length);
                        await _filewriter.WriteAsync(cnt);
                        _hash.TryRemove(actualNumder, out _);
                        actualNumder = actualNumder + 1;
                    }

                    while (_hash.TryGetValue(actualNumder, out var nMsg))
                    {
                        byte[] cnt = new byte[nMsg.content.Length - 16];
                        Array.Copy(nMsg.content, 16, cnt, 0, cnt.Length);
                        await _filewriter.WriteAsync(cnt);
                        _hash.TryRemove(actualNumder, out _);
                        actualNumder = actualNumder + 1;
                    }

                    if (actualNumder == expectedQuantity)
                    {
                        _queueToWrite.Writer.Complete();
                    }
                }
            }
            finally
            {
                _disposeSelf(this);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _cts.Cancel();
            _bgTask.Wait(TimeSpan.FromSeconds(5));
            _queueToWrite.Writer.TryComplete();
            _filewriter?.Dispose();
            _cts.Dispose();
        }
    }
}
