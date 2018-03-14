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
        public MainWindow Window { get; }

        private HUDVertexBuffer _crosshair;


        public HUD(MainWindow win, ShaderProgram prog)
        {
            Window = win;
            Program = prog;

            Program.Use();

            _crosshair = new HUDVertexBuffer(new[]
            {
                new HUDVertex(-0.25f, 0.25f, -0.5f, Color4.Wheat),
                new HUDVertex(0.0f, -0.25f, -0.5f, Color4.Wheat),
                new HUDVertex(0.25f, 0.25f, -0.5f, Color4.Wheat),
            }, PrimitiveType.Points);
        }

        public void Update(double time, double delta)
        {
            LastFPS = 1 / delta;
        }

        public void Render(double time, float width, float height)
        {
            Program.Use();

            PlayerCamera cam = Window.Camera;
            Vector3 campos = cam.Position;
            Vector3 camtarg = cam.Position + (cam.FocalDistance * cam.Direction);
            Matrix4 projection = Matrix4.LookAt(campos, camtarg, cam.Up);

            GL.Viewport(0, 0, Window.Width, Window.Height);
            GL.LineWidth(10);
            GL.PointSize(10);
            GL.Uniform1(6, Window._paused ? 1 : 0);
            GL.VertexAttrib1(7, time);
            GL.Uniform1(8, width);
            GL.Uniform1(9, height);
            GL.Uniform3(10, ref campos);
            GL.Uniform3(11, ref camtarg);
            GL.UniformMatrix4(20, false, ref projection);

            _crosshair.Render();
        }

        public void Dispose()
        {
            _crosshair.Dispose();
            Program.Dispose();
        }
    }

    public sealed unsafe class HUDVertexBuffer
        : IDisposable
    {
        private readonly int _array, _buffer, _count;
        private readonly PrimitiveType _type;
        private readonly HUDVertex[] _vert;


        public HUDVertexBuffer(HUDVertex[] vertices, PrimitiveType type)
        {
            _type = type;
            _vert = vertices;
            _count = vertices.Length;
            _buffer = GL.GenBuffer();
            _array = GL.GenVertexArray();

            GL.NamedBufferStorage(_buffer, sizeof(HUDVertex) * vertices.Length, vertices, BufferStorageFlags.MapWriteBit);

            GL.VertexArrayAttribBinding(_array, 0, 0);
            GL.EnableVertexArrayAttrib(_array, 0);
            GL.VertexArrayAttribFormat(_array, 0, 4, VertexAttribType.Float, false, 0);
            GL.VertexArrayAttribBinding(_array, 1, 0);
            GL.EnableVertexArrayAttrib(_array, 1);
            GL.VertexArrayAttribFormat(_array, 1, 4, VertexAttribType.Float, false, 16);
            GL.VertexArrayAttribBinding(_array, 2, 0);
            GL.EnableVertexArrayAttrib(_array, 2);
            GL.VertexArrayAttribFormat(_array, 2, 2, VertexAttribType.Float, false, 32);
            GL.VertexArrayAttribBinding(_array, 3, 0);
            GL.EnableVertexArrayAttrib(_array, 3);
            GL.VertexArrayAttribFormat(_array, 3, 4, VertexAttribType.Float, false, 48);

            GL.VertexArrayVertexBuffer(_array, 0, _buffer, IntPtr.Zero, sizeof(HUDVertex));
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
        public Vector4 Position;
        public Vector4 Normal;
        public Vector2 TexCoord;
        private float __padding__1;
        private float __padding__2;
        public Color4 Color;


        public HUDVertex(float x, float y, Color4 c)
            : this(x, y, 0, c)
        {
        }

        public HUDVertex(float x, float y, float z, Color4 c)
            : this()
        {
            Color = c;
            Position = new Vector4(x, y, z, 1);
        }
    }
}
