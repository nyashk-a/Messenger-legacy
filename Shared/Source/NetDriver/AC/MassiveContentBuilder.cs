using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly Task bgTask;
        private readonly FileStream _filewriter;
        private int actualNumder = 0;

        public readonly string pathToFolder;
        public readonly Guid FileGuid;
        public bool IsCompleted { get => bgTask.IsCompleted; }



        public MassiveContentBuilder(Guid fileGuid, int expectedQuantity, string fileName)
        {
            this.expectedQuantity = expectedQuantity;
            FileGuid = fileGuid;
            pathToFolder = Path.Combine(swapDir, fileName);
            _queueToWrite = Channel.CreateBounded<Message>(expectedQuantity);

            bgTask = WritingContent();

            Directory.CreateDirectory(swapDir);
            _filewriter = File.Create(pathToFolder);
        }
        public async Task WritePackage(Message msg)
        {
            if (msg.serialNumber == actualNumder)
            {
                _queueToWrite.Writer.TryWrite(msg);
            }
            else
            {
                _hash.TryAdd(msg.serialNumber, msg);
            }
        }

        private async Task WritingContent()
        {
            var reader = _queueToWrite.Reader;

            await foreach (var msg in reader.ReadAllAsync())
            {
                await _filewriter.WriteAsync(msg.content);
                actualNumder = msg.serialNumber + 1;

                while (_hash.TryGetValue(actualNumder, out var nMsg))
                {
                    await _filewriter.WriteAsync(nMsg.content);
                    _hash.TryRemove(actualNumder, out _);
                    actualNumder = nMsg.serialNumber + 1;
                }

                if (actualNumder == expectedQuantity)
                {
                    _queueToWrite.Writer.Complete();
                }
            }
        }

        public void Dispose()
        {
            _queueToWrite.Writer.TryComplete();
            bgTask.Wait();
            _filewriter?.Close();
            _filewriter?.Dispose();
        }
    }
}
