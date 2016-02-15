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
        public static readonly int RBS = new TcpClient().ReceiveBufferSize;
        public static readonly int SBS = new TcpClient().SendBufferSize;
        public static readonly Encoding Encoder = new UTF8Encoding();
        public static byte SpaceKey = Encoder.GetBytes(" ")[0];

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

        public static ushort ReadInt16(this byte[] m_buffer, ref int offset)
        {
            offset+=2;
            if (BitConverter.IsLittleEndian)
                return (ushort)(m_buffer[offset-1] | (m_buffer[offset-2] << 0x08));
            return (ushort)(m_buffer[offset-2] | (m_buffer[offset-1] << 0x08));
        }
        public static ulong ReadInt64(this byte[] m_buffer, ref int offset)
        {
            uint num, num2;
            if (BitConverter.IsLittleEndian)
            {
                num = (uint)(((m_buffer[offset+7] |
                             (m_buffer[offset + 6] << 0x08)) |
                             (m_buffer[offset + 5] << 0x10)) |
                             (m_buffer[offset + 4] << 0x18));
                num2 = (uint)(((m_buffer[offset + 3] |
                           (m_buffer[offset + 2] << 0x08)) |
                           (m_buffer[offset + 1] << 0x10)) |
                           (m_buffer[offset] << 0x18));
            }
            else
            {
                num = (uint)(((m_buffer[offset] |
                          (m_buffer[offset+1] << 0x08)) |
                          (m_buffer[offset + 2] << 0x10)) |
                          (m_buffer[offset + 3] << 0x18));
                num2 = (uint)(((m_buffer[offset + 4] |
                           (m_buffer[offset + 5] << 0x08)) |
                           (m_buffer[offset + 6] << 0x10)) |
                           (m_buffer[offset + 7] << 0x18));
            }
            offset += 8;
            return (ulong)((num2 << 0x20) | num);
        }
        public static void WriteInt16(this byte[] m_buffer,ushort value, int offset)
        {
            var data = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                data = data.Reverse().ToArray();
            Buffer.BlockCopy(data, 0, m_buffer, offset, 2);
        }
        public static void WriteInt64(this byte[] m_buffer, ulong value, int offset)
        {
            var data = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                data = data.Reverse().ToArray();
            Buffer.BlockCopy(data, 0, m_buffer, offset, 8);
        }


        public static byte[] Join(IEnumerable<byte[]> parts)
        {
            var res = new byte[parts.Sum(q => q.Length)];
            var off = 0;
            foreach (byte[] part in parts)
            {
                Buffer.BlockCopy(part, 0, res, off, part.Length);
                off += part.Length;
            }
            return res;
        }
        public static byte[] Join(params byte[][] parts)
        {
            return Join(parts as IEnumerable<byte[]>);
        }
        public static byte[] Join(params string[] parts)
        {
            var counts = parts.Select(q => Encoder.GetByteCount(q)).ToArray();
            var res = new byte[counts.Sum()];
            var off = 0;
            for(var q=0;q<parts.Length;q++){
                Buffer.BlockCopy(Encoder.GetBytes(parts[q]),0,res,off,counts[q]);
                off+=counts[q];
            }
            return res;
        }
    }
}
