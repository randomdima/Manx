//using Fleck;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Web.Script.Serialization;
//using System.Reflection;
//using HandlerType = System.Func<System.Collections.Generic.Dictionary<string,object>, object>;

//namespace WebTools.RPC
//{
//    public class RPCServer : WebSocketServer
//    {
//        public static string Client
//        {
//            get
//            {
//                return Properties.Resources.Client;
//            }
//        }
//        private List<Tuple<string,HandlerType>> Handlers;
//        private List<IWebSocketConnection> Clients;
//        private static JavaScriptSerializer Serializer = new JavaScriptSerializer();

//        public RPCServer(string URL):base(URL)
//        {
//            FleckLog.Level = LogLevel.Error;
//            Clients = new List<IWebSocketConnection>();
//            Handlers = new List<Tuple<string, HandlerType>>();
//        }

//        public void FireEvent(string Name, object Data)
//        {
//            Clients.ForEach(c => Send(c,"",Name, Serializer.Serialize(Data)));
//        }

//        public void Add(params Type[] Assemblies)
//        {
//            var Members = Assemblies.SelectMany(q =>
//            {
//                var Types = q.Assembly.GetTypes();
//                return Types.Union(Types.SelectMany(w => w.GetMembers()));
//            });
//            foreach (var M in Members)
//            {
//                var AMember = M.GetCustomAttribute<RPCMember>();
//                if (AMember == null) continue;
//                Add(M.DeclaringType.Name + "/" + M.Name, AMember.CreateHandler(M));
//            }
//        }
//        public void Add(string path, HandlerType handler)
//        {
//            Handlers.Add(new Tuple<string, HandlerType>(path, handler));
//        }
//        private void InitSocket(IWebSocketConnection Socket)
//        {
//            var client = new wsClient(Socket);
//            Socket.OnOpen = () => OnOpen(Socket);
//            Socket.OnClose = () => OnClose(Socket);
//            Socket.OnMessage = message => OnMessage(Socket,message);
//        }
//        private void OnOpen(IWebSocketConnection Socket)
//        {
//            Clients.Add(Socket);
//        }
//        private void OnClose(IWebSocketConnection Socket)
//        {
//            Clients.Remove(Socket);
//        }
//        private void OnMessage(IWebSocketConnection Socket, string Message)
//        {
//            //Message:  "key handlerID argumentsJSON"
//            var parts = Message.Split(new char[] { ' ' }, 3);
//            string key = parts[0];
//            try
//            {                
//                HandlerType handler = Handlers[int.Parse(parts[1])].Item2;
//                var data = Serializer.Deserialize<Dictionary<string, object>>("{"+parts[2]+"}");

//                //If no callback requested
//                if (key.Length == 0) 
//                    handler(data);
//                else
//                    Send(Socket, ResponseStatus.Success, key, Serializer.Serialize(handler(data)));
//            }
//            catch(Exception E)
//            {
//                Send(Socket, ResponseStatus.Failed, key,E.Message);
//            }
//        }

//        private void Send(IWebSocketConnection socket,params object[] Params)
//        {
//            socket.Send(string.Join(" ", Params));
//        }

//        public void Start()
//        {
//            Start(InitSocket);
//        }
//    }
//}
