using PixelsorterClassLib;

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
                var image = PixelsorterClassLib.Image.LoadImage(path);

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
    }
}
