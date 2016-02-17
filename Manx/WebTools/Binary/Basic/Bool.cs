using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Binary.Basic
{
    public class BooleanConverter : IBinaryConverter<Boolean>
    {
        public override int GetSize(bool value)
        {
            return 1;
        }
        public override int GetSize(object value)
        {
            return 1;
        }
        public override bool Read(byte[] buffer, ref int offset)
        {
            return buffer[offset++]==1;
        }
        public override void Write(byte[] buffer, bool value, ref int offset)
        {
            buffer[offset++] = (value ? (byte)1 : (byte)0);
        }
    }
}
