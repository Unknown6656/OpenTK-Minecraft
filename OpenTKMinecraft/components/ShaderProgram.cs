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
        public int ID { get; }
        public string Name { get; }

        public PolygonMode PolygonMode
        {
            set
            {
                Use();

                GL.PolygonMode(MaterialFace.FrontAndBack, value);
            }
        }

        private (ShaderType, string)[] _shaders;


        static ShaderProgram() => KnownPrograms = new Dictionary<int, ShaderProgram>();

        public ShaderProgram(string name, params (ShaderType, string)[] shaders)
        {
            Name = name;
            _shaders = shaders;
            ID = CreateProgram();

            KnownPrograms.Add(ID, this);

            PolygonMode = PolygonMode.Fill;
        }

        private int CompileShader(ShaderType type, string path)
        {
            int ptr = GL.CreateShader(type);

            GL.ShaderSource(ptr, Regex.Replace(File.ReadAllText(path), @"\#version\s*[0-9]{3}", $"#version {Program.GL_VERSION_MAJ}{Program.GL_VERSION_MIN}0", RegexOptions.Compiled | RegexOptions.IgnoreCase));
            GL.CompileShader(ptr);

            Console.WriteLine($"({Name}) GL.CompileShader[{type}]:\n{string.Join("\n", (GL.GetShaderInfoLog(ptr) ?? "").Split('\n').Select(x => "\t" + x))}");

            return ptr;
        }

        private int CreateProgram()
        {
            int prog = GL.CreateProgram();
            IEnumerable<int> sh = from t in _shaders select CompileShader(t.Item1, t.Item2);

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
}
