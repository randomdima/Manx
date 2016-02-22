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
using WebTools.HttpServer;

namespace WebTools.WebSocket
{
    public class WSHandler<T>:IHttpHandler where T : WSClient
    {
        public List<T> Clients=new List<T>();
        public event Action<T> OnConnect;
        public event Action<T> OnDisconnect;
        protected ReaderWriterLockSlim clientsLocker = new ReaderWriterLockSlim();
        public void Handle(Stream stream,byte[] request)
        {
            var wsClient = Activator.CreateInstance<T>();
            wsClient.Handshake(stream,request);
            onConnect(wsClient);
            wsClient.Listen();
            onDisconnect(wsClient); 
        }
        protected virtual void onDisconnect(T wsClient)
        {
            clientsLocker.EnterWriteLock();
            Clients.Remove(wsClient);
            clientsLocker.ExitWriteLock();
            if (OnDisconnect != null) OnDisconnect(wsClient);
        }
        protected virtual void onConnect(T wsClient)
        {
            clientsLocker.EnterWriteLock();
            Clients.Add(wsClient);
            clientsLocker.ExitWriteLock();
            if (OnConnect != null) OnConnect(wsClient);
        }

        public void Send(byte[] Message)
        {
            var data = WSClient.BuildRespose(Message);
            clientsLocker.EnterReadLock();
            Parallel.ForEach(Clients, q => q.SendRaw(data));
            clientsLocker.ExitReadLock();
        }
        public void Send(string Message)
        {
            Send(StreamHelpers.Encoder.GetBytes(Message));
        }
    }
}
