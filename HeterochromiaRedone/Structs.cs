using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeterochromiaRedone
{
    public class Structs
    {
        public struct EyesMesh
        {
            public EyeMesh Right { get; set; }
            public EyeMesh Left { get; set; }
        }

        public struct EyeMesh
        {
            public string NifPath { get; set; }
            public string TriPath { get; set; }
            public string ChargenPath { get; set; }
        }
    }
}
