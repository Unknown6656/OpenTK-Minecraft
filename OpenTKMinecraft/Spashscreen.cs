using System.Windows.Forms;
using System;

namespace OpenTKMinecraft
{
    public partial class Spashscreen
        : Form
    {
        public string Title
        {
            set
            {
                while (Handle == IntPtr.Zero)
                    Application.DoEvents();

                Invoke(new MethodInvoker(() =>
                {
                    label1.Text = value?.Trim() ?? "";

                    Update();

                    Application.DoEvents();
                }));
            }
            get => label1.Text;
        }

        public string Subtitle
        {
            set
            {
                while (Handle == IntPtr.Zero)
                    Application.DoEvents();

                Invoke(new MethodInvoker(() =>
                {
                    label2.Text = value?.Trim() ?? "";

                    Update();

                    Application.DoEvents();
                }));
            }
            get => label2.Text;
        }

        new public (string Title, string Subtitle) Text
        {
            set => (Title, Subtitle) = value;
            get => (Title, Subtitle);
        }


        public Spashscreen() => InitializeComponent();
    }
}
