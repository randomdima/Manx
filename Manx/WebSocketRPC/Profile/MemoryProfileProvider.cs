//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Profile;

//namespace WebRPC
//{
//    //ToDo
//    public class ProfileData : Dictionary<string, object>
//    {
//        private object _locker = new Object();
//        public string UserName { get; set; }
//        public T Get<T>(string Name, Func<string, T> Init = null)
//        {
//            object Data;
//            if (TryGetValue(Name, out Data) || Init == null) return (T)Data;
//            lock (_locker)
//                if (!TryGetValue(Name, out Data))
//                    Add(Name, Data = Init(UserName));
//            return (T)Data;
//        }
//        public T Set<T>(string Name, T Value)
//        {
//            lock (_locker)
//            {
//                Remove(Name);
//                if (Value != null)
//                    Add(Name, Value);
//                return Value;
//            }
//        }
//        public void Reset()
//        {
//            lock (_locker)
//                Clear();
//        }
//    }

//    //Stupid temp alternative for session/profile
//    public static class Profile
//    {
//        private static ProfileData All = new ProfileData();

//     //   [ThreadStatic]
//      //  private static ProfileData _Current;
//        private static ProfileData Current
//        {
//            get
//            {
//             //   if (_Current != null) return _Current;
//                var User = HttpContext.Current.Request.Headers["Impersonate"] ?? HttpContext.Current.User.Identity.Name;
//                return //_Current=
//                    All.Get(User, q => new ProfileData() { UserName = User });                
//            }
//        }
//        public static T Get<T>(string Name, Func<string, T> Init = null)
//        {
//            return Current.Get(Name,Init);
//        }
//        public static T Set<T>(string Name, T Value)
//        {
//            return Current.Set(Name, Value);
//        }
//        public static void Foreach(Action<ProfileData> Fn)
//        {
//            foreach (var q in All)
//                Fn(q.Value as ProfileData);
//        }
//        public static void Foreach<T>(string Name,Action<T> Fn)
//        {
//            foreach (var q in All)
//            {
//                T Val = (q.Value as ProfileData).Get<T>(Name);
//                if (Val == null) continue;
//                Fn(Val);
//            }
//        }
//        public static void Reset(string Name=null)
//        {
//            if (Name!=null) All.Set<object>(Name, null);
//            else All.Reset();
//        }
//    }
//}
