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
using WebTools.HttpServer;

namespace WebTools.WebSocket
{
    public class WSHandler:IHttpHandler
    {
        public List<WSClient> Clients=new List<WSClient>();
        public event Action<WSClient> OnConnect;
        public event Action<WSClient> OnDisconnect;
        protected ReaderWriterLockSlim clientsLocker = new ReaderWriterLockSlim();
        public void Handle(Stream stream,byte[] request)
        {
            var wsClient = new WSClient(stream);
            wsClient.Handshake(request);
            onConnect(wsClient);
            wsClient.Listen();
            onDisconnect(wsClient); 
        }
        protected void onDisconnect(WSClient wsClient)
        {
            clientsLocker.EnterWriteLock();
            Clients.Remove(wsClient);
            clientsLocker.ExitWriteLock();
            if (OnDisconnect != null) OnDisconnect(wsClient);
        }
        protected void onConnect(WSClient wsClient)
        {
            clientsLocker.EnterWriteLock();
            Clients.Add(wsClient);
            clientsLocker.ExitWriteLock();
            if (OnConnect != null) OnConnect(wsClient);
        }

        public void Send(string Message)
        {
            var data = WSClient.BuildRespose(Message);
            clientsLocker.EnterReadLock();
            Parallel.ForEach(Clients, new ParallelOptions { MaxDegreeOfParallelism = 50 }, q => q.SendRaw(data));
            clientsLocker.ExitReadLock();
        }
    }
}
