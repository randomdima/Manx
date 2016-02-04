//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data.Common;
//using System.Data.Objects;
//using System.Data.Objects.SqlClient;
//using System.Diagnostics;
//using System.Dynamic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Reflection;
//using System.Reflection.Emit;
//using System.Text;
//using System.Threading.Tasks;
//using WebRPC.Helpers;

//namespace WebRPC.Helpers
//{   
//    public class RefMethods
//    {
//        private static RefMethods NonGeneric;
//        private static Dictionary<Type, RefMethods> Generic;
//        public Type Type;
//        public NewExpression Selector;
//        public ParameterExpression Parameter;
//        public Dictionary<string, MemberBinding> Bindings;
//        public Dictionary<string, Expression> Properties;
//        public Expression Key;
//        public MethodInfo ToString;
//        public MethodInfo Contains;
//        public MethodInfo Take;
//        public MethodInfo Skip;
//        public MethodInfo Distinct;
//        public MethodInfo Count;
//        public MethodInfo Select;
//        public MethodInfo Where;
//        public MethodInfo OrderBy;
//        public MethodInfo OrderByDescending;
//        private static ModuleBuilder DynamicModule;
//        private static object _locker = new Object();
//        static RefMethods()
//        {
//            NonGeneric = new RefMethods();
//            AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynamicClasses"), AssemblyBuilderAccess.Run);
//            DynamicModule = assembly.DefineDynamicModule("Module");
//            Generic = new Dictionary<Type, RefMethods>();
//            NonGeneric.ToString = typeof(Object).GetMethod("ToString");
//            NonGeneric.Contains = typeof(String).GetMethod("Contains");
//            var QM = typeof(Queryable).GetMethods();
//            foreach (var q in QM)
//            {
//                switch (q.Name)
//                {
//                    case "Select": NonGeneric.Select = NonGeneric.Select ?? q; break;
//                    case "Distinct": NonGeneric.Distinct = NonGeneric.Distinct ?? q; break;
//                    case "Take": NonGeneric.Take = NonGeneric.Take ?? q; break;
//                    case "Skip": NonGeneric.Skip = NonGeneric.Skip ?? q; break;
//                    case "Count": NonGeneric.Count = NonGeneric.Count ?? q; break;
//                    case "Where": NonGeneric.Where = NonGeneric.Where ?? q; break;
//                    case "OrderBy": NonGeneric.OrderBy = NonGeneric.OrderBy ?? q; break;
//                    case "OrderByDescending": NonGeneric.OrderByDescending = NonGeneric.OrderByDescending ?? q; break;
//                }
//            }
//        }
//       public static Type GetNullableType(Type type)
//        {
//            // Use Nullable.GetUnderlyingType() to remove the Nullable<T> wrapper if type is already nullable.
//            type = Nullable.GetUnderlyingType(type)??type;
//            if (type.IsValueType)
//                return typeof(Nullable<>).MakeGenericType(type);
//            else
//                return type;
//        }
//        public static RefMethods GetGenericMethods(Type T)
//        {
//            RefMethods M;
//            if (!Generic.TryGetValue(T, out M))
//                lock (_locker)
//                    if (!Generic.TryGetValue(T, out M))
//                    {
//                        M = new RefMethods();
//                        M.Type = T;
//                        M.Parameter=Expression.Parameter(T, "q");
//                        M.Bindings = new Dictionary<string, MemberBinding>();
//                        M.Properties  = new Dictionary<string,Expression>();
//                        TypeBuilder tb = DynamicModule.DefineType(T.Name,
//                            TypeAttributes.Public);
//                        foreach (var f in T.GetProperties())
//                        {
//                            M.Key = M.Key ?? Expression.Property(M.Parameter, f);
//                            FieldBuilder newf = tb.DefineField(f.Name, GetNullableType(f.PropertyType), FieldAttributes.Public);
//                            M.Properties.Add(f.Name,Expression.Property(M.Parameter, f));
//                        }
//                        var SimpleType = tb.CreateType();
//                        foreach(var f in SimpleType.GetFields())
//                            M.Bindings.Add(f.Name, Expression.Bind(f, Expression.Property(M.Parameter, T.GetProperty(f.Name))));
//                        M.Selector = Expression.New(SimpleType);
//                        M.ToString = RefMethods.NonGeneric.ToString;    
//                        M.Contains = RefMethods.NonGeneric.Contains;
//                        M.Take = RefMethods.NonGeneric.Take.MakeGenericMethod(T);
//                        M.Skip = RefMethods.NonGeneric.Skip.MakeGenericMethod(T);
//                        M.Distinct = RefMethods.NonGeneric.Distinct.MakeGenericMethod(SimpleType);
//                        M.Count = RefMethods.NonGeneric.Count.MakeGenericMethod(T);
//                        M.Select = RefMethods.NonGeneric.Select.MakeGenericMethod(T,SimpleType);
//                        M.Where = RefMethods.NonGeneric.Where.MakeGenericMethod(T);
//                        M.OrderBy = RefMethods.NonGeneric.OrderBy;
//                        M.OrderByDescending = RefMethods.NonGeneric.OrderByDescending;
//                        Generic.Add(T, M);
//                    }
//            return M;
//        }
//    }
//    public class QueryRequest
//    {
//        public bool Count { get; set; }
//        public bool Distinct { get; set; }
//        public string[] Select { get; set; }
//        public int? Take { get; set; }
//        public int? Skip { get; set; }
//        public Dictionary<string,object> Where { get; set; }
//        public Dictionary<string, bool> OrderBy { get; set; }

