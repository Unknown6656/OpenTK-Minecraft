using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System;

namespace OpenTKMinecraft.Components.UI
{
    using static Math;


    public interface IHUDClickable
    {
    }

    public interface IHUDPaddable
    {
        float Padding { set; get; }
    }

    public abstract class HUDControl
    {
        private float _cx, _cy, _w, _h;
        private HUDControl _par;
        private bool _press;

        public event EventHandler<float> WidthChanged;
        public event EventHandler<float> HeightChanged;


        public Action<HUDControl> BeforeRender { set; private get; }

        public Action<HUDControl> AfterRender { set; private get; }

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

        // [Obsolete("Use `CenterY` instead")]
        public float Top
        {
            set => CenterY = value + (Height / 2);
            get => CenterY - (Height / 2);
        }

        // [Obsolete("Use `CenterX` instead")]
        public float Left
        {
            set => CenterX = value + (Width / 2);
            get => CenterX - (Width / 2);
        }

        public float AbsoluteX => CenterX - (Width / 2) + (Parent?.AbsoluteX ?? 0f);

        public float AbsoluteY => CenterY - (Height / 2) + (Parent?.AbsoluteY ?? 0f);

        public float CenterX
        {
            set => _cx = Max(Width / 2, Min(value, (Parent?.Width ?? float.MaxValue) - (Width / 2)));
            get => _cx;
        }

        public float CenterY
        {
            set => _cy = Max(Height / 2, Min(value, (Parent?.Height ?? float.MaxValue) - (Height / 2)));
            get => _cy;
        }

        public float Height
        {
            set
            {
                _h = Min(Max(value, 10), Parent?.Width ?? float.MaxValue);

                HeightChanged?.Invoke(this, Height);
            }
            get => _h;
        }

        public float Width
        {
            set
            {
                _w = Min(Max(value, 10), Parent?.Width ?? float.MaxValue);

                WidthChanged?.Invoke(this, Width);
            }
            get => _w;
        }


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
            BeforeRender?.Invoke(this);

            bool cont = Contains(mouse.X, mouse.Y);
            bool pressed = cont && mouse.Pressed;

            if (_press && !pressed)
                OnClick();

            _press = pressed;

            OnRender(g, pressed, cont);

            cont &= this is IHUDClickable;

            foreach (HUDControl c in Childern)
                cont |= c?.Render(g, mouse) ?? false;

            AfterRender?.Invoke(this);

            return cont;
        }

        public bool Contains(float absx, float absy) => RectangleF.Contains(absx, absy);

        protected abstract void OnRender(Graphics g, bool mousedown, bool mousehover);

        protected void RenderBoundingBox(Graphics g, Color bg, Color fg)
        {
            g.FillRectangle(new SolidBrush(bg), RectangleF);
            g.DrawRectangle(new Pen(fg), AbsoluteX - 1, AbsoluteY - 1, Width + 2, Height + 2);
        }

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

    public class HUDPanel
        : HUDControl
        , IHUDPaddable
    {
        private float _pad;

        public float Padding
        {
            set => _pad = Max(0, Min(value, Max(Height / 2, Width / 2)));
            get => _pad;
        }


        public HUDPanel(HUDControl parent)
            : base(parent)
        {
        }

        protected override void OnRender(Graphics g, bool mousedown, bool mousehover) => RenderBoundingBox(g, BackgroundColor, ForegroundColor);

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
                control.Width = Width - (2 * Padding);
            }

