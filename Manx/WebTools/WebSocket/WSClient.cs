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
        private static UTF8Encoding Encoder= new UTF8Encoding(false, false);
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
            var stream = client.GetStream();
            StringBuilder sb =new StringBuilder();
            var RBS = client.ReceiveBufferSize;
            Handshake(stream);
            while (true)
            {
                var header = stream.ReadBytes(2);             
                if ((header[0] & 15) == 8) return;  //If closed
                var len = header[1] & 127;
                if (len == 127)
                    len = (int)ToUInt64(stream.ReadBytes(8));
                else if (len == 126)
                    len = (int)ToUInt16(stream.ReadBytes(2));
                var mask = stream.ReadBytes(4);
                var bytes=new byte[len];              
                var loaded = 0;
                while (loaded < len)    //loading until loaded xD
                    loaded += stream.Read(bytes, loaded, Math.Min(RBS, len - loaded));                
                while (len-- >0) bytes[len] ^= mask[len % 4];   //demasking data
                sb.Append(Encoder.GetString(bytes));                
                if ((header[0] & 128) == 0) continue;   //If not yet final block              
                Console.Write(sb);
                sb.Clear(); 
            }
        }
        protected void Handshake(NetworkStream stream)
        {
            string text = Encoder.GetString(stream.ReadBytes(1000));
            if (string.IsNullOrEmpty(text)) return;
            string key = KeyRegex.Match(text).Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(key)) return;
            var data =Encoder.GetBytes(
                        Convert.ToBase64String(
                            Encryptor.ComputeHash(
                                Encoder.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))));


            stream.Write(HandshakeMessageA, 0, HandshakeMessageA.Length);
            stream.Write(data, 0, data.Length);
            stream.Write(HandshakeMessageB, 0, HandshakeMessageB.Length);
        }
    }
}
