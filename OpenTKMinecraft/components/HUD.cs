using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Text;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using OpenTK;

using OpenTKMinecraft.Native;

using SDI = System.Drawing.Imaging;

namespace OpenTKMinecraft.Components
{
    using static SHADER_BIND_LOCATIONS;
    using static Math;
    using OpenTKMinecraft.Properties;

    public unsafe sealed class HUD
        : IVisuallyUpdatable
        , IUpdatable
        , IDisposable
        , IShaderTarget
    {
        private static readonly Font _fontfg = new Font("Consolas", 16, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Pen _penpausedfg = new Pen(Color.FromArgb(0x7f808080), 3);
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
        private bool _disposed;

        public int RenderedGDIHUDTextureID { get; private set; }
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
                if (UseHUD)
                {
                    HUDData _dat = Data;

                    if (((_dat.Width > 0) || (_dat.Height > 0)) && !_dat.Paused)
                    {
                        Bitmap bmp = new Bitmap(_dat.Width, _dat.Height, SDI.PixelFormat.Format32bppPArgb);
                        Pen pen = _dat.Paused ? _penpausedfg : _penfg;

                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.CompositingMode = CompositingMode.SourceOver;
                            g.CompositingQuality = CompositingQuality.HighQuality;
                            g.InterpolationMode = InterpolationMode.HighQualityBilinear;

                            int w = bmp.Width;
                            int h = bmp.Height;
                            float w2 = w / 2f;
                            float h2 = h / 2f;
                            const int crosshairsz = 15;
                            const int circlesz = 40;

                            g.DrawLine(pen, w2 - crosshairsz, h2, w2 + crosshairsz, h2);
                            g.DrawLine(pen, w2, h2 - crosshairsz, w2, h2 + crosshairsz);
                            g.DrawEllipse(pen, w2 - circlesz, h2 - circlesz, circlesz * 2, circlesz * 2);
                            g.DrawLine(pen, w2 - crosshairsz - circlesz, h2, w2 - circlesz, h2);
                            g.DrawLine(pen, w2 + crosshairsz + circlesz, h2, w2 + circlesz, h2);
                            g.DrawLine(pen, w2, h2 - crosshairsz - circlesz, w2, h2 - circlesz);
                            g.DrawLine(pen, w2, h2 + crosshairsz + circlesz, w2, h2 + circlesz);

                            string str_h = $"{_dat.HDirection * 180 / PI:F2}°";
                            string str_v = $"{_dat.VDirection * 90,6:F2}°";

                            g.DrawString($"[{_dat.Position.X:F2}, {_dat.Position.Y:F2}, {_dat.Position.Z:F2}]", _fontfg, Brushes.WhiteSmoke, 20, 20);
                            g.DrawString(str_h, _fontfg, Brushes.WhiteSmoke, w2 - g.MeasureString(str_h, _fontfg).Width / 2, 20);
                            g.DrawString(str_v, _fontfg, Brushes.WhiteSmoke, 20, h2);

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
                                        g.DrawLine(pen, 0, yoffs, len, yoffs);
                                        g.DrawLine(pen, w - len, yoffs, w, yoffs);
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
                                        g.DrawLine(pen, _x, 0, _x, len);
                                        g.DrawLine(pen, _x, h - len, _x, h);
                                    }
                                }
                            }

                            //if (_dat.Anaglyph)
                            //    g.DrawImage(Resources.anaglyph, 0, 0);
                        }

                        LastHUD = bmp;
                    }
                }
                else
                    Thread.Sleep(100);
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

        public void Update(double time, double delta, float aspectratio)
        {
            PlayerCamera cam = Window.Camera;

            Data = new HUDData
            {
                Width = Window.Width,
                Height = Window.Height,
                Time = time,
                Delta = delta,
                Paused = Window.IsPaused,
                HDirection = cam.HorizontalAngle,
                VDirection = cam.VerticalAngle,
                Position = cam.Position,
                Anaglyph = cam.IsStereoscopic,
            };
        }

        public void Render(double time, float width, float height)
        {
            if (!UseHUD)
                return;

            Program.Use();

            // GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.Viewport(0, 0, (int)width, (int)height);
            GL.LineWidth(1);
            GL.PointSize(1);
            GL.VertexAttrib1(WINDOW_TIME, time);
            GL.Uniform1(WINDOW_WIDTH, width);
            GL.Uniform1(WINDOW_HEIGHT, height);
            GL.Uniform1(WINDOW_PAUSED, Window.IsPaused ? 1 : 0);

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
            }
        }

        internal void OnWindowResize(object sender, EventArgs e)
        {
            if ((Window.Width < 10) || (Window.Height < 10) || !UseHUD)
                return;

            GL.DeleteTexture(RenderedGDIHUDTextureID);

            OverlayInit();
        }

        public void Update(double time, double delta) => Update(time, delta, Window.Width / (float)Window.Height);
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
    }
}
