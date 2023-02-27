using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace CaptchaGenerator
{
    public class Captcha : ICaptchaModule
    {
        private static Random Rand = new Random(DateTime.Now.GetHashCode());
        private readonly CaptchaOptions _options;

        public Captcha(CaptchaOptions options)
        {
            _options = options;
        }

        public byte[] Generate(string text)
        {
            byte[] result;
            using (var imgText = new Image<Rgba32>(_options.Width, _options.Height))
            {
                float position = 5;
                byte charPadding = (byte)Rand.Next(5, 10);
                imgText.Mutate(ctx => ctx.BackgroundColor(_options.BackgroundColor[Rand.Next(0, _options.BackgroundColor.Length)]));

                string fontName = _options.FontFamilies[Rand.Next(0, _options.FontFamilies.Length)];
                Font font = SystemFonts.CreateFont(fontName, _options.FontSize, _options.FontStyle);

                foreach (char ch in text)
                {
                    var location = new PointF(charPadding + position, Rand.Next(6, Math.Abs(_options.Height - _options.FontSize - 5)));
                    imgText.Mutate(ctx => ctx.DrawText(ch.ToString(), font, _options.TextColor[Rand.Next(0, _options.TextColor.Length)], location));
                    position += TextMeasurer.Measure(ch.ToString(), new TextOptions(font)).Width;
                }

                // add rotation
                AffineTransformBuilder rotation = GetRotation();
                imgText.Mutate(ctx => ctx.Transform(rotation));

                // add the dynamic image to original image
                var size = (ushort)TextMeasurer.Measure(text, new TextOptions(font)).Width;
                var img = new Image<Rgba32>(size + 15, _options.Height);
                img.Mutate(ctx => ctx.BackgroundColor(_options.BackgroundColor[Rand.Next(0, _options.BackgroundColor.Length)]));
                img.Mutate(ctx => ctx.DrawImage(imgText, 0.80f));

                for (var i = 0; i < _options.DrawLines; i++)
                    DrawNoiseLines(img);

                for (var i = 0; i < _options.NoiseRate; i++)
                    AddNoisePoint(img);

                img.Mutate(x => x.Resize(_options.Width, _options.Height));

                using var ms = new MemoryStream();
                img.Save(ms, _options.Encoder);
                result = ms.ToArray();
            }

            return result;
        }

        private void AddNoisePoint(Image img)
        {
            int x0 = Rand.Next(0, img.Width);
            int y0 = Rand.Next(0, img.Height);
            img.Mutate(ctx => ctx.DrawLines(_options.NoiseRateColor[Rand.Next(0, _options.NoiseRateColor.Length)],
                    1, new PointF[] { new Vector2(x0, y0), new Vector2(x0, y0) }));
        }

        private void DrawNoiseLines(Image img)
        {
            int x0 = Rand.Next(0, Rand.Next(0, 30));
            int y0 = Rand.Next(10, img.Height);
            int x1 = Rand.Next(img.Width - Rand.Next(0, (int)(img.Width * 0.25)), img.Width);
            int y1 = Rand.Next(0, img.Height);
            img.Mutate(ctx => ctx.DrawLines(_options.DrawLinesColor[Rand.Next(0, _options.DrawLinesColor.Length)],
                          Extensions.GenerateNextFloat(_options.MinLineThickness, _options.MaxLineThickness),
                          new PointF[] { new PointF(x0, y0), new PointF(x1, y1) }));
        }

        private AffineTransformBuilder GetRotation()
        {
            var builder = new AffineTransformBuilder();
            var width = Rand.Next(10, _options.Width);
            var height = Rand.Next(10, _options.Height);
            var pointF = new PointF(width, height);
            var rotationDegrees = Rand.Next(0, _options.MaxRotationDegrees);
            var result = builder.PrependRotationDegrees(rotationDegrees, pointF);
            return result;
        }
    }
}
