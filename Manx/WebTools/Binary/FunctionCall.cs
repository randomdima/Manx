using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary
{
    public class FunctionCall: IBinaryAction
    {
        public Delegate fn;
        public object[] args;
        public void Process()
        {
            fn.DynamicInvoke(args);
        }
    }

    public class FunctionCallConverter : IBinaryConverter<FunctionCall>
    {
        BinaryConverter Root;
        public override int GetSize(FunctionCall value)
        {
            throw new NotImplementedException();
        }

        public override FunctionCall Read(byte[] buffer, ref int offset)
        {
            var value = new FunctionCall();
            value.fn = Root.Read<Delegate>(buffer, ref offset);
            var param = value.fn.GetMethodInfo().GetParameters().Skip(1).ToArray();
            value.args = new object[param.Length];
            for (var q = 0; q < param.Length; q++)
                value.args[q] = Root.GetConverter(param[q].ParameterType).Read(buffer, ref offset);
            return value;
        }

        public override void Write(byte[] buffer, FunctionCall value, ref int offset)
        {
            throw new NotImplementedException();
        }
        public override void Init(BinaryConverter root)
        {
            Root = root;
        }

    }
}
