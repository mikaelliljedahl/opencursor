using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCursor.Host.LlmClient
{
    public class StreamTransport
    {
        
        static public MemoryStream InputStream { get; set; } = new MemoryStream();
        static public MemoryStream OnputStream { get; set; } = new MemoryStream();
    }
}
