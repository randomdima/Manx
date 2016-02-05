using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebTools.Helpers;

namespace WebTools.WebSocket
{
    public enum FrameType : byte
    {
        Continuation,
        Text,
        Binary,
        Close = 8,
        Ping = 9,
        Pong = 10,
    }
    public class WSClient:IDisposable
    {
        private static UTF8Encoding Encoder= new UTF8Encoding(false, true);
        private static Regex KeyRegex = new Regex("Sec-WebSocket-Key: (.*)");
        private static byte[] HandshakeMessageA = Encoder.GetBytes("HTTP/1.1 101 Switching Protocols\r\nConnection:Upgrade\r\nUpgrade:websocket\r\nSec-WebSocket-Accept:");
        private static byte[] HandshakeMessageB = Encoder.GetBytes("\r\n\r\n");
        private static SHA1 Encryptor = SHA1.Create();

        private Task listener;

        public void Dispose()
        {
            if (listener != null) listener.Dispose();
        }
        public void Listen(TcpClient client)
        {
            Dispose();
            listener = Task.Run(() => { ProcessData(client); });
        }

        protected uint ToUInt16(byte[] m_buffer)
        {
            if (BitConverter.IsLittleEndian)
                 return (uint)(m_buffer[1] | (m_buffer[0] << 0x08));
            return (uint)(m_buffer[0] | (m_buffer[1] << 0x08));
        }
        protected long ToUInt64(byte[] m_buffer)
        {
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
            return (long)((num2 << 0x20) | num);
        }
        protected void ProcessData(TcpClient client)
        {
            bool handshake = false;
            var stream = client.GetStream();
           
            while (true)
            {
                while (!stream.DataAvailable) Thread.Sleep(1);
                var Available = client.Available;
                if (!handshake)
                {
                    Handshake(stream, stream.ReadBytes(Available));
                    handshake = true;
                    continue;
                }

                var first = stream.ReadByte();
                if ((first & 15) != (int)FrameType.Text) continue;
                var isFinal = (first & 128) != 0;
                var len = stream.ReadByte() & 127;
                if (len == 127)
                    len = (int)ToUInt64(stream.ReadBytes(8));
                else
                    if (len == 126)
                        len = (int)ToUInt16(stream.ReadBytes(2));
                var mask = stream.ReadBytes(4);

                var bytes=new byte[len];
                var loaded = 0;
                while (loaded<len)
                {
                    loaded+=stream.Read(bytes, loaded, client.Available);
                    while (!stream.DataAvailable) Thread.Sleep(1);
                }
                while (loaded-- > 0) bytes[loaded] ^= mask[loaded % 4];

                if (isFinal)
                {
                    Console.WriteLine(Encoder.GetString(bytes));
                }
            }
        }
        protected void Handshake(NetworkStream stream,byte[] data)
        {
            string text = Encoder.GetString(data);
            if (string.IsNullOrEmpty(text)) return;
            string key = KeyRegex.Match(text).Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(key)) return;
            data =Encoder.GetBytes(
                        Convert.ToBase64String(
                            Encryptor.ComputeHash(
                                Encoder.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))));


            stream.Write(HandshakeMessageA, 0, HandshakeMessageA.Length);
            stream.Write(data, 0, data.Length);
            stream.Write(HandshakeMessageB, 0, HandshakeMessageB.Length);
        }
    }
}
