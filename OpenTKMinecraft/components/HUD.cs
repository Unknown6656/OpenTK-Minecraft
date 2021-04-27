using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Drawing.Text;
using System.Threading;
using System.Drawing;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using OpenTK;

using OpenTKMinecraft.Components.UI;
using OpenTKMinecraft.Properties;
using OpenTKMinecraft.Native;

using SDI = System.Drawing.Imaging;

namespace OpenTKMinecraft.Components
{
    using static SHADER_BIND_LOCATIONS;
    using static Math;

    public unsafe sealed class HUD
        : IVisuallyUpdatable
        , IUpdatable
        , IDisposable
        , IShaderTarget
    {
        private static readonly Font _fontfg = new Font("Consolas", 16, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Pen _penfg = new Pen(Color.WhiteSmoke, 3);
        private static readonly Vector4[] _vertices = new[]
        {
            new Vector4(-1, -1, 0, 1),
            new Vector4(1, -1, 0, 1),
            new Vector4(-1, 1, 0, 1),
            new Vector4(-1, 1, 0, 1),
            new Vector4(1, -1, 0, 1),
            new Vector4(1, 1, 0, 1),
        };
        private readonly int _vertexarr, _vertexbuff, _hudtex;
        private readonly Thread _paintthread;
        private Bitmap _todisp;
        private bool _disposed;

        public int RenderedGDIHUDTextureID { get; private set; }
        public HUDControl PauseScreen { set; get; }
        public Bitmap LastHUD { get; private set; }
        public HUDData Data { get; private set; }
        public bool UseHUD { get; set; } = true;
        public ShaderProgram Program { get; }
        public MainWindow Window { get; }


        public HUD(MainWindow win, ShaderProgram prog)
        {
            Window = win;
            Program = prog;
            Program.PolygonMode = PolygonMode.Fill;

            _vertexarr = GL.GenVertexArray();
            _vertexbuff = GL.GenBuffer();

            GL.BindVertexArray(_vertexarr);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexbuff);
            GL.NamedBufferStorage(_vertexbuff, sizeof(Vector4) * _vertices.Length, _vertices, BufferStorageFlags.MapWriteBit);
            GL.VertexArrayAttribBinding(_vertexarr, HUD_VERTEX_POSITION, 0);
            GL.EnableVertexArrayAttrib(_vertexarr, HUD_VERTEX_POSITION);
            GL.VertexArrayAttribFormat(_vertexarr, HUD_VERTEX_POSITION, 4, VertexAttribType.Float, false, 0);
            GL.VertexArrayVertexBuffer(_vertexarr, 0, _vertexbuff, IntPtr.Zero, sizeof(Vector4));

            OverlayInit();

            _hudtex = GL.GetUniformLocation(Program.ID, "overlayTexture");
            _paintthread = new Thread(PaintHUD);
            _paintthread.Start();

            Window.Resize += OnWindowResize;
        }

        private void PaintHUD()
        {
            while (!_disposed)
            {
                HUDData _dat = Data;

                if ((UseHUD || _dat.Paused) && ((_dat.Width > 0) || (_dat.Height > 0)))
                {
                    Bitmap bmp = new Bitmap(_dat.Width, _dat.Height, SDI.PixelFormat.Format32bppPArgb);

                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        void DrawCenteredString(string s, Font f, Brush b, float x, float y)
                        {
                            SizeF sz = g.MeasureString(s, f);

                            g.DrawString(s, f, b, x - sz.Width / 2, y - sz.Height / 2);
                        }

                        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.CompositingMode = CompositingMode.SourceOver;
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBilinear;

                        if (UseHUD)
                        {
                            int w = bmp.Width;
                            int h = bmp.Height;
                            float w2 = w / 2f;
                            float h2 = h / 2f;
                            const int crosshairsz = 15;
                            const int circlesz = 40;

                            g.DrawLine(_penfg, w2 - crosshairsz, h2, w2 + crosshairsz, h2);
                            g.DrawLine(_penfg, w2, h2 - crosshairsz, w2, h2 + crosshairsz);
                            g.DrawEllipse(_penfg, w2 - circlesz, h2 - circlesz, circlesz * 2, circlesz * 2);
                            g.DrawLine(_penfg, w2 - crosshairsz - circlesz, h2, w2 - circlesz, h2);
                            g.DrawLine(_penfg, w2 + crosshairsz + circlesz, h2, w2 + circlesz, h2);
                            g.DrawLine(_penfg, w2, h2 - crosshairsz - circlesz, w2, h2 - circlesz);
                            g.DrawLine(_penfg, w2, h2 + crosshairsz + circlesz, w2, h2 + circlesz);

                            g.DrawString($"[{_dat.Position.X:F2}, {_dat.Position.Y:F2}, {_dat.Position.Z:F2}]", _fontfg, Brushes.WhiteSmoke, 20, 20);
                            DrawCenteredString($"{_dat.HDirection * 180 / PI:F2}°", _fontfg, Brushes.WhiteSmoke, w2, 30);
                            g.DrawString($"{_dat.VDirection * 90,6:F2}°", _fontfg, Brushes.WhiteSmoke, 20, h2);

                            const float bardist = 20;

                            { // y scale
                                float ybar = h2 + (float)_dat.VDirection * h2;
                                int ybarcnt = (int)(h2 / bardist) + 1;

                                for (int i = -ybarcnt; i <= ybarcnt + 1; ++i)
                                {
                                    float yoffs = ybar + i * bardist;
                                    int len = Abs(i % 4) == 1 ? 20 : Abs(i % 2) == 0 ? 10 : 5;

                                    if ((yoffs >= bardist) && (yoffs < h))
                                    {
                                        g.DrawLine(_penfg, 0, yoffs, len, yoffs);
                                        g.DrawLine(_penfg, w - len, yoffs, w, yoffs);
                                    }
                                }
                            }
                            { // x scale
                                float xbar = w2 + (float)((_dat.HDirection % (2 * PI)) * w2 / PI);
                                int xbarcnt = (int)(w2 / bardist) + 1;

                                float xoffs = w2 + ((xbar - w2) % (bardist * 4));

                                for (int i = -xbarcnt; i <= xbarcnt + 1; ++i)
                                {
                                    float _x = xoffs + (i * bardist);
                                    int len = Abs(i % 4) == 1 ? 20 : Abs(i % 2) == 0 ? 10 : 5;

                                    if (_x > bardist)
                                    {
                                        g.DrawLine(_penfg, _x, 0, _x, len);
                                        g.DrawLine(_penfg, _x, h - len, _x, h);
                                    }
                                }
                            }

                            //if (_dat.Anaglyph)
                            //    g.DrawImage(Resources.anaglyph, 0, 0);
                        }

                        if (_dat.Paused)
                        {
                            bool hover = PauseScreen?.Render(g, _dat.Mouse) ?? false;

                            g.DrawImage(hover ? Resources.aero_hand : Resources.aero_arrow, _dat.Mouse.X - 15, _dat.Mouse.Y, 32, 32);
                        }
                    }

                    _todisp = LastHUD;
                    LastHUD = bmp;
                }
                else
                    Thread.Sleep(100);
            }
        }

