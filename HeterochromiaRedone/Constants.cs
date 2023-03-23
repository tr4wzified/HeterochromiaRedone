using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HeterochromiaRedone.Structs;

namespace HeterochromiaRedone
{
    public static class Constants
    {
        public static EyesMesh ArgonianEyes = new EyesMesh
        {
            Left = new EyeMesh
            {
                NifPath = "meshes\\actors\\character\\character assets\\eyesargonianleft.nif",
                TriPath = "meshes\\actors\\character\\character assets\\eyesargonianleft.tri",
                ChargenPath = "meshes\\actors\\character\\character assets\\eyesargonianleftchargen.tri",
            },
            Right = new EyeMesh
            {
                NifPath = "meshes\\actors\\character\\character assets\\eyesargonianright.nif",
                TriPath = "meshes\\actors\\character\\character assets\\eyesargonianright.tri",
                ChargenPath = "meshes\\actors\\character\\character assets\\eyesargonianrightchargen.tri",
            }
        };

        public static EyesMesh KhajiitFemaleEyes = new EyesMesh
        {
            Left = new EyeMesh
            {
                NifPath = "meshes\\actors\\character\\character assets\\eyeskhajiitfemaleleft.nif",
                TriPath = "meshes\\actors\\character\\character assets\\eyeskhajiitfemaleleft.tri",
                ChargenPath = "meshes\\actors\\character\\character assets\\eyeskhajiitfemaleleftchargen.tri",
            },
            Right = new EyeMesh
            {
                NifPath = "meshes\\actors\\character\\character assets\\eyeskhajiitfemaleright.nif",
                TriPath = "meshes\\actors\\character\\character assets\\eyeskhajiitfemaleright.tri",
                ChargenPath = "meshes\\actors\\character\\character assets\\eyeskhajiitfemalerightchargen.tri",
            }
        };

        public static EyesMesh KhajiitMaleEyes = new EyesMesh
        {
            Left = new EyeMesh
            {
                NifPath = "meshes\\actors\\character\\character assets\\eyeskhajiitleft.nif",
                TriPath = "meshes\\actors\\character\\character assets\\eyeskhajiitleft.tri",
                ChargenPath = "meshes\\actors\\character\\character assets\\eyeskhajiitleftchargen.tri",
            },
            Right = new EyeMesh
            {
                NifPath = "meshes\\actors\\character\\character assets\\eyeskhajiitright.nif",
                TriPath = "meshes\\actors\\character\\character assets\\eyeskhajiitright.tri",
                ChargenPath = "meshes\\actors\\character\\character assets\\eyeskhajiitrightchargen.tri",
            }
        };

        public static EyesMesh NonBeastFemaleEyes = new EyesMesh
        {
            Left = new EyeMesh
            {
                NifPath = "meshes\\actors\\character\\character assets\\FaceParts\\EyeFemaleLeft.nif",
                TriPath = "meshes\\actors\\character\\character assets\\FaceParts\\EyesFemaleLeft.tri",
                ChargenPath = "meshes\\actors\\character\\character assets\\FaceParts\\EyesFemaleLeftChargen.tri",
            },
            Right = new EyeMesh
            {
                NifPath = "meshes\\actors\\character\\character assets\\FaceParts\\EyeFemaleRight.nif",
                TriPath = "meshes\\actors\\character\\character assets\\FaceParts\\EyesFemaleRight.tri",
                ChargenPath = "meshes\\actors\\character\\character assets\\FaceParts\\EyesFemaleRightChargen.tri",
            }
        };
        public static EyesMesh NonBeastMaleEyes = new EyesMesh
        {
            Left = new EyeMesh
            {
                NifPath = "meshes\\actors\\character\\character assets\\FaceParts\\EyesMaleLeft.nif",
                TriPath = "meshes\\actors\\character\\character assets\\FaceParts\\EyesMaleLeft.tri",
                ChargenPath = "meshes\\actors\\character\\character assets\\FaceParts\\EyesMaleLeftChargen.tri",
            },
            Right = new EyeMesh
            {
                NifPath = "meshes\\actors\\character\\character assets\\FaceParts\\EyesMaleRight.nif",
                TriPath = "meshes\\actors\\character\\character assets\\FaceParts\\EyesMaleRight.tri",
                ChargenPath = "meshes\\actors\\character\\character assets\\FaceParts\\EyesMaleRightChargen.tri",
            }
        };
    }
}
