using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System;


namespace OpenTKMinecraft.Components.UI
{
    using static System.Math;


    public abstract class HUDControl
    {
        private float _x, _y, _w, _h;
        private HUDControl _par;


        public List<HUDControl> Childern { get; } = new List<HUDControl>();

        public Color ForegroundColor { get; set; }

        public Color BackgroundColor { get; set; }

        public RectangleF RectangleF => new RectangleF(_x, _y, _w, _h);

        public Rectangle Rectangle => new Rectangle((int)_x, (int)_y, (int)_w, (int)_h);

        public HUDControl Parent
        {
            internal set
            {
                _par?.Childern?.Remove(this);
                (_par = value)?.Childern?.Add(this);
            }
            get => _par;
        }

        public float CenterX
        {
            set => Left = value - (Width / 2);
            get => Left + (Width / 2);
        }

        public float CenterY
        {
            set => Top = value - (Height / 2);
            get => Top + (Height / 2);
        }

        public float Height
        {
            set => _h = Max(value, 10);
            get => _h;
        }

        public float Width
        {
            set => _w = Max(value, 10);
            get => _w;
        }

        public float Left
        {
            set => _x = Max(0, Min(value, (Parent?.Width ?? float.MaxValue) - Width)) + (Parent?.Left ?? 0);
            get => _x - (Parent?.Left ?? 0);
        }

        public float Top
        {
            set => _y = Max(0, Min(value, (Parent?.Height ?? float.MaxValue) - Height)) + (Parent?.Top ?? 0);
            get => _y - (Parent?.Top ?? 0);
        }


        public HUDControl(HUDControl parent)
        {
            ForegroundColor = Color.WhiteSmoke;
            BackgroundColor = Color.DarkGray;
            Width = 100;
            Height = 50;
            Left = 20;
            Top = 20;
            Parent = parent;
        }

        public void Render(Graphics g, HUDMouseData mouse)
        {
            OnRender(g, IsInside(mouse.X, mouse.Y) && mouse.Pressed);

            foreach (HUDControl c in Childern)
                c?.Render(g, mouse);
        }

        public bool IsInside(float x, float y) => (x >= Left) && (y >= Top) && (x < Left + Width) && (y < Top + Height);

        protected abstract void OnRender(Graphics g, bool mousedown);

        protected virtual void OnClick()
        {
        }
    }

    public class HUDWindow
        : HUDControl
    {
        public HUDWindow(HUDControl parent)
            : base(parent)
        {
        }

        protected override void OnRender(Graphics g, bool mousedown)
        {
            g.FillRectangle(new SolidBrush(BackgroundColor), RectangleF);
            g.DrawRectangle(new Pen(ForegroundColor), Rectangle);
        }
    }

    public class HUDLabel
        : HUDControl
    {
        public HUDLabel(HUDControl parent)
            : base(parent)
        {
        }

        protected override void OnRender(Graphics g, bool mousedown)
        {
        }
    }

    public class HUDButton
        : HUDControl
    {
        public HUDButton(HUDControl parent)
            : base(parent)
        {
        }

        protected override void OnRender(Graphics g, bool mousedown)
        {
        }
    }

    public class HUDCheckbox
        : HUDButton
    {
        public HUDCheckbox(HUDControl parent)
            : base(parent)
        {
        }

        protected override void OnRender(Graphics g, bool mousedown)
        {
        }
    }
}
