using System.Runtime.InteropServices;
using System.Text;
using System;

namespace OpenTKMinecraft.Native
{
    public static unsafe class OpenGL32
    {
        public const string Library = "opengl32.dll";


        [DllImport(Library)]
        public static extern void glAccum(AccumulationOperation op, float value);
    }

    [Flags]
    public enum AccumulationOperation
        : uint
    {
        Accumulate = 0x0100,
        Load = 0x0101,
        Return = 0x0102,
        Multiply = 0x0103,
        Add = 0x0104,
    }
}