//    }
//    public static class QueryableHelper
//    {
//        private static Expression Select(this Expression query, string[] fields, RefMethods Ref)
//        {
//            if (fields == null) return query;
//            var Initializer = Expression.MemberInit(Ref.Selector, fields.Select(q => Ref.Bindings[q]));
//            return Expression.Call(Ref.Select, query, Expression.Lambda(Initializer, Ref.Parameter));
//        }
//        private static Expression Satisfy(this Expression member, object value, RefMethods Ref)
//        {
//            if (value is JArray)
//            {
//                Expression Exp = null;
//                foreach (var P in (value as JArray))
//                {
//                    if (Exp == null) Exp = member.Satisfy((P as JValue).Value,Ref);
//                    else Exp = Expression.OrElse(Exp, member.Satisfy((P as JValue).Value, Ref));
//                }
//                return Exp;
//            }
//            if (value is JValue) value = (value as JValue).Value;
//            if (value == null)  return Expression.Equal(member, Expression.Constant(null));
//            var type = member.Type;

            
//            if (type!=typeof(string))
//            {
//                type = HandlerInfo.GetNotNullableType(type);
//                TypeConverter typeConverter = TypeDescriptor.GetConverter(type);
//                Expression propValue = Expression.Constant(typeConverter.ConvertFromString(value.ToString()));
//                if (propValue.Type != member.Type) propValue = Expression.Convert(propValue, member.Type);
//                return Expression.Equal(member, propValue);
//            }
           
//            var constant=Expression.Constant(value.ToString());
//            return Expression.Call(Expression.Call(member, Ref.ToString), Ref.Contains,constant);
//        }
//        private static Expression Where(this Expression query, Dictionary<string,object> filter, RefMethods Ref)
//        {
//            if (filter == null || filter.Count == 0) return query;
//            Expression Exp = null;
//            foreach (var s in filter)
//            {
//                Expression P = null;
//                if (!Ref.Properties.TryGetValue(s.Key, out P))
//                    throw new Exception("Property " + s.Key + " does not exist in " + Ref.Type.Name);
//                var Cond = P.Satisfy(s.Value, Ref);
                
