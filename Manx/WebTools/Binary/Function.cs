using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary
{
    public class FunctionProvider : IConverterProvider
    {
        public IBinaryConverter GetConverter(Type type, BinaryConverter Root)
        {
            if (!typeof(Delegate).IsAssignableFrom(type)) return null;
            type = typeof(FunctionConverter<>).MakeGenericType(type);
            return Activator.CreateInstance(type) as IBinaryConverter;
        }
    }
    public class FunctionConverter<T> : IBinaryConverter<T>
    {
        protected BinaryConverter Root;
        protected IBinaryConverter[] ArgumentConverters;
        public override int GetSize(T value)
        {
            return 0;
        }
        private class DelegateScope<T2>
        {
            public T2 value;
        }
        public override T Read(byte[] buffer, ref int offset)
        {
            return default(T);
            //var param= typeof(T).GetMethod("Invoke").GetParameters();
            //var etype = TypeConverter.BuildType(param.Select(q => q.ParameterType));
            //var eparam =  param.Select(q => Expression.Parameter(q.ParameterType, q.Name)).ToArray();
            //var scope = new DelegateScope<T>();
            //var body = Expression.Call(Expression.Constant(Root), Root.GetType().GetMethod("onFunctionCall"),
            //    Expression.Field(Expression.Constant(scope), "value"),
            //    Expression.New(etype.GetConstructors().First(), eparam));
            //var exp = Expression.Lambda<T>(body, eparam);
            //return scope.value = exp.Compile();
        }

        public override void Write(byte[] buffer, T value, ref int offset)
        {
           
        }

        public void qwe(byte[] buffer, object[] value, ref int offset) {
            for (var q = 0; q < ArgumentConverters.Length; q++)
                ArgumentConverters[q].Write(buffer,value[q],ref offset);
        }
        public override void Init(BinaryConverter root)
        {
            Root = root;
           // ArgumentConverters = typeof(T).GetMethod("Inkove").GetParameters().Select(q => Root.GetConverter(q.ParameterType)).ToArray();
        }
    }
}
