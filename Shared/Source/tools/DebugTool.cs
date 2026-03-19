using Microsoft.Maui.Controls;
using Shared.Source.NetDriver.AC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Shared.Source.tools
{
    public static class DebugTool
    {
        private static readonly string path = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly Channel<log> logs = Channel.CreateUnbounded<log>();
        private static readonly ConcurrentDictionary<string, StreamWriter> writerList = new();
        private static Task? exec;

        public static void StartDebugTool()
        {
            exec = Exec();
        }

        public static void Log(log l)
        {
            logs.Writer.TryWrite(l);
        }

        private static async Task Exec()
        {
            
            var reader = logs.Reader;
            await foreach (var req in reader.ReadAllAsync())
            {
                var writer = writerList.GetOrAdd(req.d, _ =>
                {
                    var stream = new FileStream(
                        Path.Combine(path, req.d),
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.Read,
                        4096,
                        true);
                    return new StreamWriter(stream, Encoding.UTF8);
                });

                await writer.WriteLineAsync($"{req.l}: {req.m}");
            }
        }


        public static async Task Shutdown()
        {
            logs.Writer.TryComplete();
            foreach (var a in writerList)
            {
                await a.Value.DisposeAsync();
            }

            if (exec != null) await exec;
        }

        public struct log(log.Level level, string message, string destination)
        {
            public enum Level
            {
                Info,
                Error,
                Warning,
            }
            public readonly string d = destination;
            public readonly Level l = level;
            public readonly string m = message;
        }
    }
}
