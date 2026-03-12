using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Pixelsorter.Tests.SorterTests
{
    public class SorterTestHelpers
    {
        public static readonly byte[] Gray = [128, 128, 128, 255];
        public static readonly byte[] LowSaturation = [200, 150, 150, 255];
        public static readonly byte[] MidSaturation = [200, 100, 100, 255];
        public static readonly byte[] HighSaturation = [200, 0, 0, 255];

        public static Image<Rgba32> CreateUnsortedImageData()
        {
            return CreateImageFromBytes([
                ..HighSaturation, ..LowSaturation, ..Gray, ..MidSaturation,
                ..MidSaturation, ..HighSaturation, ..LowSaturation, ..Gray,
                ..LowSaturation, ..Gray, ..MidSaturation, ..HighSaturation,
                ..Gray, ..MidSaturation, ..HighSaturation, ..LowSaturation
            ], 4, 4);
        }

        public static byte[] GetImageBytes(Image<Rgba32> image)
        {
            var data = new byte[image.Width * image.Height * 4];

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    int rowOffset = y * image.Width * 4;
                    for (int x = 0; x < image.Width; x++)
                    {
                        int pixelOffset = rowOffset + x * 4;
                        var pixel = row[x];
                        data[pixelOffset] = pixel.R;
                        data[pixelOffset + 1] = pixel.G;
                        data[pixelOffset + 2] = pixel.B;
                        data[pixelOffset + 3] = pixel.A;
                    }
                }
            });

            return data;
        }

        public static Image<Rgba32> CreateImageFromBytes(byte[] data, int width, int height)
        {
            var image = new Image<Rgba32>(width, height);

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    int rowOffset = y * width * 4;
                    for (int x = 0; x < width; x++)
                    {
                        int pixelOffset = rowOffset + x * 4;
                        row[x] = new Rgba32(
                            data[pixelOffset],
                            data[pixelOffset + 1],
                            data[pixelOffset + 2],
                            data[pixelOffset + 3]);
                    }
                }
            });

            return image;
        }
    }
}
