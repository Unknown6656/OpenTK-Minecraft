using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using OpenTK;

using OpenTKMinecraft.Native;

namespace OpenTKMinecraft.Components
{
    using static SHADER_BIND_LOCATIONS;


    public interface IShaderTarget
    {
        ShaderProgram Program { get; }
        MainWindow Window { get; }

        void Render(double time, float width, float height);
    }

    public sealed class ShaderProgram
        : IDisposable
    {
        public static Dictionary<int, ShaderProgram> KnownPrograms { get; }
        public string[] CompiletimeConstants { get; }
        public string Name { get; }
        public int ID { get; }

        public PolygonMode PolygonMode
        {
            set
            {
                _mode = value;

                Use();
            }
            get => _mode;
        }

        private readonly (ShaderProgramType Type, string Path)[] _shaders;
        private readonly Dictionary<string, int> _includes;
        private PolygonMode _mode = PolygonMode.Fill;
        private bool _disposed;


        static ShaderProgram() => KnownPrograms = new Dictionary<int, ShaderProgram>();

        public ShaderProgram(string name, string[] constants, params (ShaderProgramType, string)[] shaders)
        {
            Name = name;
            _shaders = shaders;
            _includes = new Dictionary<string, int>();
            CompiletimeConstants = (constants ?? new string[0]).Select(s => s.ToUpper()).ToArray();
            ID = CreateProgram();

            KnownPrograms.Add(ID, this);

            PolygonMode = PolygonMode.Fill;
        }

        private int CompileShader(ShaderProgramType type, string path)
        {
            Regex reg_include = new Regex(@"\#include\s*\""\s*(?<name>[^\r\n]+)\s*\""\s*(\r|\n)+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Regex reg_if = new Regex(@"\#if\s+(?<condition>[^\r\n\#]+)(\r|\n)+(?<code>[^\#]+)(\r|\n)+\#endif\s*(\r|\n)+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            int ptr = GL.CreateShader((ShaderType)type);
            string code = File.ReadAllText(path);

            code = Regex.Replace(code, @"\#version\s*([0-9]{3}|xxx)\s*(\r|\n)+", $"#version {Program.GL_VERSION_MAJ}{Program.GL_VERSION_MIN}0", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            while (reg_include.IsMatch(code))
                code = reg_include.Replace(code, m =>
                {
                    string name = m.Groups["name"].ToString().ToLower().Trim();

                    try
                    {
                        string incl = _includes.ContainsKey(name) ? _shaders[_includes[name]].Path : $"{path}/../{name}";

                        return $"// begin of include '{name}'\n{File.ReadAllText(incl)}\n// end of include '{name}'\n";
                    }
                    catch
                    {
                        return $"#error \"The shader include file '{name}' could not be found\"\n";
                    }
                });

            while (reg_if.IsMatch(code))
                code = reg_if.Replace(code, m =>
                {
                    string shadertpstr = type.ToString().Replace("ShaderArb", "Shader").Replace("Shader", "_SHADER").ToUpper();
                    string condition = m.Groups["condition"].ToString().Trim();
                    string content = m.Groups["code"].ToString();
                    bool cond_met = false;
                    bool inv = condition.StartsWith("!");

                    if (inv)
                        condition = condition.Substring(1);

                    cond_met |= (condition.ToUpper() == shadertpstr) ^ inv;
                    cond_met |= CompiletimeConstants.Contains(condition.ToUpper()) ^ inv;
                    // todo : more expressions ?

                    return cond_met ? '\n' + content + '\n' : "";
                });

            GL.ShaderSource(ptr, code);
            GL.CompileShader(ptr);

            Console.WriteLine($"({Name}) GL.CompileShader[{type}]:\n{string.Join("\n", (GL.GetShaderInfoLog(ptr) ?? "").Split('\n').Select(x => "\t" + x))}");

            return ptr;
        }

        private int CreateProgram()
        {
            int prog = GL.CreateProgram();

            for (int i = 0; i < _shaders.Length; ++i)
                if (_shaders[i].Type == ShaderProgramType.ShaderInclude)
                    _includes[new FileInfo(_shaders[i].Path).Name.ToLower()] = i;

            IEnumerable<int> sh = from t in _shaders
                                  where t.Type != ShaderProgramType.ShaderInclude
                                  select CompileShader(t.Type, t.Path);

            foreach (int s in sh)
                GL.AttachShader(prog, s);

            GL.LinkProgram(prog);

            Console.WriteLine($"({Name}) GL.LinkProgram:\n{string.Join("\n", (GL.GetProgramInfoLog(prog) ?? "").Split('\n').Select(x => "\t" + x))}");

            foreach (int s in sh)
            {
                GL.DetachShader(prog, s);
                GL.DeleteShader(s);
            }

            return prog;
        }

        public void Use()
        {
            if (!_disposed)
            {
                // precaution
                GL.UseProgram(ID);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode);
            }
        }

        public void Use(Action f)
        {
            Use();
            f();

            GL.UseProgram(0);
        }

        public override string ToString() => $"'{Name}' ({ID:x4}h) : {string.Join(", ", _shaders.Select(x => x.Type.ToString()))}; Constants: {string.Join(", ", CompiletimeConstants)}";

        public void Dispose()
        {
            if (_disposed)
                return;
            else
                _disposed = true;

            GL.UseProgram(0);
            GL.DeleteProgram(ID);

            Console.WriteLine($"({Name}) GL.DeleteProgram");

            KnownPrograms.Remove(ID);
        }

        public static void DisposeAll()
        {
            foreach (ShaderProgram p in KnownPrograms.Values.ToArray())
                p.Dispose();
        }
    }

    public unsafe sealed class PostEffectShaderProgram<T>
        : IShaderTarget
        , IDisposable
        , IVisuallyUpdatable
        where T : class
                , IShaderTarget
                , IDisposable
                , IVisuallyUpdatable
    {
        private static readonly Vector4[] _vertices = new[]
        {
            new Vector4(-1, -1, 0, 1),
            new Vector4(1, -1, 0, 1),
            new Vector4(-1, 1, 0, 1),
            new Vector4(-1, 1, 0, 1),
            new Vector4(1, -1, 0, 1),
            new Vector4(1, 1, 0, 1),
        };
        private readonly int _vertexarr, _vertexbuff, _coltex, _deptex, _edeptex;
        private bool _disposed;

        public bool UsePostEffect { set; get; } = true;
        public PredefinedShaderEffect Effect { set; get; }
        public int RenderedEffectiveDepthTextureID { get; private set; }
        public int RenderedDepthTextureID { get; private set; }
        public int RenderedColorTextureID { get; private set; }
        public int FramebufferID { get; private set; }
        public ShaderProgram Program { get; }
        public MainWindow Window { get; }
        public T Object { get; }


        public PostEffectShaderProgram(T obj, ShaderProgram p, MainWindow win)
        {
            Object = obj;
            Window = win;
            Program = p;

            _vertexarr = GL.GenVertexArray();
            _vertexbuff = GL.GenBuffer();

            GL.BindVertexArray(_vertexarr);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexbuff);
            GL.NamedBufferStorage(_vertexbuff, sizeof(Vector4) * _vertices.Length, _vertices, BufferStorageFlags.MapWriteBit);
            GL.VertexArrayAttribBinding(_vertexarr, POSTRENDER_VERTEX_POSITION, 0);
            GL.EnableVertexArrayAttrib(_vertexarr, POSTRENDER_VERTEX_POSITION);
            GL.VertexArrayAttribFormat(_vertexarr, POSTRENDER_VERTEX_POSITION, 4, VertexAttribType.Float, false, 0);
            GL.VertexArrayVertexBuffer(_vertexarr, 0, _vertexbuff, IntPtr.Zero, sizeof(Vector4));

            FramebufferInit();

            Program.Use();

            _coltex = GL.GetUniformLocation(Program.ID, "renderedColor");
            _deptex = GL.GetUniformLocation(Program.ID, "renderedDepth");
            _edeptex = GL.GetUniformLocation(Program.ID, "renderedEffectiveDepth");

            Window.Resize += OnWindowResize;
        }

        ~PostEffectShaderProgram() => Dispose();

        public void Dispose()
        {
            lock (_vertices)
            {
                if (_disposed)
                    return;
                else
                    _disposed = true;

                Window.Resize -= OnWindowResize;

                Program.Use();

                GL.DeleteVertexArray(_vertexarr);
                GL.DeleteBuffer(_vertexbuff);

                FramebufferDispose();
                Program.Dispose();
                Object.Dispose();
            }
        }

        public void Render(double time, float width, float height)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, UsePostEffect ? FramebufferID : 0);
            GL.Viewport(0, 0, (int)width, (int)height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Object.Render(time, width, height);

            // for debugging:
            //
            //using (Bitmap bmp = new Bitmap(Window.Width, Window.Height))
            //{
            //    System.Drawing.Imaging.BitmapData data = bmp.LockBits(new Rectangle(0, 0, Window.Width, Window.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            //
            //    GL.ReadPixels(0, 0, Window.Width, Window.Height, PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
            //
            //    bmp.UnlockBits(data);
            //    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            //    bmp.Save("framebuffer.png");
            //}

            if (!UsePostEffect)
                return;

            Program.Use();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, (int)width, (int)height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.VertexAttrib1(WINDOW_TIME, time);
            GL.Uniform1(WINDOW_WIDTH, width);
            GL.Uniform1(WINDOW_HEIGHT, height);
            GL.Uniform1(WINDOW_PAUSED, Window.IsPaused ? 1 : 0);
            GL.Uniform1(POSTRENDER_EFFECT, (int)Effect);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, RenderedColorTextureID);
            GL.Uniform1(_coltex, 1);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, RenderedDepthTextureID);
            GL.Uniform1(_deptex, 2);

            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, RenderedEffectiveDepthTextureID);
            GL.Uniform1(_edeptex, 3);

            GL.BindVertexArray(_vertexarr);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertices.Length);
        }

        internal void OnWindowResize(object sender, EventArgs e)
        {
            if ((Window.Width < 10) || (Window.Height < 10) || !UsePostEffect)
                return;

            FramebufferDispose();
            FramebufferInit();
        }

        private void FramebufferDispose()
        {
            GL.DeleteTexture(RenderedColorTextureID);
            GL.DeleteTexture(RenderedDepthTextureID);
            GL.DeleteTexture(RenderedEffectiveDepthTextureID);
            GL.DeleteFramebuffer(FramebufferID);
        }

        private void FramebufferInit()
        {
            FramebufferID = GL.GenFramebuffer();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferID);

            RenderedColorTextureID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, RenderedColorTextureID);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, Window.Width, Window.Height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new[] { (int)TextureMagFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new[] { (int)TextureMinFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new[] { (int)TextureWrapMode.ClampToBorder });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new[] { (int)TextureWrapMode.ClampToBorder });

            RenderedDepthTextureID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, RenderedDepthTextureID);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, Window.Width, Window.Height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new[] { (int)TextureMagFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new[] { (int)TextureMinFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new[] { (int)TextureWrapMode.ClampToBorder });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new[] { (int)TextureWrapMode.ClampToBorder });

            RenderedEffectiveDepthTextureID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, RenderedEffectiveDepthTextureID);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32f, Window.Width, Window.Height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new[] { (int)TextureMagFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new[] { (int)TextureMinFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new[] { (int)TextureWrapMode.ClampToBorder });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new[] { (int)TextureWrapMode.ClampToBorder });

            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderedColorTextureID, 0);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, RenderedDepthTextureID, 0);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderedEffectiveDepthTextureID, 0);
            GL.DrawBuffers(2, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 });

            FramebufferErrorCode err = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            if (err != FramebufferErrorCode.FramebufferComplete)
                throw new Exception($"The framebuffer initialization failed as follows:  {err}/{GL.GetError()}");

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Update(double time, double delta, float aspectratio) => Object.Update(time, delta, aspectratio);

        public static implicit operator T(PostEffectShaderProgram<T> fx) => fx.Object;
    }

    public enum ShaderProgramType
    {
        ShaderInclude = 0,
        FragmentShader = 0x8B30,
        FragmentShaderArb = 0x8B30,
        VertexShader = 0x8B31,
        VertexShaderArb = 0x8B31,
        GeometryShader = 0x8DD9,
        TessEvaluationShader = 0x8E87,
        TessControlShader = 0x8E88,
        ComputeShader = 0x91B9
    }

    public enum PredefinedShaderEffect
    {
        None = 0,
        Edge = 1,
        Wobbles = 2,
    }
}