            return control;
        }
    }

    public class HUDWindow
        : HUDTextualControl
        , IHUDPaddable
    {
        private float _pad;

        public float Padding
        {
            set => _pad = Max(0, Min(value, Max(Height / 2, Width / 2)));
            get => _pad;
        }


        public HUDWindow(HUDControl parent)
            : base(parent)
        {
        }

        protected override void OnRender(Graphics g, bool mousedown, bool mousehover)
        {
            RenderBoundingBox(g, BackgroundColor, ForegroundColor);

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
                control.Width = Width - (2 * Padding);
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

            RenderBoundingBox(g, bg, fg);

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
        internal bool IsOption { set; get; }
        public bool IsChecked { set; get; }
        public bool IsEnabled { set; get; }

        public event EventHandler<bool> StateChanged;


        public HUDCheckbox(HUDControl parent)
            : base(parent) => IsEnabled = true;

        protected override void OnRender(Graphics g, bool mousedown, bool mousehover)
        {
            Color bg = IsEnabled ? mousedown ? PressedBackgroundColor : mousehover ? HoveredBackgroundColor : BackgroundColor : DisabledBackgroundColor;
            Color fg = IsEnabled ? ForegroundColor : DisabledForegroundColor;

            int h = (int)((Height / 2) - 16);

            Rectangle r_in = new Rectangle(2 + (int)AbsoluteX, h + (int)AbsoluteY, 32, 32);
            Rectangle r_out = new Rectangle(2 + (int)AbsoluteX, h + (int)AbsoluteY, 31, 31);
            Rectangle r_chk = new Rectangle(9 + (int)AbsoluteX, h + (int)AbsoluteY + 8, 16, 16);

            if (IsOption)
            {
                g.FillEllipse(new SolidBrush(bg), r_in);
                g.DrawEllipse(new Pen(fg), r_out);
            }
            else
            {
                g.FillRectangle(new SolidBrush(bg), r_in);
                g.DrawRectangle(new Pen(fg), r_out);
            }

            if (IsChecked)
                if (IsOption)
                    g.FillEllipse(new SolidBrush(fg), r_chk);
                else
                    g.FillRectangle(new SolidBrush(fg), r_chk);

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

    public class HUDOptionbox
        : HUDTextualControl
    {
        private string[] _opt = new string[0];
        private bool _enb;
        private int _sel;

        
        public bool IsEnabled
        {
            set
            {
                _enb = value;

                foreach (HUDControl c in Childern)
                    if (c is HUDCheckbox cb)
                        cb.IsEnabled = value;
            }
            get => _enb;
        }

        public string[] Options
        {
            set
            {
                _opt = (from s in value ?? new string[0]
                        where s != null
                        select s.Trim()).ToArray();

                UpdateControls();
            }
            get => _opt;
        }

        public int SelectedIndex
        {
            set
            {
                if ((_sel >= 0) && (_sel < _opt.Length))
                    _sel = value;
            }
            get => _sel;
        }

        public string SelectedOption => Options[SelectedIndex];


        public event EventHandler<int> SelectedIndexChanged;


        public HUDOptionbox(HUDControl parent)
            : base(parent)
        {
            Options = new string[] { "Option 1", "Option 2" };

            WidthChanged += (_, __) => UpdateControls();
        }

        private void UpdateControls()
        {
            Childern.Clear();

            float hoffs = 2;

            foreach (string opt in Options)
            {
                Size sz = TextRenderer.MeasureText(opt, Font, new Size((int)(Width - 40), 500));

                sz.Height = Max(sz.Height + 4, 36);

                HUDCheckbox cb = new HUDCheckbox(this)
                {
                    Text = opt,
                    Font = Font,
                    Parent = this,
                    Width = Width - 4,
                    Height = sz.Height,
                    CenterX = Width / 2,
                    Top = hoffs,
                    IsOption = true,
                    ForegroundColor = ForegroundColor,
                    BackgroundColor = BackgroundColor
                };
                cb.StateChanged += ChildStateChanged;

                hoffs += sz.Height + 10;
            }

            float nh = hoffs - 8;
            float cy = CenterY - (Height / 2) + (nh / 2);

            Height = nh;
            CenterY = cy;
        }

        private void ChildStateChanged(object sender, bool e)
        {
            if (e)
            {
                int ndx = 0;

                foreach (HUDControl c in Childern)
                    if (c is HUDCheckbox cb)
                    {
                        if (cb != sender)
                            cb.IsChecked = false;
                        else
                            _sel = ndx;

                        ++ndx;
                    }

                SelectedIndexChanged?.Invoke(this, _sel);
            }
        }

        protected override void OnRender(Graphics g, bool mousedown, bool mousehover)
        {
        }
    }
}
