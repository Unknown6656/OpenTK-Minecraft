using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK;

namespace OpenTKMinecraft.Components
{
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
        public string Name { get; }
        public int ID { get; }

        public PolygonMode PolygonMode
        {
            set
            {
                Use();

                GL.PolygonMode(MaterialFace.FrontAndBack, value);
            }
        }

        private readonly (ShaderProgramType Type, string Path)[] _shaders;
        private readonly Dictionary<string, int> _includes;


        static ShaderProgram() => KnownPrograms = new Dictionary<int, ShaderProgram>();

        public ShaderProgram(string name, params (ShaderProgramType, string)[] shaders)
        {
            Name = name;
            _shaders = shaders;
            _includes = new Dictionary<string, int>();
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

                    cond_met |= condition.ToUpper() == shadertpstr;
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

        public void Use() => GL.UseProgram(ID);

        public void Use(Action f)
        {
            Use();
            f();

            GL.UseProgram(0);
        }

        public void Dispose()
        {
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

    public sealed class AfterEffectShaderProgram<T>
        : IShaderTarget
        , IDisposable
        where T : class, IShaderTarget
    {
        public ShaderProgram Program { get; }
        public int TargetTextureID { get; }
        public MainWindow Window { get; }
        public int FramebufferID { get; }
        public int DepthbufferID { get; }
        public T Object { get; }


        public AfterEffectShaderProgram(T obj, ShaderProgram p, MainWindow win)
        {
            Object = obj;
            Window = win;
            Program = p;

            GL.GetError(); // clear error
            GL.CreateFramebuffers(1, out int buff);
            GL.CreateTextures(TextureTarget.Texture2D, 1, out int tex);
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Window.Width, Window.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new[] { (int)TextureMagFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new[] { (int)TextureMinFilter.Nearest });

            DepthbufferID = GL.GenRenderbuffer();
            FramebufferID = buff;
            TargetTextureID = tex;

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthbufferID);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, Window.Width, Window.Height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, DepthbufferID);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TargetTextureID, 0);
            GL.DrawBuffers(1, new[] { DrawBuffersEnum.ColorAttachment0 });

            ErrorCode err0 = GL.GetError();
            FramebufferErrorCode err1 = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            if ((err0 != ErrorCode.NoError) || (err1 != FramebufferErrorCode.FramebufferComplete))
                throw new Exception($"The framebuffer initialization failed as follows:  {err0} : {err1}");

            Window.Resize += Win_Resize;
        }

        ~AfterEffectShaderProgram() => Dispose();

        public void Dispose()
        {
            Window.Resize -= Win_Resize;

            GL.DeleteTexture(TargetTextureID);
            GL.DeleteFramebuffer(FramebufferID);
            GL.DeleteRenderbuffer(DepthbufferID);
        }

        public void Render(double time, float width, float height)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferID);
            GL.Viewport(0, 0, (int)width, (int)height);

            Object.Render(time, width, height);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // todo : render to screen

            // GL.Uniform1(6, Window._paused ? 1 : 0);

        }

        private void Win_Resize(object sender, EventArgs e)
        {
            GL.BindTexture(TextureTarget.Texture2D, TargetTextureID);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Window.Width, Window.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthbufferID);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, Window.Width, Window.Height);
        }

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
}
