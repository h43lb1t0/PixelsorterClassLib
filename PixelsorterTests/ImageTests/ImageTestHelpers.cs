using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pixelsorter.Tests.ImageTests
{
    public class ImageTestHelpers
    {
        public static readonly String[] FileExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp"];
        public static readonly int[] ChannelCounts = [1, 3, 4];

        public static string CreateTestImage(String ext, int chanels)
        {
            String path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"test_image_{Guid.NewGuid()}{ext}");
            switch (chanels)
            {
                case 1:
                    using (var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.L8>(24, 24)) image.Save(path);
                    break;
                case 3:
                    using (var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(24, 24)) image.Save(path);
                    break;
                case 4:
                default:
                    using (var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(24, 24)) image.Save(path);
                    break;
            }
            return path;
        }

        public static String CreateCoruptedImage(String ext)
        {
            String path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"corrupted_image_{Guid.NewGuid()}{ext}");
            using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(24, 24);
            image.Save(path);
            // Corrupt the file by writing random bytes
            var randomData = new byte[100];
            new Random().NextBytes(randomData);
            System.IO.File.WriteAllBytes(path, randomData);
            return path;
        }

        public static String CreateTestImageWithAlpha(String ext)
        {
            String path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"test_image_alpha_{Guid.NewGuid()}{ext}");
            using (var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(24, 24)) image.Save(path);
            return path;
        }

        public static String CreateGrayscaleTestImage(String ext)
        {
            String path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"test_image_grayscale{ext}");
            using (var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.L8>(24, 24)) image.Save(path);
            return path;
        }
    }
}