        private void OverlayInit()
        {
            RenderedGDIHUDTextureID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, RenderedGDIHUDTextureID);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Window.Width, Window.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new[] { (int)TextureMagFilter.Linear });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new[] { (int)TextureMinFilter.Linear });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new[] { (int)TextureWrapMode.ClampToBorder });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new[] { (int)TextureWrapMode.ClampToBorder });
        }

        public void Update(double time, double delta) => Update(time, delta, Window.ClientRectangle.Width / (float)Window.ClientRectangle.Height);

        public void Update(double time, double delta, float aspectratio)
        {
            PlayerCamera cam = Window.Camera;
            Point mouse = Cursor.Position;

            Data = new HUDData
            {
                Width = Window.ClientRectangle.Width,
                Height = Window.ClientRectangle.Height,
                Time = time,
                Delta = delta,
                Paused = Window.IsPaused,
                UseEffect = Window.Scene.UsePostEffect,
                HDirection = cam.HorizontalAngle,
                VDirection = cam.VerticalAngle,
                Position = cam.Position,
                Anaglyph = cam.IsStereoscopic,
                Mouse = new HUDMouseData
                {
                    Pressed = Mouse.GetState().LeftButton == OpenTK.Input.ButtonState.Pressed,
                    X = mouse.X - Window.ClientRectangle.Width + Window.Width + Window.X,
                    Y = mouse.Y - Window.ClientRectangle.Height + Window.Height + Window.Y,
                }
            };
        }

        public void Render(double time, float width, float height)
        {
            HUDData _dat = Data;

            if (!UseHUD && !_dat.Paused)
                return;

            Program.Use();
            Update(time, 0);

            // GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.Viewport(0, 0, (int)width, (int)height);
            GL.LineWidth(1);
            GL.PointSize(1);
            GL.VertexAttrib1(WINDOW_TIME, time);
            GL.Uniform1(WINDOW_WIDTH, width);
            GL.Uniform1(WINDOW_HEIGHT, height);
            GL.Uniform1(WINDOW_PAUSED, _dat.Paused && _dat.UseEffect ? 1 : 0);
            GL.Uniform1(HUD_INUSE, UseHUD ? 1 : 0);

            if (LastHUD is Bitmap b)
            {
                int w = b.Width;
                int h = b.Height;
                SDI.BitmapData dat = b.LockBits(new Rectangle(0, 0, w, h), SDI.ImageLockMode.ReadOnly, b.PixelFormat);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, RenderedGDIHUDTextureID);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0, PixelFormat.Bgra, PixelType.UnsignedByte, dat.Scan0);
                GL.Uniform1(_hudtex, 0);

                b.UnlockBits(dat);

                if (_dat.Paused && PauseScreen is HUDControl c)
                    GL.Uniform4(HUD_EXCLUDE_EFFECT, new Vector4(
                        (c.AbsoluteX - 1) / _dat.Width,
                        (c.AbsoluteY - 1 ) / _dat.Height,
                        (c.Width + 2) / _dat.Width,
                        (c.Height + 2) / _dat.Height
                    ));
                else
                    GL.Uniform4(HUD_EXCLUDE_EFFECT, Vector4.Zero);

                _todisp?.Dispose();
                _todisp = null;
            }

            GL.BindVertexArray(_vertexarr);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertices.Length);
            GL.Disable(EnableCap.Blend);
        }

        public void Dispose()
        {
            lock (_vertices)
            {
                if (_disposed)
                    return;
                else
                    _disposed = true;

                Program.Dispose();

                GL.DeleteTexture(RenderedGDIHUDTextureID);
                GL.DeleteVertexArray(_vertexarr);
                GL.DeleteBuffer(_vertexbuff);

                _todisp?.Dispose();
                _todisp = null;

                LastHUD?.Dispose();
                LastHUD = null;
            }
        }

        internal void OnWindowResize(object sender, EventArgs e)
        {
            if ((Window.Width < 10) || (Window.Height < 10) || !UseHUD)
                return;

            GL.DeleteTexture(RenderedGDIHUDTextureID);

            OverlayInit();
        }
    }

    public struct HUDData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public double Delta { get; set; }
        public double Time { get; set; }
        public bool Paused { get; set; }
        public bool Anaglyph { get; set; }
        public Vector3 Position { get; set; }
        public double VDirection { get; set; }
        public double HDirection { get; set; }
        public bool UseEffect { get; set; }
        public HUDMouseData Mouse { get; set; }
}

    public struct HUDMouseData
    {
        public float X { set; get; }
        public float Y { set; get; }
        public bool Pressed { set; get; }
    }
}
