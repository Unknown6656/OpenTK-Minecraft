using System.Runtime.InteropServices;

using OpenTK.Graphics;
using OpenTK;

namespace OpenTKMinecraft.Components
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Color4 color;
        public Vector3 tangent;
        public Vector3 bitangent;


        public Vertex(Vector3 p, Vector3 n, Color4 c)
        {
            tangent = Vector3.Zero;
            bitangent = Vector3.Zero;
            position = p;
            normal = n;
            color = c;
        }

        public Vertex(Vector3 p, Vector3 n, Vector2 t)
            : this(p, n, new Color4(t.X, t.Y, 0, 1))
        {
        }

        public override string ToString() => $"P:{position}   N:{normal}   T:{tangent}   B:{bitangent}   C:{color}";
    }
}
