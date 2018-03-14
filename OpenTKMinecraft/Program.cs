using System.Runtime.ExceptionServices;
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

            try
            {
                using (MainWindow win = new MainWindow(args)
                {
                    Width = 1280,
                    Height = 720,
                    Title = $"Test renderer [OpenGL v.{GL.GetString(StringName.Version)}]"
                })
                    win.Run(120);
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

            if (Debugger.IsAttached && (ret != 0))
            {
                Console.WriteLine("Press any key to exit ...");
                Console.ReadKey(true);
            }

            return ret;
        }
    }
}
