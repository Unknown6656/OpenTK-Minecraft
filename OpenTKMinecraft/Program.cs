using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK;
using OpenTKTestRenderer.Components;

namespace OpenTKTestRenderer
{
    public static unsafe class Program
    {
        public const int GL_VERSION_MAJ = 4;
        public const int GL_VERSION_MIN = 6;


        [STAThread]
        public static int Main(string[] args)
        {
            int ret = 0;

            try
            {
                InnerMain(args, ref ret);
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

        public static void InnerMain(string[] args, ref int exit)
        {
            using (MainWindow win = new MainWindow(args)
            {
                Width = 1280,
                Height = 720,
                Title = $"Test renderer [OpenGL v.{GL.GetString(StringName.Version)}]"
            })
                win.Run(120);
        }
    }
}
