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
    internal sealed class BinaryMemberInfo
    {
        public string Name { get; set; }
        public BinaryTypeInfo Type { get; set; }
    }
    internal sealed class BinaryTypeInfo:IRefObject
    {
        public BinaryTypeInfo() { }
        public BinaryTypeInfo(Type type)
        {
            this.type = type;
        }
        public override int GetHashCode()
        {
            return type.Name.GetHashCode();
        }
        public Type type;

        public BinaryTypeInfo ElementType
        {
            get
            {
                if (!typeof(IList).IsAssignableFrom(type)) return null;
                return new BinaryTypeInfo(type.GetElementType() ?? type.GetGenericArguments()[0]);
            }
        }
        public BinaryMemberInfo[] Methods
        {
            get
            {
                if (typeof(IList).IsAssignableFrom(type)) return null;
                return type.GetMethods(BindingFlags.InvokeMethod).Select(q => new BinaryMemberInfo() { Name = q.Name, Type = new BinaryTypeInfo(q.ReturnType) }).ToArray();
            }
        }
        public BinaryMemberInfo[] Properties
        {
            get
            {
                if (typeof(IList).IsAssignableFrom(type)) return null;
                return type.GetProperties().Select(q => new BinaryMemberInfo() { Name = q.Name, Type = new BinaryTypeInfo(q.PropertyType) }).ToArray();
            }
        }
    }
}
