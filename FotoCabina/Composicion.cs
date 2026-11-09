using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace FotoCabina
{
    static class Composicion
    {
        const int W = 600, H = 1800, Margen = 30, Pie = 240, Sep = 30;
        public static Bitmap CrearTira(List<Bitmap> fotos)
        {
            int n = fotos.Count;
            int areaH = H - Pie - Margen - Margen;
            int ph = (areaH - Sep * (n - 1)) / n;
            int pw = W - Margen * 2;
            var bmp = new Bitmap(W, H);
            bmp.SetResolution(300, 300);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                int y = Margen;
                foreach (var f in fotos)
                {
                    var src = RecorteCentral(f, pw / (float)ph);
                    g.DrawImage(f, new Rectangle(Margen, y, pw, ph), src.X, src.Y, src.Width, src.Height, GraphicsUnit.Pixel);
                    y += ph + Sep;
                }
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var f1 = new Font("Segoe UI", 34, FontStyle.Bold))
                using (var f2 = new Font("Segoe UI", 20))
                {
                    g.DrawString(Config.Texto, f1, Brushes.Black, new RectangleF(0, H - Pie, W, Pie * 0.6f), sf);
                    g.DrawString(DateTime.Now.ToString("dd/MM/yyyy"), f2, Brushes.DimGray, new RectangleF(0, H - Pie * 0.4f, W, Pie * 0.3f), sf);
                }
                var marco = Path.Combine(Config.CarpetaApp, "marco.png");
                if (File.Exists(marco))
                {
                    using (var m = new Bitmap(marco)) g.DrawImage(m, new Rectangle(0, 0, W, H));
                }
            }
            return bmp;
        }
        static RectangleF RecorteCentral(Bitmap b, float aspecto)
        {
            float w = b.Width, h = b.Height;
            if (w / h > aspecto)
            {
                float nw = h * aspecto;
                return new RectangleF((w - nw) / 2, 0, nw, h);
            }
            float nh = w / aspecto;
            return new RectangleF(0, (h - nh) / 2, w, nh);
        }
        public static void GuardarJpg(Bitmap b, string ruta, long calidad = 95L)
        {
            var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using (var ep = new EncoderParameters(1))
            {
                ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, calidad);
                b.Save(ruta, codec, ep);
            }
        }
    }
}