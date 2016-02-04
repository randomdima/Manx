//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Mail;
//using System.Text;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Configuration;

//namespace WebRPC.Helpers
//{
//    public static class ExceptionHelper
//    {
//        public static string LogEmailTo
//        {
//            get
//            {
//              return  WebConfigurationManager.AppSettings["LogEmailTo"];
//            }
//        }
//        public static string LogEmailFrom
//        {
//            get
//            {
//                return WebConfigurationManager.AppSettings["LogEmailFrom"]??"LogService@MCDEAN.COM";
//            }
//        }
//        public static string LogEmailSubject
//        {
//            get
//            {
//                return WebConfigurationManager.AppSettings["LogEmailSubject"]??"Exception Report";
//            }
//        }
//        public static void Email(this Exception E)
//        {
//            try
//            {
//                SmtpClient SC = new SmtpClient("SMTP.MCDEAN.COM");
//                MailMessage Mess = new MailMessage(LogEmailFrom, LogEmailTo, LogEmailSubject, E.Format(true));
//                SC.Send(Mess);
//            }
//            catch { }
//        }
//        private static string GetHTTPInfo(HttpRequest Request)
//        {
//            StringBuilder SB = new StringBuilder();
//            var URL = Request.Url.AbsoluteUri;
//            var i=URL.IndexOf("?");
//            if (i > 0) URL = URL.Substring(0, i);
//            SB.AppendLine("URL: " + URL);
//            SB.AppendLine("------------------");
//            try
//            {
//                var PParams = Json.ParseRequest(Request, null);
//                SB.AppendLine("Params:");
//                SB.AppendLine(PParams.ToString(Newtonsoft.Json.Formatting.Indented));
//                SB.AppendLine("------------------");
//            }
//            catch{
//                var query = Request.Url.Query;
//                if (query != null && query.Length > 0)
//                {
//                    SB.AppendLine("Query Data:");
//                    SB.AppendLine(query);
//                    SB.AppendLine("------------------");
//                }

//                var form= Request.Unvalidated.Form;
//                if (form != null)
//                    foreach (string q in form.AllKeys)
//                        {
//                            SB.AppendLine("Form Data ["+q+"]:");
//                            SB.AppendLine(form[q]);
//                            SB.AppendLine("------------------");
//                        }
//            }
//            return SB.ToString();
//        }
//        private static string GetUserInfo(HttpRequest Request)
//        {
//            var Impersonate = Request.Headers["Impersonate"];
//            if (Impersonate != null) return "User: " + Impersonate;
//            if (Request.LogonUserIdentity!=null)
//                    return "User: " + Request.LogonUserIdentity.Name;
//            return "User: N/A";
//        }
//        public static string Format(this Exception Ex,bool Detailed=false)
//        {
//            StringBuilder SB = new StringBuilder();
//            if (Detailed)
//            {
//                SB.AppendLine(GetUserInfo(HttpContext.Current.Request));
//                SB.AppendLine(GetHTTPInfo(HttpContext.Current.Request));    
//            }
//            do
//            {
//                SB.Append("Exception:   ");
//                if (!String.IsNullOrWhiteSpace(Ex.Message))
//                {
//                    SB.AppendLine(Ex.Message);
//                    SB.AppendLine("------------------");
//                }
//                if (!String.IsNullOrWhiteSpace(Ex.StackTrace))
//                {
//                    SB.AppendLine(Ex.StackTrace);
//                    SB.AppendLine("------------------");
//                }
//            }
//            while ((Ex = Ex.InnerException) != null);
//            return SB.ToString();

//        }
//        public static string FormatAsHtml(this Exception Ex, bool Detailed=false)
//        {
//            return Ex.Format(Detailed).Replace("\r\n", "<br/>").Replace("\n", "<br/>");
//        }
//    }
//}
