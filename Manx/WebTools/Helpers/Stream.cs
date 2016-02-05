using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTools.Helpers
{
    public static class StreamHelpers
    {
        public static byte[] ReadBytes(this Stream stream, int length)
        {
            var bytes = new byte[length];
            var qwe=stream.Read(bytes, 0, length);
            return bytes;
        }
    }
}
