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
        private static byte[] HandshakePatternA;
        private static byte[] HandshakePatternB;
        private static byte[] KeyPrefix;
        private static byte[] KeyPattern;
        private static byte SpaceKey;
        private static SHA1 Encryptor;
        private const byte MaskSize = 4; 
        static WSClient()
        {
            Encryptor = SHA1.Create();
            SpaceKey = StreamHelpers.Encoder.GetBytes(" ")[0];
            KeyPattern = StreamHelpers.Encoder.GetBytes("************************258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
            KeyPrefix = StreamHelpers.Encoder.GetBytes("Sec-WebSocket-Key:");
            HandshakePatternA = StreamHelpers.Encoder.GetBytes("HTTP/1.1 101 Switching Protocols\r\nConnection:Upgrade\r\nUpgrade:websocket\r\nSec-WebSocket-Accept:");
            HandshakePatternB = StreamHelpers.Encoder.GetBytes("\r\n\r\n");
        }
        
        protected Stream wsStream;      
        public event Action<string> OnMessage;
        public WSClient(Stream stream)
        {
            wsStream = stream;        
        }

        private class wsHeader
        {
            public int blockSize;
            public byte[] mask;
            public bool isFinal = false;
            public static wsHeader Parse(byte[] data, ref int offset)
            {
                var res = new wsHeader();
                res.isFinal = data[offset] >= 128;
                if ((data[offset++] & 8) > 0) return null;  //If closed

                res.blockSize = data[offset++];
                res.blockSize = (res.blockSize == 255 ? (int)data.ReadInt64(ref offset) :
                                (res.blockSize == 254 ? data.ReadInt16(ref offset) :
                                 res.blockSize & 127));
                res.mask=new byte[MaskSize];
                Buffer.BlockCopy(data,offset,res.mask,0,MaskSize);
                offset += MaskSize;
                return res;
            }
        }
        public void Listen()
        {
            StringBuilder sb =new StringBuilder();
            byte[] buffer = new byte[StreamHelpers.RBS];
            wsHeader header=null;
            int offset=0;
            int blockLoaded = 0;
            int bufferLoaded=0;
            while (true)
            {
                if (offset == bufferLoaded)
                    bufferLoaded = wsStream.Read(buffer, offset = 0, StreamHelpers.RBS);

                if (header == null)
                    if((header=wsHeader.Parse(buffer,ref offset))==null) break;

                var xoff = offset;
                while (offset<bufferLoaded && blockLoaded<header.blockSize)
                    buffer[offset++] ^= header.mask[(blockLoaded++) % MaskSize];
                sb.Append(StreamHelpers.Encoder.GetString(buffer, xoff, offset-xoff));

                if (blockLoaded != header.blockSize) continue;
                if (header.isFinal) {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(onMessage), sb);
                    sb = new StringBuilder();
                }
                blockLoaded = 0;
                header = null;
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
        protected virtual void onMessage(object message)
        {
            string msg = message.ToString();
            if (OnMessage != null) OnMessage(msg.ToString());
        }
        public static byte[] BuildRespose(string data)
        {
            return BuildRespose(StreamHelpers.Encoder.GetBytes(data));
        }
        public static byte[] BuildRespose(byte[] data)
        {
            var dataOffset = 2;
            if (data.Length > UInt16.MaxValue) dataOffset += 8;
            else if (data.Length > 125) dataOffset += 2;
            var resp = new byte[data.Length + dataOffset];
            resp[0] = 129;
            if (dataOffset == 10)
            {
                resp[1] = 127;
                resp.WriteInt64((ulong)data.Length,2);
            }
            else if (dataOffset == 4)
            {
                resp[1] = 126;
                resp.WriteInt16((ushort)data.Length, 2);
            }
            else
                resp[1] = (byte)data.Length;
            Buffer.BlockCopy(data, 0, resp, dataOffset, data.Length);
            return resp;
        }

        public void Send(string Message)
        {
            Send(StreamHelpers.Encoder.GetBytes(Message));
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
