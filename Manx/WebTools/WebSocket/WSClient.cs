using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebTools.Helpers;

namespace WebTools.WebSocket
{
    public class WSClient
    {
        private static UTF8Encoding Encoder;
        private static byte[] HandshakePatternA;
        private static byte[] HandshakePatternB;
        private static byte[] KeyPrefix;
        private static byte[] KeyPattern;
        private static byte SpaceKey;
        private static SHA1 Encryptor;
        private const byte MaskSize = 4; 
        static WSClient()
        {
            Encoder = new UTF8Encoding(false, false);
            Encryptor = SHA1.Create();
            SpaceKey = Encoder.GetBytes(" ")[0];
            KeyPattern = Encoder.GetBytes("************************258EAFA5-E914-47DA-95CA-C5AB0DC85B11");            
            KeyPrefix = Encoder.GetBytes("Sec-WebSocket-Key:");
            HandshakePatternA = Encoder.GetBytes("HTTP/1.1 101 Switching Protocols\r\nConnection:Upgrade\r\nUpgrade:websocket\r\nSec-WebSocket-Accept:");
            HandshakePatternB = Encoder.GetBytes("\r\n\r\n");
        }
        
        protected Stream wsStream;      
        public event Action<string> OnMessage;
        public WSClient(Stream stream)
        {
            wsStream = stream;        
        }
        public unsafe void Listen()
        {
            StringBuilder sb =new StringBuilder();
            while (true)
            {
                var header = wsStream.Read(2);             
                if ((header[0] & 8) > 0)  break;  //If closed
                var len = (header[1]==255? (int)wsStream.ReadInt64():
                          (header[1]==254? (int)wsStream.ReadInt16():
                           header[1] & 127)) + MaskSize;
                var bytes = wsStream.Read(len);    
                while (len--> MaskSize) bytes[len] ^= bytes[len % MaskSize];   //demasking data
                sb.Append(Encoder.GetString(bytes,MaskSize,bytes.Length-MaskSize));
                if(header[0] < 128) continue;   //If not yet final block              
                onMessage(sb.ToString());
                sb.Clear(); 
            }
        }
        public void Handshake(byte[] request)
        {
            int m = 0,q = 0;
            while (m != KeyPrefix.Length)
                if (request[q++] != KeyPrefix[m++])
                    m = 0;                
            while (request[++q] == SpaceKey);
            var _KeyPattern = new byte[KeyPattern.Length];
            Buffer.BlockCopy(KeyPattern, 0, _KeyPattern, 0, KeyPattern.Length);
            Buffer.BlockCopy(request, q, _KeyPattern, 0, 24);                  

            wsStream.Write(HandshakePatternA);
            wsStream.Write(Convert.ToBase64String(Encryptor.ComputeHash(_KeyPattern)));
            wsStream.Write(HandshakePatternB);
            wsStream.Flush();
        }
        protected void onMessage(string message)
        {
            if (OnMessage != null) Task.Run(()=>OnMessage(message));
        }
        public unsafe static byte[] BuildRespose(string data)
        {
            return BuildRespose(Encoder.GetBytes(data));
        }
        public unsafe static byte[] BuildRespose(byte[] data)
        {
            var dataOffset = 2;
            if (data.Length > UInt16.MaxValue) dataOffset += 8;
            else if (data.Length > 125) dataOffset += 2;
            var resp = new byte[data.Length + dataOffset];
            resp[0] = 129;
            if (dataOffset == 10)
            {
                resp[1] = 127;
                fixed (byte* len = &resp[2])
                    *(ulong*)len = (ulong)data.Length;
                // Buffer.BlockCopy(BitConverter.GetBytes(),0,resp,2,8);
            }
            else if (dataOffset == 4)
            {
                resp[1] = 126;
                fixed (byte* len = &resp[2])
                    *(ushort*)len = (ushort)data.Length;
                // Buffer.BlockCopy(BitConverter.GetBytes((ushort)data.Length), 0, resp, 2, 2);
            }
            else
                resp[1] = (byte)data.Length;
            Buffer.BlockCopy(data, 0, resp, dataOffset, data.Length);
            return resp;
        }
        public void Send(byte[] data)
        {
            data = BuildRespose(data);
            wsStream.WriteAsync(data,0,data.Length);
        }
        public void SendRaw(byte[] data)
        {
            wsStream.WriteAsync(data, 0, data.Length);
        }
    }

}
