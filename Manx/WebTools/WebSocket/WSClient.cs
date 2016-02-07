using System;
using System.Collections.Concurrent;
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
        protected object locker=new object();
        protected Task sender;
        public event Action<string> OnMessage;
        public WSClient(Stream stream)
        {
            wsStream = stream;
            sender = Task.Run(() => { });
        }
        public void Listen()
        {
            StringBuilder sb =new StringBuilder();
            while (true)
            {
                var header = wsStream.Read(2);             
                if ((header[0] & 15) == 8) break;  //If closed
                var len = (header[1]==255? (int)wsStream.ReadInt64():
                          (header[1]==254? (int)wsStream.ReadInt16():
                           header[1] & 127)) + MaskSize;
                var bytes = wsStream.Read(len);    
                while (len--> MaskSize) bytes[len] ^= bytes[len % MaskSize];   //demasking data
                sb.Append(Encoder.GetString(bytes, MaskSize, bytes.Length- MaskSize));
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
        protected void onMessage(string Message)
        {
            if (OnMessage != null) Task.Run(()=>OnMessage(Message));
        }
        protected void SendData(object data)
        {
            SendData(data as byte[]);
        }
        protected void SendData(byte[] data)
        {
            wsStream.WriteByte(129);
            if (data.Length > UInt16.MaxValue)
            {
                wsStream.WriteByte(127);
                wsStream.WriteInt64((ulong)data.Length);
            }
            else if (data.Length > 125)
            {
                wsStream.WriteByte(126);
                wsStream.WriteInt16((ushort)data.Length);
            }
            else {
                wsStream.WriteByte((byte)data.Length);
            }
            wsStream.Write(data);
            wsStream.Flush();
        }
        public void Send(byte[] data)
        {
            lock(locker) 
                sender = sender.ContinueWith(q => { SendData(data); });
        }
    }

}
