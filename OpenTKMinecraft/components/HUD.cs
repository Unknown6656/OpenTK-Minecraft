using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using OpenTK;

namespace OpenTKMinecraft.Components
{
    public unsafe sealed class HUD
        : IUpdatable
        , IDisposable
        , IShaderTarget
    {
        public double LastFPS { get; private set; }
        public ShaderProgram Program { get; }
        public GameWindow Window { get; }

        private VertexBuffer<HUDVertex> _crosshair;


        public HUD(GameWindow win, ShaderProgram prog)
        {
            Window = win;
            Program = prog;

            Program.Use();

            _crosshair = new VertexBuffer<HUDVertex>(new[]
            {
                new HUDVertex(-.1f, 0, Color4.Wheat),
                new HUDVertex(.1f, 0, Color4.Wheat),
                new HUDVertex(0, -.1f, Color4.Wheat),
                new HUDVertex(0, .1f, Color4.Wheat),
            }, 0, PrimitiveType.Lines, new[]
            {
                (0, VertexAttribType.Float, 3),
                (12, VertexAttribType.Float, 3),
                (24, VertexAttribType.Float, 2),
                (32, VertexAttribType.Float, 4),
            });
        }

        public void Update(double time, double delta)
        {
            LastFPS = 1 / delta;
        }

        public void Render(double time, float width, float height)
        {
            Program.Use();

            GL.LineWidth(3);
            GL.PointSize(3);

            _crosshair.Render();
        }

        public void Dispose()
        {
            _crosshair.Dispose();
            Program.Dispose();
        }
    }

    public sealed class VertexBuffer<T>
        : IDisposable
        where T : struct
    {
        private readonly int _array, _buffer, _count;
        private readonly PrimitiveType _type;


        public VertexBuffer(T[] vertices, int locoffset, PrimitiveType type, params (int offset, VertexAttribType type, int size)[] layout)
        {
            _type = type;
            _count = vertices.Length;
            _buffer = GL.GenBuffer();
            _array = GL.GenVertexArray();

            int tsz = Marshal.SizeOf<T>();

            GL.NamedBufferStorage(_buffer, tsz * vertices.Length, vertices, BufferStorageFlags.MapWriteBit);

            for (int i = 0; i < layout.Length; ++i)
            {
                GL.VertexArrayAttribBinding(_array, locoffset + i, 0);
                GL.EnableVertexArrayAttrib(_array, locoffset + i);
                GL.VertexArrayAttribFormat(_array, locoffset + i, layout[i].size, layout[i].type, false, layout[i].offset);
            }

            GL.VertexArrayVertexBuffer(_array, 0, _buffer, IntPtr.Zero, tsz);
        }

        public void Bind()
        {
            GL.BindVertexArray(_array);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _buffer);
        }

        public void Render()
        {
            Bind();

            GL.DrawArrays(_type, 0, _count);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_buffer);
            GL.DeleteVertexArray(_array);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HUDVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;
        public Color4 Color;


        public HUDVertex(float x, float y, Color4 c)
            : this(x, y, 0, c)
        {
        }

        public HUDVertex(float x, float y, float z, Color4 c)
            : this()
        {
            Color = c;
            Position = new Vector3(x, y, z);
        }
    }
}
