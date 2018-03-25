using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.IO;
using System;

using OpenTK.Graphics.OpenGL4;

namespace OpenTKMinecraft
{
    public static unsafe class Program
    {
        public const int GL_VERSION_MAJ = 4;
        public const int GL_VERSION_MIN = 6;
        public const string TEMP_DIR = ".tmp";

        public static readonly Spashscreen spscreen = new Spashscreen();

        [STAThread]
        [HandleProcessCorruptedStateExceptions]
        public static int Main(string[] args)
        {
            Assembly asm = typeof(Program).Assembly;
            string dir = new FileInfo(asm.Location).Directory.FullName;
            int ret = 0;

            Directory.SetCurrentDirectory(dir);

            if (!Directory.Exists(TEMP_DIR))
                Directory.CreateDirectory(TEMP_DIR);
#if !DEBUG
            try
            {
#endif
                spscreen.Show();
                spscreen.Text = ("Initializing ...", "");

                using (MainWindow win = new MainWindow(args)
                {
                    Width = 1280,
                    Height = 720,
                    Title = "OpenTK Minecraft"
                })
                {
                    string glverstr = GL.GetString(StringName.Version);

                    glverstr = Regex.Replace(glverstr, @"^(?<vers>[0-9\.]+)\s*.*$", m => m.Groups["vers"].ToString());

                    Version glvers = Version.Parse(glverstr);

                    if ((glvers.Major < GL_VERSION_MAJ) || ((glvers.Major == GL_VERSION_MAJ) && (glvers.Minor < GL_VERSION_MIN)))
                    {
                        Console.WriteLine($"This application requires at least OpenGL v.{GL_VERSION_MAJ}.{GL_VERSION_MIN} - however, only version {glvers} could be found on this machine.");

                        spscreen.Text = ("Failed.", $"OpenGL v.{GL_VERSION_MAJ}.{GL_VERSION_MIN} is at least required.");

                        ret = -1;
                    }
                    else
                        win.Run(200);
                }
#if !DEBUG
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();

                ret = ex.HResult;

                if (ret == 0)
                    ret = -1;

                while (ex != null)
                {
                    sb.Insert(0, $"{ex.Message}:\n{ex.StackTrace}\n");

                    ex = ex.InnerException;
                }

                Console.WriteLine(sb.ToString());
            }
#endif
            if (Debugger.IsAttached && (ret != 0))
            {
                Console.WriteLine("Press any key to exit ...");
                Console.ReadKey(true);
            }

            spscreen.Close();
            spscreen.Dispose();

            return ret;
        }
    }
}
