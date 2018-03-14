using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using OpenTK;

using OpenTKMinecraft.Utilities;
using OpenTKMinecraft.Minecraft;
using OpenTKMinecraft.Native;

namespace OpenTKMinecraft.Components
{
    public sealed class Scene
        : Renderable
        , IShaderTarget
    {
        public Camera Camera { set; get; }
        public float Brightness { set; get; }
        public MainWindow Window { get; }
        public Lights Lights { get; }
        public World World { get; }


        public Scene(MainWindow win, ShaderProgram program)
            : base(program, 0)
        {
            Brightness = .25f;
            Lights = new Lights(program);
            World = new World(this);
            Camera = null;
            Window = win;
        }

        public void Update(double time, double delta, float aspectratio)
        {
            World.Update(time, delta);
            Camera?.Update(time, delta, aspectratio);
        }

        public override void Bind() => Lights.Bind();

        [Obsolete("use 'Render(double, float, float)' instead.", true)]
        public new void Render()
        {
        }

        public void Render(double time, float width, float height)
        {
            Program.Use();

            Vector3 campos = Camera.Position;
            Vector3 camtarg = Camera.Position + (Camera.FocalDistance * Camera.Direction);

            Bind();

            GL.LineWidth(1.5f);
            GL.PointSize(1.5f);
            // GL.PatchParameter(PatchParameterInt.PatchVertices, 4);

            GL.Uniform1(6, Window._paused ? 1 : 0);
            GL.VertexAttrib1(7, time);
            GL.Uniform1(8, width);
            GL.Uniform1(9, height);
            GL.Uniform3(10, ref campos);
            GL.Uniform3(11, ref camtarg);
            GL.Uniform1(12, Camera.FocalDistance);
            GL.Uniform1(30, Math.Max(0, Math.Min(Brightness, 1)));

            Lights.Render();

            if (Camera.IsStereoscopic)
            {
                Vector3 right = Vector3.Normalize(Vector3.Cross(camtarg - campos, Camera.Up));

                // left eye
                World.Render(Camera, new CameraRenderData
                {
                    Projection = Matrix4.LookAt(campos - Camera.EyeSeparation / 2 * right, camtarg, Camera.Up),
                    StereoMode = CameraStereoMode.LeftEye,
                });
                OpenGL32.glAccum(AccumulationOperation.Load, 1);

                // right eye
                World.Render(Camera, new CameraRenderData
                {
                    Projection = Matrix4.LookAt(campos + Camera.EyeSeparation / 2 * right, camtarg, Camera.Up),
                    StereoMode = CameraStereoMode.RightEye,
                });
                OpenGL32.glAccum(AccumulationOperation.Accumulate, 1);
                OpenGL32.glAccum(AccumulationOperation.Return, 1);
            }
            else
                World.Render(Camera, null);
        }

        protected override void Dispose(bool disposing)
        {
            Lights?.Dispose();
            World?.Dispose();
        }
    }
}
