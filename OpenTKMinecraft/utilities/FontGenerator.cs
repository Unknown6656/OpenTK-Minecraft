using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenTKMinecraft.Utilities
{
    public static class FontGenerator
    {
        private const string Characters = @"qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM0123456789µäöüÖÄÜßÿ§½!""αβγπ¯τθσλ#¤%&/()=?^*@£€${[]}\+~¨'-_.:,;<>|°©®±¥";


        public static Bitmap GenerateCharacters(int fontSize, string fontName, out Size charSize)
        {
            using (Font font = new Font(fontName, fontSize))
            {
                List<Bitmap> characters = new List<Bitmap>();

                for (int i = 0; i < Characters.Length; i++)
                    characters.Add(GenerateCharacter(font, Characters[i]));

                charSize = new Size(characters.Max(x => x.Width), characters.Max(x => x.Height));

                Bitmap charMap = new Bitmap(charSize.Width * characters.Count, charSize.Height);

                using (Graphics gfx = Graphics.FromImage(charMap))
                {
                    gfx.FillRectangle(Brushes.Black, 0, 0, charMap.Width, charMap.Height);

                    for (int i = 0; i < characters.Count; i++)
                        using (Bitmap c = characters[i])
                            gfx.DrawImageUnscaled(c, i * charSize.Width, 0);
                }

                return charMap;
            }
        }

        private static Bitmap GenerateCharacter(Font font, char c)
        {
            SizeF size = GetSize(font, c);
            Bitmap bmp = new Bitmap((int)size.Width, (int)size.Height);

            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                gfx.FillRectangle(Brushes.Black, 0, 0, bmp.Width, bmp.Height);
                gfx.DrawString(c.ToString(), font, Brushes.White, 0, 0);
            }

            return bmp;
        }

        private static SizeF GetSize(Font font, char c)
        {
            using (Bitmap bmp = new Bitmap(512, 512))
            using (Graphics gfx = Graphics.FromImage(bmp))
                return gfx.MeasureString(c.ToString(), font);
        }
    }
}
