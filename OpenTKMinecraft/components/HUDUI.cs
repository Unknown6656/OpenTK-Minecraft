using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System;


namespace OpenTKMinecraft.Components.UI
{
    using static System.Math;


    internal interface IHUDClickable
    {
    }

    public abstract class HUDControl
    {
        private float _cx, _cy, _w, _h;
        private HUDControl _par;
        private bool _press;


        public List<HUDControl> Childern { get; } = new List<HUDControl>();

        public Color ForegroundColor { get; set; }

        public Color BackgroundColor { get; set; }

        protected Color PressedBackgroundColor
        {
            get
            {
                const float fac = 0.6f;
                Color c = BackgroundColor;

                return Color.FromArgb(c.A, (int)Min(255, c.R * fac), (int)Min(255, c.G * fac), (int)Min(255, c.B * fac));
            }
        }

        protected Color HoveredBackgroundColor
        {
            get
            {
                const float fac = 0.8f;
                Color c = BackgroundColor;

                return Color.FromArgb(c.A, (int)Min(255, c.R * fac), (int)Min(255, c.G * fac), (int)Min(255, c.B * fac));
            }
        }

        protected Color DisabledBackgroundColor
        {
            get
            {
                const float fac = 1.2f;
                Color c = BackgroundColor;

                return Color.FromArgb(c.A, (int)Min(255, c.R * fac), (int)Min(255, c.G * fac), (int)Min(255, c.B * fac));
            }
        }

        protected Color DisabledForegroundColor
        {
            get
            {
                const float fac = 1.2f;
                Color c = ForegroundColor;

                return Color.FromArgb(c.A, (int)Min(255, c.R * fac), (int)Min(255, c.G * fac), (int)Min(255, c.B * fac));
            }
        }

        public RectangleF RectangleF => new RectangleF(AbsoluteX, AbsoluteY, Width, Height);

        public Rectangle Rectangle => new Rectangle((int)AbsoluteX, (int)AbsoluteY, (int)Width, (int)Height);

        public HUDControl Parent
        {
            internal set
            {
                _par?.Childern?.Remove(this);
                (_par = value)?.Childern?.Add(this);
            }
            get => _par;
        }

        public float AbsoluteX => CenterX - (Width / 2) + (Parent?.AbsoluteX ?? 0f);

        public float AbsoluteY => CenterY - (Height / 2) + (Parent?.AbsoluteY ?? 0f);

        public float CenterX
        {
            set => _cx = Max(Height / 2, Min(value, (Parent?.Height ?? float.MaxValue) - (Height / 2)));
            get => _cx;
        }

        public float CenterY
        {
            set => _cy = Max(Width / 2, Min(value, (Parent?.Width ?? float.MaxValue) - (Width / 2)));
            get => _cy;
        }

        public float Height
        {
            set => _h = Min(Max(value, 10), Parent?.Width ?? float.MaxValue);
            get => _h;
        }

        public float Width
        {
            set => _w = Min(Max(value, 10), Parent?.Width ?? float.MaxValue);
            get => _w;
        }

        public float Left => CenterX - Width / 2;

        public float Top => CenterY - Height / 2;


        public HUDControl(HUDControl parent)
        {
            ForegroundColor = Color.WhiteSmoke;
            BackgroundColor = Color.DarkGray;
            Width = 100;
            Height = 50;
            Parent = parent;
        }

        public bool Render(Graphics g, HUDMouseData mouse)
        {
            bool cont = Contains(mouse.X, mouse.Y);
            bool pressed = cont && mouse.Pressed;

            if (_press && !pressed)
                OnClick();

            _press = pressed;

            OnRender(g, pressed, cont);

            cont &= this is IHUDClickable;

            foreach (HUDControl c in Childern)
                cont |= c?.Render(g, mouse) ?? false;

            return cont;
        }

        public bool Contains(float absx, float absy) => RectangleF.Contains(absx, absy);

        protected abstract void OnRender(Graphics g, bool mousedown, bool mousehover);

        protected virtual void OnClick()
        {
        }
    }

    public abstract class HUDTextualControl
        : HUDControl
    {
        private Font _font;


        public string Text { set; get; }

        public float FontSizePt
        {
            set
            {
                if (value >= 6)
                    _font = new Font(_font.FontFamily.Name, value, _font.Style, GraphicsUnit.Point);
            }
            get => _font.SizeInPoints;
        }

        public FontStyle FontStyle
        {
            set => _font = new Font(_font.FontFamily.Name, _font.SizeInPoints, value, GraphicsUnit.Point);
            get => _font.Style;
        }

        public string FontFamily
        {
            set
            {
                if (value is string s)
                    _font = new Font(s, _font.SizeInPoints, _font.Style, GraphicsUnit.Point);
            }
            get => _font.FontFamily.Name;
        }

        public Font Font
        {
            get => _font;
            set
            {
                if (value is Font f)
                    _font = f;
            }
        }

        public HUDTextualControl(HUDControl parent)
            : base(parent) => _font = new Font("Consolas", 14, FontStyle.Regular, GraphicsUnit.Point);
    }

