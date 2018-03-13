using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK;

using OpenTKMinecraft.Properties;
using OpenTKMinecraft.Components;
using OpenTKMinecraft.Minecraft;
using OpenTKMinecraft.Utilities;

using static System.Math;

namespace OpenTKMinecraft
{
    public sealed unsafe class MainWindow
        : GameWindow
    {
        public PlayerCamera Camera => _scene.Camera as PlayerCamera;
        public double Time { get; private set; }
        public string[] Arguments { get; }

        private int _mousex, _mousey;
        private Scene _scene;
        private HUD _hud;


        public MainWindow(string[] args)
            : base(1920, 1080, new GraphicsMode(new ColorFormat(8, 8, 8, 8), 16, 16, 4), nameof(MainWindow), GameWindowFlags.Default, DisplayDevice.Default, Program.GL_VERSION_MAJ, Program.GL_VERSION_MIN, GraphicsContextFlags.ForwardCompatible)
        {
            Arguments = args;
            WindowBorder = WindowBorder.Resizable;
        }

        protected override void OnLoad(EventArgs e)
        {
            Closed += (s, a) => Exit();

            _hud = new HUD(this, new ShaderProgram(
                "HUD Shader",
                (ShaderType.VertexShader, "shaders/hud_vshader.vert"),
                (ShaderType.FragmentShader, "shaders/hud_fshader.frag")
            ));
            _scene = new Scene(this, new ShaderProgram(
                "Scene Shader",
                (ShaderType.VertexShader, "shaders/scene_vshader.vert"),
                (ShaderType.FragmentShader, "shaders/scene_fshader.frag")
            ))
            {
                Camera = new PlayerCamera(),
            };
            _scene.Lights.LightData[0] = Light.CreateEnvironmentLight(new Vector3(3, 2, 10), Color.Magenta);
            _scene.Lights.LightData[1] = Light.CreatePointLight(new Vector3(0, 0, 2), Color.Wheat);

            BuildScene();
            ResetCamera();

            CursorVisible = false;
            VSync = VSyncMode.Adaptive;
            WindowState = WindowState.Maximized;
        }

        internal void BuildScene()
        {
            for (int i = 0; i < 4; ++i)
                for (int j = 0; j < 4; ++j)
                    if ((i == 0) || (i == 3) || (j == 0) || (j == 3))
                    {
                        var block = _scene.World[1 - i, j + 1, 0];

                        block.Material = BlockMaterial.Stone;
                    }

            int side = 6;

            for (int i = -side; i <= side; ++i)
                for (int j = -side; j <= side; ++j)
                    _scene.World[i, (int)(Sin((i + j) / 3) * 1.5), j].Material = BlockMaterial.Grass;

            // _scene.World.PlaceCustomBlock(4, 1, 0, WavefrontFile.FromPath("resources/center-piece.obj"));
        }

        private void ResetCamera()
        {
            Camera.MoveTo(new Vector3(0, 2, 0));
            Camera.ResetZoom();
            Camera.ResetAngles();
            Camera.VerticalAngle = -.5f;
        }

        public override void Exit()
        {
            _scene.Dispose();
            _hud.Dispose();

            ShaderProgram.DisposeAll();

            base.Exit();
        }

        protected override void OnResize(EventArgs e) => GL.Viewport(0, 0, Width, Height);

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            HandleInput();

            Time += e.Time;

            _hud.Update(Time, e.Time);
            _scene.Update(Time, e.Time, (float)Width / Height);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.ClearColor(new Color4(.2f, .3f, .5f, 1f));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit | ClearBufferMask.AccumBufferBit);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            _scene.Render(Time, Width, Height);
            _hud.Render(Time, Width, Height);

            SwapBuffers();
        }

        internal void HandleInput()
        {
            KeyboardState kstate = Keyboard.GetState();
            MouseState mstate = Mouse.GetState();
            int δx = _mousex - mstate.X;
            int δy = _mousey - mstate.Y;
            float speed = .1f;

            if (kstate.IsKeyDown(Key.AltLeft))
                speed *= 10;
            if (kstate.IsKeyDown(Key.ShiftLeft))
                speed /= 10;

            if (kstate.IsKeyDown(Key.Escape))
                Exit();
            if (kstate.IsKeyDown(Key.Number1))
                _scene.Program.PolygonMode = PolygonMode.Point;
            if (kstate.IsKeyDown(Key.Number2))
                _scene.Program.PolygonMode = PolygonMode.Line;
            if (kstate.IsKeyDown(Key.Number3))
                _scene.Program.PolygonMode = PolygonMode.Fill;
            if (kstate.IsKeyDown(Key.W))
                Camera.MoveBackwards(speed);
            if (kstate.IsKeyDown(Key.S))
                Camera.MoveForwards(speed);
            if (kstate.IsKeyDown(Key.A))
                Camera.MoveLeft(speed);
            if (kstate.IsKeyDown(Key.D))
                Camera.MoveRight(speed);
            if (kstate.IsKeyDown(Key.Space))
                Camera.MoveUp(speed);
            if (kstate.IsKeyDown(Key.ControlLeft))
                Camera.MoveDown(speed);
            if (kstate.IsKeyDown(Key.Q))
                --Camera.ZoomFactor;
            if (kstate.IsKeyDown(Key.E))
                ++Camera.ZoomFactor;
            if (kstate.IsKeyDown(Key.R))
                ResetCamera();

            Camera.RotateRight(δx * .075f);
            Camera.RotateUp(δy * .075f);

            _mousex = mstate.X;
            _mousey = mstate.Y;

            System.Windows.Forms.Cursor.Position = new Point(X + (Width / 2), Y + (Height / 2));
        }
    }
}
