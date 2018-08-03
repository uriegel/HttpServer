using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    public class Alias
    {
        public Alias(string value, string path, string defaultFile)
        {
            Value = value;
            Path = path;
            if (System.IO.Path.IsPathRooted(Path))
                IsRooted = true;
            DefaultFile = defaultFile;
        }

        public string Value { get; }

        public string Path { get; }

        public string DefaultFile { get; }

        public bool IsRooted { get; }
    }
}
