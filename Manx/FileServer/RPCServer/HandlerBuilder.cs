using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using HandlerType = System.Func<System.Collections.Generic.Dictionary<string, object>, object>;

namespace WebTools.RPC
{
    public static class HandlerBuilder
    {

        private static JavaScriptSerializer JsonSerializer = new JavaScriptSerializer();
        public static object GetDefaultValue(Type Type)
        {
            if (Type.IsValueType)
            {
                return Activator.CreateInstance(Type);
            }
            return null;
        }
        public static object GetParamDefaultValue(ParameterInfo Param)
        {
            if (Param.HasDefaultValue) return Param.DefaultValue;
            return GetDefaultValue(Param.ParameterType);
        }

        public static T Field<T>(Dictionary<string, object> Obj, string Name, T Def)
        {
            object V;
            if (Obj.TryGetValue(Name, out V))
                return (T)JsonSerializer.ConvertToType(V, typeof(T));
            else return Def;
        }

        private static MethodInfo DisposeMethod = typeof(IDisposable).GetMethod("Dispose");
     //   private static MethodInfo QInvokeMethod = typeof(QueryableHelper).GetMethod("Invoke", new Type[] { typeof(IQueryable), typeof(JObject) });
    //    private static MethodInfo EInvokeMethod = typeof(QueryableHelper).GetMethod("Invoke", new Type[] { typeof(IEnumerable), typeof(JObject) });
        private static Expression GetLambda_Exec(Expression Caller, ParameterExpression Param)
        {
            // J => {Method(J);return null;}
            if (Caller.Type == typeof(void))
                return Expression.Block(Caller, Expression.Constant(null));

            // J=> Exec(Method(J))
       //     if (typeof(IQueryable).IsAssignableFrom(Caller.Type))
      //          return Expression.Call(QInvokeMethod, Caller, Param);



            // J=>(object)Method(J)
            return Expression.Convert(Caller,typeof(object));

      //      if (typeof(IEnumerable).IsAssignableFrom(Caller.Type))
      //          return Expression.Call(EInvokeMethod, Caller, Param);

            // J=>Method(J)
        }
        public static HandlerType GetHandler(this MethodInfo MI)
        {
            ParameterExpression JParam = Expression.Parameter(typeof(Dictionary<string, object>), "j");

            //Creating Param Convert/Assign -> F(J["A"],J["B"],...)
            Expression[] PBinder = MI.GetParameters().Select(q => Expression.Call(typeof(HandlerBuilder), "Field", new Type[] { q.ParameterType },
                                                                        JParam,
                                                                        Expression.Constant(q.Name),
                                                                        Expression.Constant(GetParamDefaultValue(q), q.ParameterType)))
                                        .ToArray();

            // Simple Static Method Invoke
            // J=> Class.Method(J["A"],J["B"],...)
            if (MI.IsStatic)
                return Expression.Lambda<HandlerType>(GetLambda_Exec(Expression.Call(MI, PBinder), JParam), JParam).Compile();

            //Constructor for not static methods
            Expression Constructor = Expression.New(MI.DeclaringType.GetConstructor(new Type[] { }));

            //if not disposable -> inline call
            //J=> (new Class()).Method(J["A"],J["B"],...)
            if (!typeof(IDisposable).IsAssignableFrom(MI.DeclaringType))
                return Expression.Lambda<HandlerType>(GetLambda_Exec(Expression.Call(Constructor, MI, PBinder), JParam), JParam).Compile();

            //if disposable -> need to dispose 
            ParameterExpression Instance = Expression.Variable(MI.DeclaringType, "i");

            //J=> using(var i=new Class()) i.Method(J["A"],J["B"],...);
            var res = Expression.Block(
                new[] { Instance },
                Expression.TryFinally(
                        GetLambda_Exec(Expression.Call(Expression.Assign(Instance, Constructor), MI, PBinder), JParam),
                        Expression.Call(Instance, DisposeMethod)));

            return Expression.Lambda<HandlerType>(res, JParam).Compile();
        }
        //public static HandlerType GetDelegate(this TypeInfo TI)
        //{
        //    object Constant;
        //    if (TI.IsEnum)
        //        Constant = Enum.GetValues(TI).Cast<object>().ToDictionary(q => q.ToString(), q => (int)q);
        //    else Constant = TI.GetFields(BindingFlags.Public | BindingFlags.Static).ToDictionary(q => q.Name, q => q.GetValue(null));
        //    return q => Constant;
        //}
        //public static HandlerType GetDelegate(this FieldInfo FI)
        //{
        //    ParameterExpression JParam = Expression.Parameter(typeof(JObject), "j");

        //    // Simple Static Method Invoke
        //    // J=> Class.Method(J["A"],J["B"],...)
        //    if (FI.IsStatic)
        //        return Expression.Lambda<JMethod>(GetLambda_Exec(Expression.Field(null, FI), JParam), JParam).Compile();

        //    //Constructor for not static methods
        //    Expression Constructor = Expression.New(FI.DeclaringType.GetConstructor(new Type[] { }));

        //    //if not disposable -> inline call
        //    //J=> (new Class()).Method(J["A"],J["B"],...)
        //    if (!typeof(IDisposable).IsAssignableFrom(FI.DeclaringType))
        //        return Expression.Lambda<JMethod>(GetLambda_Exec(Expression.Field(Constructor, FI), JParam), JParam).Compile();

        //    //if disposable -> need to dispose 
        //    ParameterExpression Instance = Expression.Variable(FI.DeclaringType, "i");

        //    //J=> using(var i=new Class()) i.Method(J["A"],J["B"],...);
        //    var res = Expression.Block(
        //        new[] { Instance },
        //        Expression.TryFinally(
        //                GetLambda_Exec(Expression.Field(Expression.Assign(Instance, Constructor), FI), JParam),
        //                Expression.Call(Instance, DisposeMethod)));

        //    return Expression.Lambda<JMethod>(res, JParam).Compile();
        //}
        public static HandlerType GetHandler(this MemberInfo Q)
        {
            var MI = Q as MethodInfo;
            if (MI != null) return MI.GetHandler();
            var PI = Q as PropertyInfo;
            if (PI != null) return PI.GetMethod.GetHandler();
            var FI = Q as FieldInfo;
            if (FI != null) return FI.GetHandler();
            var TI = Q as TypeInfo;
            if (TI != null) return TI.GetHandler();
            throw new Exception("Member Type is not supported");
        }
    }
}
