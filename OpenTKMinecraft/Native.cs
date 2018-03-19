using System.Runtime.InteropServices;
using System.Text;
using System;

namespace OpenTKMinecraft.Native
{
    internal static class SHADER_BIND_LOCATIONS
    {
        public const int SCENE_VERTEX_POSITION = 1;
        public const int SCENE_VERTEX_NORMAL = 2;
        public const int SCENE_VERTEX_COLOR = 3;
        public const int SCENE_VERTEX_TANGENT = 4;
        public const int SCENE_VERTEX_BITANGENT = 5;
        public const int SCENE_VERTEX_TEXTURESIZE = 6;

        public const int WINDOW_TIME = 10;
        public const int WINDOW_WIDTH = 11;
        public const int WINDOW_HEIGHT = 12;
        public const int WINDOW_PAUSED = 13;

        public const int CAMERA_POSITION = 20;
        public const int CAMERA_TARGET = 21;
        public const int CAMERA_FOCALDISTANCE = 22;
        public const int CAMERA_EYETYPE = 23;

        public const int CAMERA_PROJECTION = 30;
        public const int CAMERA_MODELVIEW = 31;
        public const int CAMERA_MODELNORMALS = 32;

        public const int SCENE_ENVIRONMENT_AMBIENTBRIGHTNESS = 40;
        public const int SCENE_ENVIRONMENT_LIGHTCOUNT = 41;

        public const int HUD_VERTEX_POSITION = 1;
        public const int HUD_VERTEX_NORMAL = 2;
        public const int HUD_VERTEX_COLOR = 3;
        public const int HUD_VERTEX_TEXCOORD = 4;

        public const int AFTEREFFECT_VERTEX_POSITION = 1;
    }

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