//                if (Exp == null) Exp = Cond;
//                else Exp = Expression.AndAlso(Exp, Cond);
//            }
//            return Expression.Call(Ref.Where, query, Expression.Lambda(Exp, Ref.Parameter));
//        }
//        public static IQueryable<T> Where<T>(this IQueryable<T> Query, Dictionary<string, object> Filter)
//        {
//            if (Query == null || Filter == null) return Query;
//            var Ref = RefMethods.GetGenericMethods(Query.ElementType);
//            Expression Exp = Query.Expression.Where(Filter, Ref);
//            Query = Query.Provider.CreateQuery<T>(Exp);
//            return Query;
//        }

//        private static Expression OrderBy(this Expression query, string Field, bool Asc, RefMethods Ref)
//        {
//            var field = Field==null?Ref.Key:Ref.Properties[Field];
//            var Order = Asc ? Ref.OrderBy : Ref.OrderByDescending;
//            return Expression.Call(Order.MakeGenericMethod(Ref.Type, field.Type), query, Expression.Lambda(field, Ref.Parameter));
//        }
//        private static Expression OrderBy(this Expression query, Dictionary<string, bool> sorter, RefMethods Ref)
//        {
//            if (sorter == null || sorter.Count == 0) 
//                return query;      
//            foreach (var S in sorter)
//                query = query.OrderBy(S.Key, S.Value, Ref);
//            return query;
//        }
//        private static Expression Take(this Expression query, int? number, RefMethods Ref)
//        {
//            if (number == null|| number==0) return query;
//            return Expression.Call(Ref.Take, query, Expression.Constant(number));
//        }
//        private static Expression Skip(this Expression query, int? number, RefMethods Ref)
//        {
//            if (number == null || number==0) return query;
//            return Expression.Call(Ref.Skip, query, Expression.Constant(number));
//        }
//        private static Expression Distinct(this Expression query,bool condition, RefMethods Ref)
//        {
//            if (condition) return Expression.Call(Ref.Distinct, query);
//            return query;
//        }
//        private static Expression Count(this Expression query, RefMethods Ref)
//        {
//            return Expression.Call(Ref.Count, query);
//        }
//        private static IList ToList(this IQueryable query)
//        {
//            //  return new List(query);
//            List<object> Res = new List<object>();
//            IEnumerator en = query.GetEnumerator();
//            while (en.MoveNext()) Res.Add(en.Current);
//            return Res;
//        }
        
//        public static object Invoke(this IEnumerable Query, JObject Request)
//        {
//            if (Query == null) return null;
//            if (Request == null) return Query;
//            var QReq = Request.ToObject<QueryRequest>();
//            QReq.Select = null;
//            if (QReq.OrderBy == null && QReq.Skip==null &&
//                QReq.Take==null && QReq.Where==null) 
//                return Query;
//            return Query.AsQueryable().Invoke(QReq);
//        }
//        public static object Invoke(this IQueryable Query, JObject Request)
//        {
//            if (Query == null) return null;
//            return Query.Invoke(Request.ToObject<QueryRequest>());
//        }
//        public static object Invoke(this IQueryable Query, QueryRequest Request)
//        {
//            if (Query == null) return null;
//            if (Request == null) return Query.ToList();
//            var Ref = RefMethods.GetGenericMethods(Query.ElementType);
//            Expression Exp = Query.Expression.Where(Request.Where,Ref);

//            if ((Request.OrderBy == null || Request.OrderBy.Count == 0) && (Request.Take != null || Request.Skip != null))
//                Exp = Exp.OrderBy(null,false,Ref);
//            else Exp=Exp.OrderBy(Request.OrderBy, Ref);

//            Exp = Exp.Skip(Request.Skip, Ref)
//                     .Take(Request.Take, Ref)
//                     .Select(Request.Select, Ref)
//                     .Distinct(Request.Distinct,Ref);

//            if (Request.Count)
//                return Query.Provider.Execute<int>(Exp.Count(Ref));

//            Query = Query.Provider.CreateQuery(Exp);
//            return Query.ToList();
//        }
//    }
//}
