using AVcontrol;
using Shared.Source.tools;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace Shared.Source.NetDriver.AC
{
    public class Message
    {
        public readonly Guid msgsuid;
        public readonly byte[] content;
        public readonly int serialNumber;
        public int size 
        { 
            get 
            {
                // [длинна контента][длинна сюида][порядковый номер (если пакет один, то -1)][контент][сюид]
                return 4 + 4 + 4 + msgsuid.ToByteArray().Length + content.Length;
            }
        }
        public byte[] pack
        {
            get
            {
                byte[] p = new byte[size];

                Buffer.BlockCopy(ToBinary.LittleEndian(content.Length), 0, p, 0, 4);                    // запись длинны контента
                Buffer.BlockCopy(ToBinary.LittleEndian(msgsuid.ToByteArray().Length), 0, p, 4, 4);      // запись длинны айдишнника
                Buffer.BlockCopy(ToBinary.LittleEndian(serialNumber), 0, p, 8, 4);
                Buffer.BlockCopy(content, 0, p, 4 + 4 + 4, content.Length);
                Buffer.BlockCopy(msgsuid.ToByteArray(), 0, p, 4 + 4 + 4 + content.Length, msgsuid.ToByteArray().Length);

                return p;
            }
        }


        public Message(Guid? suid, byte[] content, int serialNumber=-1)
        {
            if (content.Length > int.MaxValue - 8)
                throw new Exception($"content size ({content.Length}) bigger then limit ({int.MaxValue - 8})");

            msgsuid = suid.HasValue ? suid.Value : Guid.NewGuid();
            this.content = content;
            this.serialNumber = serialNumber;
        }
        public Message(byte[] pack)
        {
            if (pack.Length < 8)
            {
                DebugTool.Log(new DebugTool.log(DebugTool.log.Level.Error, "pack too short", "MessageLogs.txt"));
            }
            int contentSize = FromBinary.LittleEndian<int>(pack.AsSpan(0, 4).ToArray());
            int idSize = FromBinary.LittleEndian<int>(pack.AsSpan(4, 4).ToArray());
            serialNumber = FromBinary.LittleEndian<int>(pack.AsSpan(8, 4).ToArray());
            var idBuffer = new byte[idSize];
            Buffer.BlockCopy(pack, 4 + 4 + 4 + contentSize, idBuffer, 0, idSize);
            msgsuid = new Guid(idBuffer);
            content = new byte[contentSize];
            Buffer.BlockCopy(pack, 4 + 4 + 4, content, 0, contentSize);
        }
        public static sizeConf PartialParse(byte[] pack)
        {
            var sc = new sizeConf();
            sc.contentSize = FromBinary.LittleEndian<int>(pack.AsSpan(0, 4).ToArray());
            sc.idSize = FromBinary.LittleEndian<int>(pack.AsSpan(4, 4).ToArray());
            sc.serialNumb = FromBinary.LittleEndian<int>(pack.AsSpan(8, 4).ToArray());

            return sc;
        }
        public struct sizeConf
        {
            public int idSize;
            public int contentSize;
            public int serialNumb;
            public int size
            {
                get
                {
                    return idSize + contentSize;
                }
            }
        }
    }
}
