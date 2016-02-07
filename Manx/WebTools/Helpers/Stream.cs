using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Helpers
{
    public static class StreamHelpers
    {
        private static int RBS = new TcpClient().ReceiveBufferSize;
       private static int SBS = new TcpClient().SendBufferSize;
        private static Encoding Encoder = new UTF8Encoding();
        public static byte[] Read(this Stream stream, int length)
        {
            var bytes = new byte[length];
            while (length > 0)
            {
                var loaded = stream.Read(bytes, bytes.Length - length, Math.Min(RBS, length));
                if (loaded == 0) break;
                length -= loaded;
            }
            return bytes;
        }
        public static byte[] Read(this Stream stream)
        {
            var bytes = new byte[1024];
            stream.Read(bytes,0,1024);
            return bytes;
        }
        public static string ReadText(this Stream stream)
        {
            return Encoder.GetString(stream.Read());
        }     
        public static void Write(this Stream stream, byte[] data)
        {
            var sended = 0;
            var len = data.Length;
            while (sended < len)
            {
                var s = Math.Min(SBS, len - sended);
                stream.Write(data, sended, s);
                sended += s;
            }
        }
        public static void Write(this Stream stream, string message)
        {
            stream.Write(Encoder.GetBytes(message));
        }

        public static ushort ReadInt16(this Stream stream)
        {
            var m_buffer = stream.Read(2);
            if (BitConverter.IsLittleEndian)
                return (ushort)(m_buffer[1] | (m_buffer[0] << 0x08));
            return (ushort)(m_buffer[0] | (m_buffer[1] << 0x08));
        }
        public static void WriteInt16(this Stream stream, ushort value)
        {
            if (BitConverter.IsLittleEndian)
                stream.Write(BitConverter.GetBytes(value).Reverse().ToArray());
            else stream.Write(BitConverter.GetBytes(value));
        }
        public static ulong ReadInt64(this Stream stream)
        {
            var m_buffer = stream.Read(8);
            uint num, num2;

            if (BitConverter.IsLittleEndian)
            {
                num = (uint)(((m_buffer[7] |
                             (m_buffer[6] << 0x08)) |
                             (m_buffer[5] << 0x10)) |
                             (m_buffer[4] << 0x18));
                num2 = (uint)(((m_buffer[3] |
                           (m_buffer[2] << 0x08)) |
                           (m_buffer[1] << 0x10)) |
                           (m_buffer[0] << 0x18));
            }
            else
            {
                num = (uint)(((m_buffer[0] |
                          (m_buffer[1] << 0x08)) |
                          (m_buffer[2] << 0x10)) |
                          (m_buffer[3] << 0x18));
                num2 = (uint)(((m_buffer[4] |
                           (m_buffer[5] << 0x08)) |
                           (m_buffer[6] << 0x10)) |
                           (m_buffer[7] << 0x18));
            }
            return (ulong)((num2 << 0x20) | num);
        }
        public static void WriteInt64(this Stream stream, ulong value)
        {
            if (BitConverter.IsLittleEndian)
                stream.Write(BitConverter.GetBytes(value).Reverse().ToArray());
            else stream.Write(BitConverter.GetBytes(value));
        }
        
        public static byte[] Join(params byte[][] parts)
        {
            var res=new byte[parts.Sum(q => q.Length)];
            var off = 0;
            foreach (byte[] part in parts) {
                Buffer.BlockCopy(part, 0, res, off, part.Length);
                off += part.Length;
            }
            return res;
        }
    }
}
