using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PaxeraHealthTask
{
    internal static class ImageFilters
    {
        public static Bitmap Invert(Bitmap source)
        {
            return Apply(source, pixels =>
            {
                for (var i = 0; i < pixels.Length; i += 4)
                {
                    pixels[i] = (byte)(255 - pixels[i]);
                    pixels[i + 1] = (byte)(255 - pixels[i + 1]);
                    pixels[i + 2] = (byte)(255 - pixels[i + 2]);
                }
            });
        }

        public static Bitmap Grayscale(Bitmap source)
        {
            return Apply(source, pixels =>
            {
                for (var i = 0; i < pixels.Length; i += 4)
                {
                    int b = pixels[i];
                    int g = pixels[i + 1];
                    int r = pixels[i + 2];

                    var gray = (byte)((r * 299 + g * 587 + b * 114) / 1000);

                    pixels[i] = gray;
                    pixels[i + 1] = gray;
                    pixels[i + 2] = gray;
                }
            });
        }

        private static Bitmap Apply(Bitmap source, Action<byte[]> process)
        {
            var rect = new Rectangle(0, 0, source.Width, source.Height);
            var result = source.Clone(rect, PixelFormat.Format32bppArgb);

            BitmapData data = result.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                var pixels = new byte[Math.Abs(data.Stride) * result.Height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

                process(pixels);

                Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            }
            finally
            {
                result.UnlockBits(data);
            }

            return result;
        }
    }
}