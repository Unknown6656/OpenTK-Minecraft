using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK;

using OpenTKTestRenderer.Properties;

using static System.Math;
using System.Drawing;
using System.Drawing.Imaging;

namespace OpenTKTestRenderer.Components
{
    public sealed unsafe class MainWindow
        : GameWindow
    {
        public double Time { get; private set; }
        public string[] Arguments { get; }

        private static readonly (ShaderType, string)[] _shaders = new[]
        {
            (ShaderType.VertexShader, "shaders/vshader.vert"),
            (ShaderType.FragmentShader, "shaders/fshader.frag"),
        };
        private readonly List<RenderObject> _objs = new List<RenderObject>();
        private Matrix4 _projection;
        private Matrix4 _modelview;
        private Vector3 _camtarg;
        private Vector3 _campos;
        private int _program;


        public MainWindow(string[] args)
            : base(1920, 1080, new GraphicsMode(new ColorFormat(8, 8, 8, 8), 16, 16, 4), nameof(MainWindow), GameWindowFlags.Default, DisplayDevice.Default, Program.GL_VERSION_MAJ, Program.GL_VERSION_MIN, GraphicsContextFlags.ForwardCompatible)
        {
            Arguments = args;
            WindowBorder = WindowBorder.Resizable;
        }

        internal int CompileShader(ShaderType type, string path)
        {
            int ptr = GL.CreateShader(type);

            GL.ShaderSource(ptr, Regex.Replace(File.ReadAllText(path), @"\#version\s*[0-9]{3}", $"#version {Program.GL_VERSION_MAJ}{Program.GL_VERSION_MIN}0", RegexOptions.Compiled | RegexOptions.IgnoreCase));
            GL.CompileShader(ptr);

            if (GL.GetShaderInfoLog(ptr) is string s && s.Trim().Length > 0)
                Debug.WriteLine($"GL.CompileShader[{type}]:\n{s}");

            return ptr;
        }

        internal int CreateProgram()
        {
            int prog = GL.CreateProgram();
            IEnumerable<int> sh = from t in _shaders select CompileShader(t.Item1, t.Item2);

            foreach (int s in sh)
                GL.AttachShader(prog, s);

            GL.LinkProgram(prog);

            if (GL.GetProgramInfoLog(prog) is string log && log.Trim().Length > 0)
                Debug.WriteLine($"GL.LinkProgram:\n{log}");

            foreach (int s in sh)
            {
                GL.DetachShader(prog, s);
                GL.DeleteShader(s);
            }

            return prog;
        }

        internal void RecreateProgram()
        {
            GL.DeleteProgram(_program);

            _program = CreateProgram();
        }



        public static TexturedVertex[] CreateTexturedCube(float side, float textureWidth, float textureHeight)
        {
            float h = textureHeight;
            float w = textureWidth;
            side = side / 2f; // half side - and other half

            return new TexturedVertex[]
            {
                new TexturedVertex(new Vector4(-side, -side, -side, 1.0f),   new Vector2(0, 0)),
                new TexturedVertex(new Vector4(-side, -side, side, 1.0f),    new Vector2(0, h)),
                new TexturedVertex(new Vector4(-side, side, -side, 1.0f),    new Vector2(w, 0)),
                new TexturedVertex(new Vector4(-side, side, -side, 1.0f),    new Vector2(w, 0)),
                new TexturedVertex(new Vector4(-side, -side, side, 1.0f),    new Vector2(0, h)),
                new TexturedVertex(new Vector4(-side, side, side, 1.0f),     new Vector2(w, h)),

                new TexturedVertex(new Vector4(side, -side, -side, 1.0f),    new Vector2(0, 0)),
                new TexturedVertex(new Vector4(side, side, -side, 1.0f),     new Vector2(w, 0)),
                new TexturedVertex(new Vector4(side, -side, side, 1.0f),     new Vector2(0, h)),
                new TexturedVertex(new Vector4(side, -side, side, 1.0f),     new Vector2(0, h)),
                new TexturedVertex(new Vector4(side, side, -side, 1.0f),     new Vector2(w, 0)),
                new TexturedVertex(new Vector4(side, side, side, 1.0f),      new Vector2(w, h)),

                new TexturedVertex(new Vector4(-side, -side, -side, 1.0f),   new Vector2(0, 0)),
                new TexturedVertex(new Vector4(side, -side, -side, 1.0f),    new Vector2(w, 0)),
                new TexturedVertex(new Vector4(-side, -side, side, 1.0f),    new Vector2(0, h)),
                new TexturedVertex(new Vector4(-side, -side, side, 1.0f),    new Vector2(0, h)),
                new TexturedVertex(new Vector4(side, -side, -side, 1.0f),    new Vector2(w, 0)),
                new TexturedVertex(new Vector4(side, -side, side, 1.0f),     new Vector2(w, h)),

                new TexturedVertex(new Vector4(-side, side, -side, 1.0f),    new Vector2(0, 0)),
                new TexturedVertex(new Vector4(-side, side, side, 1.0f),     new Vector2(0, h)),
                new TexturedVertex(new Vector4(side, side, -side, 1.0f),     new Vector2(w, 0)),
                new TexturedVertex(new Vector4(side, side, -side, 1.0f),     new Vector2(w, 0)),
                new TexturedVertex(new Vector4(-side, side, side, 1.0f),     new Vector2(0, h)),
                new TexturedVertex(new Vector4(side, side, side, 1.0f),      new Vector2(w, h)),

                new TexturedVertex(new Vector4(-side, -side, -side, 1.0f),   new Vector2(0, 0)),
                new TexturedVertex(new Vector4(-side, side, -side, 1.0f),    new Vector2(0, h)),
                new TexturedVertex(new Vector4(side, -side, -side, 1.0f),    new Vector2(w, 0)),
                new TexturedVertex(new Vector4(side, -side, -side, 1.0f),    new Vector2(w, 0)),
                new TexturedVertex(new Vector4(-side, side, -side, 1.0f),    new Vector2(0, h)),
                new TexturedVertex(new Vector4(side, side, -side, 1.0f),     new Vector2(0, 0)),

                new TexturedVertex(new Vector4(-side, -side, side, 1.0f),    new Vector2(0, 0)),
                new TexturedVertex(new Vector4(side, -side, side, 1.0f),     new Vector2(w, 0)),
                new TexturedVertex(new Vector4(-side, side, side, 1.0f),     new Vector2(0, h)),
                new TexturedVertex(new Vector4(-side, side, side, 1.0f),     new Vector2(0, h)),
                new TexturedVertex(new Vector4(side, -side, side, 1.0f),     new Vector2(w, 0)),
                new TexturedVertex(new Vector4(side, side, side, 1.0f),      new Vector2(w, h)),
            };
        }



        protected override void OnLoad(EventArgs e)
        {
            Closed += (s, a) => Exit();

            _program = CreateProgram();
            _projection = Matrix4.Identity;
            _modelview = Matrix4.Identity;


            
            _campos = new Vector3(0, 0, 3);
            _camtarg = Vector3.Zero;
            _objs.Add(new RenderObject(CreateTexturedCube(1, 512, 512), _program, "resources/black-stone--diffuse.png"));



            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.PatchParameter(PatchParameterInt.PatchVertices, 3);
            GL.Enable(EnableCap.DepthTest);
            GL.PointSize(2);

            CursorVisible = true;
            VSync = VSyncMode.Adaptive;
            WindowState = WindowState.Maximized;
        }

        public override void Exit()
        {
            foreach (RenderObject obj in _objs)
                obj.Dispose();

            GL.DeleteProgram(_program);

            base.Exit();
        }

        protected override void OnResize(EventArgs e) => GL.Viewport(0, 0, Width, Height);

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            HandleKeyboard();

            Time += e.Time;

            _modelview = Matrix4.CreateRotationY((float)Time);
            _projection = Matrix4.LookAt(_campos, _camtarg, new Vector3(0, 1, 0))
                        * Matrix4.CreatePerspectiveFieldOfView((float)(60 * PI / 180), (float)Width / Height, .01f, 10000f);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.ClearColor(new Color4(.2f, .3f, .5f, 1f));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit | ClearBufferMask.AccumBufferBit);
            GL.UseProgram(_program);
            GL.VertexAttrib1(2, Time);
            GL.Uniform1(3, Width);
            GL.Uniform1(4, Height);
            GL.Uniform3(10, ref _campos);
            GL.Uniform3(11, ref _camtarg);
            GL.UniformMatrix4(20, false, ref _projection);
            GL.UniformMatrix4(21, false, ref _modelview);

