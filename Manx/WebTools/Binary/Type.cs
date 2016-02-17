using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary
{
    internal class BinaryTypeInfo
    {
        public BinaryTypeInfo() { }
        public BinaryTypeInfo(Type type)
        {
            Name = type.Name;
            IsSealed = type.IsSealed;
            var mehflag=BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            Methods = type.GetMethods(mehflag).Where(q => !q.IsSpecialName).ToDictionary(q => q.Name, q => q.ReturnType);
            Properties = type.GetProperties(mehflag).Where(q => !q.IsSpecialName).ToDictionary(q => q.Name, q => q.PropertyType);
            Events = type.GetEvents(mehflag).Where(q => !q.IsSpecialName).ToDictionary(q => q.Name, q => typeof(int));// q.EventHandlerType.GetGenericArguments());
        }
        public string Name { get; set; }
        public bool IsSealed { get; set; }
        public Dictionary<string, Type> Methods { get; set; }
        public Dictionary<string, Type> Properties { get; set; }
        public Dictionary<string, Type> Events { get; set; }
    }

    public class TypeConverter:IBinaryConverter<Type>
    {
        private IBinaryConverter<BinaryTypeInfo> Converter;
        private BinaryConverter Root;
        public override int GetSize(Type value)
        {
            if (typeof(IList).IsAssignableFrom(value))
                return Root.Byte.Size + Root.GetSize(value.GetElementType() ?? value.GetGenericArguments()[0]);
            if (typeof(IDictionary).IsAssignableFrom(value))
                return Root.Byte.Size + Root.GetSize(value.GetGenericArguments()[1]);

            return Root.Byte.Size + Converter.GetSize(new BinaryTypeInfo(value));
        }

        public override Type Read(byte[] buffer, ref int offset)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, Type value, ref int offset)
        {
            if (typeof(IList).IsAssignableFrom(value))
            {
                Root.Byte.Write(buffer,1,ref offset);
                Root.Write(buffer, value.GetElementType() ?? value.GetGenericArguments()[0], ref offset);
                return;
            }
            if (typeof(IDictionary).IsAssignableFrom(value))
            {
                Root.Byte.Write(buffer, 2, ref offset);
                Root.Write(buffer, value.GetGenericArguments()[1], ref offset);
                return;
            }
            Root.Byte.Write(buffer, 0, ref offset);
            Converter.Write(buffer, new BinaryTypeInfo(value), ref offset);
        }
        public override void Init(BinaryConverter Root)
        {
            this.Root = Root;
            Converter = new ObjectConverter<BinaryTypeInfo>();
            Converter.Init(Root);
        }
    }
}
