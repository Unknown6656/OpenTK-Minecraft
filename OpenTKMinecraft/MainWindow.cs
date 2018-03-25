using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Text;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK;

using OpenTKMinecraft.Components;
using OpenTKMinecraft.Minecraft;
using OpenTKMinecraft.Utilities;

using static System.Math;

namespace OpenTKMinecraft
{
    public sealed unsafe class MainWindow
        : GameWindow
    {
        internal const int KEYBOARD_TOGGLE_DELAY = 200;

        public World World => Scene.Object.World;
        public PlayerCamera Camera => Scene.Object.Camera as PlayerCamera;
        public PostEffectShaderProgram<Scene> Scene { private set; get; }
        public HUD HUD { private set; get; }

        public float MouseSensitivityFactor { set; get; } = 1;
        public bool IsPaused { private set; get; }
        public double PausedTime { private set; get; }
        public double Time { private set; get; }
        public string[] Arguments { get; }

        private int _mousex, _mousey;
        private float _mousescroll;


        public MainWindow(string[] args)
            : base(1920, 1080, new GraphicsMode(new ColorFormat(32, 32, 32, 32), 32, 0, 4), nameof(MainWindow), GameWindowFlags.Default, DisplayDevice.Default, Program.GL_VERSION_MAJ, Program.GL_VERSION_MIN, GraphicsContextFlags.ForwardCompatible)
        {
            Arguments = args;
            MouseSensitivityFactor = 2;
            WindowBorder = WindowBorder.Resizable;
            WindowState = WindowState.Normal;
        }

        protected override void OnLoad(EventArgs e)
        {
            Program.spscreen.Text = ("Loading Shaders ...", "");

            Closed += (s, a) => Exit();

            Scene = new PostEffectShaderProgram<Scene>(
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
            )
            {
                //UsePostEffect = false
            };
            HUD = new HUD(this, new ShaderProgram(
                "HUD Shader",
                new[] { "HUD" },
                (ShaderProgramType.VertexShader, "shaders/hud.vert"),
                (ShaderProgramType.FragmentShader, "shaders/hud.frag")
            ));

            Scene.Object.AddLight(Light.CreateDirectionalLight(new Vector3(-1, -1, 0), Color.WhiteSmoke));
            Scene.Object.AddLight(Light.CreatePointLight(new Vector3(0, 0, 2), Color.Wheat, 10));

            BuildScene();
            ResetCamera();

            CursorVisible = false;
            VSync = VSyncMode.Off;
            WindowState = WindowState.Maximized;

            Program.spscreen.Text = ("Finished.", "");
            Thread.Sleep(500);
            Program.spscreen.Close();

            ShowHelp();
        }

        internal void BuildScene()
        {
            Program.spscreen.Text = ("Loading World ...", "Building scene ...");

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

            // Scene.World[xp, 3, yp].Material = BlockMaterial.Glowstone;

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

            // Scene.World.PlaceCustomBlock(4, 1, 0, WavefrontFile.FromPath("resources/center-piece.obj"));
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
            Scene.Dispose();
            HUD.Dispose();

            ShaderProgram.DisposeAll();

            base.Exit();
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);

            Scene.OnWindowResize(this, e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            HandleInput();

            if (IsPaused)
                PausedTime += e.Time;
            else
                Time += e.Time;

            HUD.Update(Time + PausedTime, e.Time);

            if (IsPaused)
                return;

            Scene.Object.Lights[1].Position = Matrix3.CreateRotationY((float)Time) * new Vector3(0, 2, 4);
            Scene.Update(Time, e.Time, (float)Width / Height);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            Scene.Render(Time, Width, Height);
            HUD.Render(Time + PausedTime, Width, Height);

            SwapBuffers();
        }

        internal void HandleInput()
        {
            KeyboardState kstate = Keyboard.GetState();
            MouseState mstate = Mouse.GetState();
            int δx = _mousex - mstate.X;
            int δy = _mousey - mstate.Y;
            float δs = _mousescroll - mstate.WheelPrecise;
            float speed = .075f;

            if (kstate.IsKeyDown(Key.H))
            {
                ShowHelp();

                return;
            }

            if (kstate.IsKeyDown(Key.P))
            {
                CursorVisible = (IsPaused = !IsPaused);

                Thread.Sleep(KEYBOARD_TOGGLE_DELAY);

                if (!IsPaused)
                {
                    _mousex = mstate.X;
                    _mousey = mstate.Y;
                    _mousescroll = mstate.WheelPrecise;
                }

                return;
            }
            else if (IsPaused)
                return;

            if (kstate.IsKeyDown(Key.ShiftLeft))
                speed /= 10;
            if (kstate.IsKeyDown(Key.AltLeft))
            {
                speed *= 3;

                Scene.EdgeBlurMode = EdgeBlurMode.RadialBlur;
            }
            else
                Scene.EdgeBlurMode = EdgeBlurMode.BoxBlur;

            if (kstate.IsKeyDown(Key.Escape))
                Exit();
            if (kstate.IsKeyDown(Key.Number1))
                Scene.Object.Program.PolygonMode = PolygonMode.Point;
            if (kstate.IsKeyDown(Key.Number2))
                Scene.Object.Program.PolygonMode = PolygonMode.Line;
            if (kstate.IsKeyDown(Key.Number3))
                Scene.Object.Program.PolygonMode = PolygonMode.Fill;
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
                if (Camera.IsStereoscopic ^= true)
                    Scene.UsePostEffect = false;

                Thread.Sleep(KEYBOARD_TOGGLE_DELAY);
            }
            if (kstate.IsKeyDown(Key.Number5))
            {
                if (Scene.UsePostEffect ^= true)
                    Camera.IsStereoscopic = false;

                Thread.Sleep(KEYBOARD_TOGGLE_DELAY);
            }
            if (kstate.IsKeyDown(Key.Number6))
            {
                HUD.UseHUD ^= true;

                Thread.Sleep(KEYBOARD_TOGGLE_DELAY);
            }
            if (kstate.IsKeyDown(Key.V))
            {
                VSync = VSync == VSyncMode.Off ? VSyncMode.On : VSyncMode.Off;

                Thread.Sleep(KEYBOARD_TOGGLE_DELAY);
            }
            if (kstate.IsKeyDown(Key.X))
            {
                Scene.Effect++;
                Scene.Effect = (PredefinedShaderEffect)((int)Scene.Effect % ((Enum.GetValues(typeof(PredefinedShaderEffect)) as int[]).Max() + 1));

                Thread.Sleep(KEYBOARD_TOGGLE_DELAY);
            }
            if (kstate.IsKeyDown(Key.F))
                using (Bitmap bmp = new Bitmap(Width, Height))
                {
                    System.Drawing.Imaging.BitmapData data = bmp.LockBits(new Rectangle(0, 0, Width, Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                    GL.ReadPixels(0, 0, Width, Height, PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);

                    bmp.UnlockBits(data);
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    bmp.Save("framebuffer.png");
                }

            if (Camera.IsStereoscopic)
            {
                Camera.FocalDistance *= (float)Pow(1.3, δs);

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
            _mousescroll = mstate.WheelPrecise;

            System.Windows.Forms.Cursor.Position = new Point(X + (Width / 2), Y + (Height / 2));
        }

        public static void ShowHelp() => MessageBox.Show(@"
---------------- KEYBOARD SHORTCUTS ----------------
[P] Pause
[ESC] Exit
[H] Show this help window
[F] Save screenshot to 'framebuffer.png'

[W] Move forwards
[A] Move left
[D] Move right
[S] Move backwards
[SPACE] Move up
[CTRL] Move down
[ALT] Fast movement
[SHIFT] Slow movement
[Q] Zoom out
[E] Zoom in

[1] Display Points
[2] Display Lines
[3] Display Faces
[4] Toggle Stereoscopic display
[5] Toggle PostProcessing effects
[6] Toggle HUD
[P.Up] Increase focal distance (Stereoscopic only)
[P.Down] Decrease focal distance (Stereoscopic only)
[Home] Increase eye separation (Stereoscopic only)
[End] Derease eye separation (Stereoscopic only)

[X] Cycle visual effects
[V] Toggle VSync
".Trim());
    }
}
