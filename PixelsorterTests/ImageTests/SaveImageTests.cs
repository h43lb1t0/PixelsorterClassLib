using PixelsorterClassLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Pixelsorter.Tests.ImageTests
{
    public class SaveImageTests
    {
        [Theory]
        [MemberData(nameof(GetImageFormatCombinations))]
        public void SaveImage_ShouldSupportFormatAndChannels(string extension, int channels)
        {
            string inPath = ImageTestHelpers.CreateTestImage(extension, channels);
            string outPath = Path.Combine(Path.GetTempPath(), $"test_save_image_{Guid.NewGuid()}{extension}");
            try
            {
                var image = PixelsorterClassLib.Image.LoadImage(inPath);
                PixelsorterClassLib.Image.SaveImage(image, outPath);

                Assert.True(File.Exists(outPath));
                Assert.True(new FileInfo(outPath).Length > 0);
            }
            finally
            {
                if (File.Exists(inPath)) File.Delete(inPath);
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }

        public static TheoryData<string, int> GetImageFormatCombinations()
        {
            var data = new TheoryData<string, int>();
            foreach (var ext in ImageTestHelpers.FileExtensions)
            {
                foreach (var chan in ImageTestHelpers.ChannelCounts)
                {
                    data.Add(ext, chan);
                }
            }
            return data;
        }

        public static TheoryData<string> GetExtensions()
        {
            var data = new TheoryData<string>();
            foreach (var ext in ImageTestHelpers.FileExtensions)
            {
                data.Add(ext);
            }
            return data;
        }

        [Theory]
        [MemberData(nameof(GetExtensions))]
        public void SaveImage_WithAlpha_ShouldSaveSuccessfully(string extension)
        {
            string inPath = ImageTestHelpers.CreateTestImageWithAlpha(extension);
            string outPath = Path.Combine(Path.GetTempPath(), $"test_save_image_alpha_{Guid.NewGuid()}{extension}");
            try
            {
                var image = PixelsorterClassLib.Image.LoadImage(inPath);
                PixelsorterClassLib.Image.SaveImage(image, outPath);

                Assert.True(File.Exists(outPath));
                Assert.True(new FileInfo(outPath).Length > 0);
            }
            finally
            {
                if (File.Exists(inPath)) File.Delete(inPath);
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }

        [Theory]
        [MemberData(nameof(GetExtensions))]
        public void SaveImage_Grayscale_ShouldSaveSuccessfully(string extension)
        {
            string inPath = ImageTestHelpers.CreateGrayscaleTestImage(extension);
            string outPath = Path.Combine(Path.GetTempPath(), $"test_save_image_grayscale_{Guid.NewGuid()}{extension}");
            try
            {
                var image = PixelsorterClassLib.Image.LoadImage(inPath);
                PixelsorterClassLib.Image.SaveImage(image, outPath);

                Assert.True(File.Exists(outPath));
                Assert.True(new FileInfo(outPath).Length > 0);
            }
            finally
            {
                if (File.Exists(inPath)) File.Delete(inPath);
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }
    }
}
