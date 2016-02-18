using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary.Basic
{   
    public class StringConverter : IBinaryConverter<string>
    {
        protected Encoding encoder = Encoding.ASCII;
        private BinaryConverter Root;
        public override void Init(BinaryConverter Root)
        {
            this.Root = Root;
        }
        public override int GetSize(string value)
        {
            if (value == null) return Root.UInt16.Size;
            return value.Length + Root.UInt16.Size;
        }
        public override string Read(byte[] buffer, ref int offset)
        {
            var len = Root.UInt16.Read(buffer, ref offset);
            var str = encoder.GetString(buffer, offset, len);
            offset += len;
            return str;
        }
        public override void Write(byte[] buffer, string value, ref int offset)
        {
            if (value == null)
            {
                Root.UInt16.Write(buffer, (UInt16)0, ref offset);
                return;
            }
            var len = encoder.GetBytes(value, 0, value.Length, buffer, offset + Root.UInt16.Size);
            Root.UInt16.Write(buffer, (UInt16)len, ref offset);
            offset += len;
        }
    }
}
