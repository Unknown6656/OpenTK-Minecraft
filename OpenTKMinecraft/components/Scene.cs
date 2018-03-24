using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using OpenTK;

using OpenTKMinecraft.Minecraft;
using OpenTKMinecraft.Native;

namespace OpenTKMinecraft.Components
{
    using static SHADER_BIND_LOCATIONS;


    public sealed class Scene
        : Renderable
        , IVisuallyUpdatable
        , IShaderTarget
    {
        public Camera Camera { set; get; }
        public float Brightness { set; get; }
        public MainWindow Window { get; }
        public Lights Lights { get; }
        public World World { get; }

        public RenderableBlock this[long x, long y, long z]
        {
            set => World[x, y, z] = value;
            get => World[x, y, z];
        }

        public ref Light this[int i] => ref Lights[i];


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
            GL.ClearColor(new Color4(.2f, .3f, .5f, 1f));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            if (Camera.IsStereoscopic)
                GL.Clear(ClearBufferMask.AccumBufferBit);

            Program.Use();

            Vector3 campos = Camera.Position;
            Vector3 camtarg = Camera.Position + (Camera.FocalDistance * Camera.Direction);

            Bind();

            GL.LineWidth(2f);
            GL.PointSize(2f);
            // GL.PatchParameter(PatchParameterInt.PatchVertices, 4);

            GL.VertexAttrib1(WINDOW_TIME, time);
            GL.Uniform1(WINDOW_WIDTH, width);
            GL.Uniform1(WINDOW_HEIGHT, height);
            GL.Uniform3(CAMERA_POSITION, ref campos);
            GL.Uniform3(CAMERA_TARGET, ref camtarg);
            GL.Uniform1(CAMERA_FOCALDISTANCE, Camera.FocalDistance);
            GL.Uniform1(SCENE_ENVIRONMENT_AMBIENTBRIGHTNESS, Math.Max(0, Math.Min(Brightness, 1)));

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

                GL.Clear(ClearBufferMask.DepthBufferBit);

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

        public int AddLight(Light? light, RenderableBlock assoc_block = null) => Lights.Add(light, assoc_block);
    }
}