            foreach (RenderObject obj in _objs)
                obj.Render();

            SwapBuffers();
        }

        internal void HandleKeyboard()
        {
            KeyboardState keyState = Keyboard.GetState();
            Vector3 camdir = _camtarg - _campos;
            Matrix4 cmtransf = Matrix4.CreateTranslation(camdir);
            Matrix4 cmtransfn = Matrix4.CreateTranslation(-camdir);

            camdir = Vector3.Normalize(camdir);

            void transformcam(Matrix4 transf)
            {
                Vector4 res = cmtransfn * transf * cmtransf * new Vector4(_campos, 1);

                _campos = res.Xyz / res.W;
            }


            if (keyState.IsKeyDown(Key.Escape))
                Exit();
            else if (keyState.IsKeyDown(Key.F5))
                RecreateProgram();
            else if (keyState.IsKeyDown(Key.Number1))
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
            else if (keyState.IsKeyDown(Key.Number2))
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            else if (keyState.IsKeyDown(Key.Number3))
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            else if (keyState.IsKeyDown(Key.W))
                ;
            else if (keyState.IsKeyDown(Key.A))
                transformcam(Matrix4.CreateRotationY(.01f));
            else if (keyState.IsKeyDown(Key.S))
                ;
            else if (keyState.IsKeyDown(Key.D))
                transformcam(Matrix4.CreateRotationY(-.01f));
            else if (keyState.IsKeyDown(Key.Q))
                _campos += camdir;
            else if (keyState.IsKeyDown(Key.E))
                _campos -= camdir;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TexturedVertex
    {
        public Vector4 position;
        public Vector2 texcoord;


        public TexturedVertex(Vector4 p, Vector2 t)
        {
            position = p;
            texcoord = t;
        }
    }

    public sealed class RenderObject
        : IDisposable
    {
        private readonly int _prog, _vert, _buffer, _cnt, _tex_diffuse;
        private bool _init;


        unsafe public RenderObject(TexturedVertex[] vertices, int program, string path_diff)
        {
            _prog = program;
            _cnt = vertices.Length;
            _vert = GL.GenVertexArray();
            _buffer = GL.GenBuffer();

            GL.BindVertexArray(_vert);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vert);
            GL.NamedBufferStorage(_buffer, sizeof(TexturedVertex) * vertices.Length, vertices, BufferStorageFlags.MapWriteBit);
            GL.VertexArrayAttribBinding(_vert, 0, 0);
            GL.EnableVertexArrayAttrib(_vert, 0);
            GL.VertexArrayAttribFormat(_vert, 0, 4, VertexAttribType.Float, false, 0);
            GL.VertexArrayAttribBinding(_vert, 1, 0);
            GL.EnableVertexArrayAttrib(_vert, 1);
            GL.VertexArrayAttribFormat(_vert, 1, 2, VertexAttribType.Float, false, 16);
            GL.VertexArrayVertexBuffer(_vert, 0, _buffer, IntPtr.Zero, sizeof(TexturedVertex));

            _tex_diffuse = InitTexture(path_diff);
            _init = true;
        }

        private int InitTexture(string path)
        {
            float[] data = LoadTexture(path, out int w, out int h);

            GL.CreateTextures(TextureTarget.Texture2D, 1, out int tex);
            GL.TextureStorage2D(tex, 1, SizedInternalFormat.Rgba32f, w, h);

            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TextureSubImage2D(tex, 0, 0, 0, w, h, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.Float, data);

            return tex;
        }

        public void Bind()
        {
            GL.UseProgram(_prog);
            GL.BindVertexArray(_vert);
            GL.BindTexture(TextureTarget.Texture2D, _tex_diffuse);
        }

        public void Render()
        {
            GL.BindVertexArray(_vert);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _cnt);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        internal void Dispose(bool disposing)
        {
            if (disposing && _init)
            {
                GL.DeleteVertexArray(_vert);
                GL.DeleteBuffer(_buffer);

                _init = false;
            }
        }

        private static unsafe float[] LoadTexture(string path, out int w, out int h)
        {
            float[] r;

            using (Bitmap bmp = Image.FromFile(path) as Bitmap)
            {
                w = bmp.Width;
                h = bmp.Height;
                r = new float[w * h * 4];

                BitmapData dat = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                byte* ptr = (byte*)dat.Scan0;

                int index = 0;

                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        r[index + 0] = ptr[index + 0] / 255f;
                        r[index + 1] = ptr[index + 1] / 255f;
                        r[index + 2] = ptr[index + 2] / 255f;
                        r[index + 3] = ptr[index + 3] / 255f;

                        index += 4;
                    }
            }

            return r;
        }
    }
}
