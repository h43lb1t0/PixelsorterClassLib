using PixelsorterClassLib.core;

namespace Pixelsorter.Tests.ImageTests
{
    public class LoadImageTests
    {
        [Theory]
        [MemberData(nameof(GetImageFormatCombinations))]
        public void LoadImage_ShouldSupportFormatAndChannels(string extension, int channels)
        {
            string path = ImageTestHelpers.CreateTestImage(extension, channels);
            try
            {
                var image = Image.LoadImage(path);

                Assert.NotNull(image);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
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

        [Fact]
        public void LoadImage_ShouldThrowOnCorruptedFile()
        {
            string path = ImageTestHelpers.CreateCoruptedImage(".png");
            try
            {
                Assert.ThrowsAny<Exception>(() => Image.LoadImage(path));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Theory]
        [MemberData(nameof(GetExtensions))]
        public void LoadImage_WithAlpha_ShouldLoadSuccessfully(string extension)
        {
            string path = ImageTestHelpers.CreateTestImageWithAlpha(extension);
            try
            {
                var image = Image.LoadImage(path);
                Assert.NotNull(image);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Theory]
        [MemberData(nameof(GetExtensions))]
        public void LoadImage_Grayscale_ShouldLoadSuccessfully(string extension)
        {
            string path = ImageTestHelpers.CreateGrayscaleTestImage(extension);
            try
            {
                var image = Image.LoadImage(path);
                Assert.NotNull(image);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