    public class HUDWindow
        : HUDTextualControl
    {
        private float _pad;

        public float Padding
        {
            set => _pad = Max(0, Min(Width / 2, Max(Height / 2, value)));
            get => _pad;
        }


        public HUDWindow(HUDControl parent)
            : base(parent)
        {
        }

        protected override void OnRender(Graphics g, bool mousedown, bool mousehover)
        {
            g.FillRectangle(new SolidBrush(BackgroundColor), RectangleF);
            g.DrawRectangle(new Pen(ForegroundColor), Rectangle);

            if (Text is string s)
            {
                float mw = Width - (2 * Padding);
                SizeF sz = g.MeasureString(s, Font, (int)mw);

                g.DrawString(s, Font, new SolidBrush(ForegroundColor), new RectangleF((Width - sz.Width) / 2 + AbsoluteX, Padding + AbsoluteY, mw, sz.Height));
            }
        }

        public T AddFill<T>(T control, float center_y)
            where T : HUDControl
        {
            if (control != null)
            {
                if (!Childern.Contains(control))
                    Childern.Add(control);

                control.Parent = this;
                control.CenterY = center_y;
                control.CenterX = Width / 2;
                control.Width = Width - 2 * Padding;
            }

            return control;
        }
    }

    public class HUDLabel
        : HUDTextualControl
    {
        public HUDLabel(HUDControl parent)
            : base(parent) => BackgroundColor = Color.Transparent;

        protected override void OnRender(Graphics g, bool mousedown, bool mousehover)
        {
            g.FillRectangle(new SolidBrush(BackgroundColor), RectangleF);

            if (Text is string s)
            {
                SizeF sz = g.MeasureString(s, Font, (int)Width);

                g.DrawString(s, Font, new SolidBrush(ForegroundColor), new RectangleF((Width - sz.Width) / 2 + AbsoluteX, (Height - sz.Height) / 2 + AbsoluteY, sz.Width, sz.Height));
            }
        }
    }

    public class HUDButton
        : HUDTextualControl
        , IHUDClickable
    {
        public bool IsEnabled { set; get; }

        public event EventHandler Clicked;


        public HUDButton(HUDControl parent)
            : base(parent) => IsEnabled = true;

        protected override void OnRender(Graphics g, bool mousedown, bool mousehover)
        {
            Color bg = IsEnabled ? mousedown ? PressedBackgroundColor : mousehover ? HoveredBackgroundColor : BackgroundColor : DisabledBackgroundColor;
            Color fg = IsEnabled ? ForegroundColor : DisabledForegroundColor;

            g.FillRectangle(new SolidBrush(bg), RectangleF);
            g.DrawRectangle(new Pen(fg), Rectangle);

            if (Text is string s)
            {
                SizeF sz = g.MeasureString(s, Font, (int)Width);

                g.DrawString(s, Font, new SolidBrush(fg), new RectangleF((Width - sz.Width) / 2 + AbsoluteX, (Height - sz.Height) / 2 + AbsoluteY, sz.Width, sz.Height));
            }
        }

        protected override void OnClick()
        {
            if (IsEnabled)
                Clicked?.Invoke(this, EventArgs.Empty);
        }
    }

    public class HUDCheckbox
        : HUDTextualControl
        , IHUDClickable
    {
        public bool IsChecked { set; get; }
        public bool IsEnabled { set; get; }

        public event EventHandler<bool> StateChanged;


        public HUDCheckbox(HUDControl parent)
            : base(parent) => IsEnabled = true;

        protected override void OnRender(Graphics g, bool mousedown, bool mousehover)
        {
            Color bg = IsEnabled ? mousedown ? PressedBackgroundColor : mousehover ? HoveredBackgroundColor : BackgroundColor : DisabledBackgroundColor;
            Color fg = IsEnabled ? ForegroundColor : DisabledForegroundColor;

            float h = Height / 2 - 16;

            g.FillRectangle(new SolidBrush(bg), 2 + AbsoluteX, h + AbsoluteY, 32, 32);
            g.DrawRectangle(new Pen(fg), 2 + AbsoluteX, h + AbsoluteY, 32, 32);

            if (IsChecked)
                g.FillRectangle(new SolidBrush(fg), 10 + AbsoluteX, h + AbsoluteY + 10, 16, 16);

            if (Text is string s)
            {
                SizeF sz = g.MeasureString(s, Font, (int)Width - 36);

                g.DrawString(s, Font, new SolidBrush(fg), new RectangleF(36 + AbsoluteX, AbsoluteY, sz.Width, sz.Height));
            }
        }

        protected override void OnClick()
        {
            if (IsEnabled)
            {
                IsChecked ^= true;
                StateChanged?.Invoke(this, IsChecked);
            }
        }
    }
}
