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

        /// <summary>
        /// Gets the path to the real test image with EXIF rotation data (rotate-test-image.jpg).
        /// This image has EXIF orientation value 6 (90 degrees CCW).
        /// </summary>
        public static string GetTestImageWithExifRotation()
        {
            // Get the directory of the test project
            var testProjectDir = AppContext.BaseDirectory;
            var imagePath = Path.Combine(testProjectDir, "TestImages", "rotate-test-image.jpg");

            if (!File.Exists(imagePath))
            {
                // Fallback: search up the directory tree for the TestImages folder
                var currentDir = new DirectoryInfo(testProjectDir);
                while (currentDir != null)
                {
                    var testImagesDir = Path.Combine(currentDir.FullName, "TestImages");
                    if (Directory.Exists(testImagesDir))
                    {
                        imagePath = Path.Combine(testImagesDir, "rotate-test-image.jpg");
                        if (File.Exists(imagePath))
                            return imagePath;
                    }
                    currentDir = currentDir.Parent;
                }

                throw new FileNotFoundException($"Test image 'rotate-test-image.jpg' not found in TestImages directory");
            }

            return imagePath;
        }

        /// <summary>
        /// Gets the raw stored dimensions of the test image without applying AutoOrient.
        /// This is used to verify that AutoOrient actually rotates the image.
        /// </summary>
        public static (int rawWidth, int rawHeight) GetRawTestImageDimensions()
        {
            string testImagePath = GetTestImageWithExifRotation();
            using var image = SixLabors.ImageSharp.Image.Load(testImagePath);
            return (image.Width, image.Height);
        }

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
