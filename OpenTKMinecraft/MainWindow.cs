using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Threading;
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
        public World World => _scene.Object.World;
        public PlayerCamera Camera => _scene.Object.Camera as PlayerCamera;
        public float MouseSensitivityFactor { set; get; } = 1;
        public double Time { get; private set; }
        public string[] Arguments { get; }

        private int _mousex, _mousey;
        public bool _paused;
        private AfterEffectShaderProgram<Scene> _scene;
        private HUD _hud;


        public MainWindow(string[] args)
            : base(1920, 1080, new GraphicsMode(new ColorFormat(8, 8, 8, 8), 16, 16, 4), nameof(MainWindow), GameWindowFlags.Default, DisplayDevice.Default, Program.GL_VERSION_MAJ, Program.GL_VERSION_MIN, GraphicsContextFlags.ForwardCompatible)
        {
            Arguments = args;
            MouseSensitivityFactor = 2;
            WindowBorder = WindowBorder.Resizable;
        }

        protected override void OnLoad(EventArgs e)
        {
            Closed += (s, a) => Exit();

            _hud = new HUD(this, new ShaderProgram(
                "HUD Shader",
                new[] { "HUD" },
                (ShaderProgramType.VertexShader, "shaders/hud.vert"),
                (ShaderProgramType.FragmentShader, "shaders/hud.frag")
            ));
            _scene = new AfterEffectShaderProgram<Scene>(
                new Scene(this, new ShaderProgram(
                    "Scene Shader",
                    new[] { "SCENE" },
                    (ShaderProgramType.VertexShader, "shaders/scene.vert"),
                    (ShaderProgramType.FragmentShader, "shaders/scene.frag")
                ))
                {
                    Camera = new PlayerCamera
                    {
                        IsStereoscopic = false,
                    },
                },
                new ShaderProgram(
                    "Scene Effect",
                    new[] { "EFFECT" },
                    (ShaderProgramType.VertexShader, "shaders/scene_effect.vert"),
                    (ShaderProgramType.FragmentShader, "shaders/scene_effect.frag")
                ),
                this
            );

            _scene.Object.Lights.Add(Light.CreateDirectionalLight(new Vector3(-1, -1, 0), Color.WhiteSmoke));
            _scene.Object.Lights.Add(Light.CreatePointLight(new Vector3(0, 0, 2), Color.Wheat, 10));

            BuildScene();
            ResetCamera();

            CursorVisible = false;
            VSync = VSyncMode.Off;
            WindowState = WindowState.Maximized;
        }

        internal void BuildScene()
        {
            World[0, 15, 0].Material = BlockMaterial.__DEBUG__;

            for (int i = 0; i < 4; ++i)
                for (int j = 0; j < 4; ++j)
                    if ((i == 0) || (i == 3) || (j == 0) || (j == 3))
                        World[1 - i, j + 1, 0].Material = ((i ^ j) & 1) != 0 ? BlockMaterial.Stone : BlockMaterial.Diamond;

            int side = 6; // 9

            for (int i = -side; i <= side; ++i)
                for (int j = -side; j <= side; ++j)
                {
                    int y = (int)(Sin((i + Sin(i) / 3 - j) / 3) * 1.5);

                    if ((i * i + j * j) < 15)
                    {
                        World[i, y, j].Material = BlockMaterial.Sand;
                        World[i, y - 1, j].Material = BlockMaterial.Grass;
                    }
                    else
                        World[i, y, j].Material = BlockMaterial.Grass;
                }

            (int xp, int yp) = (15, 15);

            // World[xp, 3, yp].Material = BlockMaterial.Glowstone;

            for (int i = -2; i <= 2; ++i)
                for (int j = -2; j <= 2; ++j)
                    if ((i >= -1) && (i < 2) && (j >= -1) && (j < 2))
                    {
                        World[xp + i, -1, yp + j].Material = BlockMaterial.Stone;

                        World[xp + i, 0, yp + j].Material = BlockMaterial.Water;
                        World[xp + i, 0, yp + j].Move(0, -.15f, 0);
                    }
                    else
                        World[xp + i, 0, yp + j].Material = BlockMaterial.Stone;

            // World.PlaceCustomBlock(4, 1, 0, WavefrontFile.FromPath("resources/center-piece.obj"));
        }

        private void ResetCamera()
        {
            Camera.MoveTo(new Vector3(0, 6, -8));
            Camera.ResetZoom();
            Camera.HorizontalAngle = (float)(PI / 2);
            Camera.VerticalAngle = -.25f;
            Camera.EyeSeparation = .1f;
            Camera.FocalDistance = 10f;
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

            if (_paused)
                return;

            Time += e.Time;

            _scene.Object.Lights[1].Position = Matrix3.CreateRotationY((float)Time) * new Vector3(0, 2, 4);

            _hud.Update(Time, e.Time);
            _scene.Object.Update(Time, e.Time, (float)Width / Height);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.ClearColor(new Color4(.2f, .3f, .5f, 1f));


            _scene.Render(Time, Width, Height);
            _hud.Render(Time, Width, Height);

            SwapBuffers();

            Title = $"{1 / e.Time:F2} FPS";
        }

        internal void HandleInput()
        {
            KeyboardState kstate = Keyboard.GetState();
            MouseState mstate = Mouse.GetState();
            int δx = _mousex - mstate.X;
            int δy = _mousey - mstate.Y;
            float speed = .1f;

            if (kstate.IsKeyDown(Key.P))
            {
                CursorVisible = (_paused = !_paused);

                Thread.Sleep(100);

                if (!_paused)
                {
                    _mousex = mstate.X;
                    _mousey = mstate.Y;
                }

                return;
            }
            else if (_paused)
                return;

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
                Camera.MoveForwards(speed);
            if (kstate.IsKeyDown(Key.S))
                Camera.MoveBackwards(speed);
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
            if (kstate.IsKeyDown(Key.Number4))
            {
                Camera.IsStereoscopic ^= true;

                Thread.Sleep(100);
            }
            if (kstate.IsKeyDown(Key.V))
            {
                VSync = VSync == VSyncMode.Off ? VSyncMode.On : VSyncMode.Off;

                Thread.Sleep(100);
            }

            if (Camera.IsStereoscopic)
            {
                if (kstate.IsKeyDown(Key.PageUp))
                    Camera.FocalDistance *= 1.1f;
                if (kstate.IsKeyDown(Key.PageDown))
                    Camera.FocalDistance /= 1.1f;
                if (kstate.IsKeyDown(Key.Home))
                    Camera.EyeSeparation += .005f;
                if (kstate.IsKeyDown(Key.End))
                    Camera.EyeSeparation -= .005f;
            }

            // CameraStereoMode

            Camera.RotateRight(δx * .2f * MouseSensitivityFactor);
            Camera.RotateUp(δy * .2f * MouseSensitivityFactor);

            _mousex = mstate.X;
            _mousey = mstate.Y;

            System.Windows.Forms.Cursor.Position = new Point(X + (Width / 2), Y + (Height / 2));
        }
    }
}
