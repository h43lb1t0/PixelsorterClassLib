using SixLabors.ImageSharp.PixelFormats;
namespace Pixelsorter.Tests.SortBy
{
    public class SortByTests
    {

        [Fact]
        public void SortBy_Brightness_ReturnsSortedPixels()
        {
            Rgba32 pixels = new(255, 255, 255);
            var result = PixelsorterClassLib.core.SortBy.Brightness()(pixels);
            Assert.Equal(255f, result);
        }

        [Fact]
        public void SortBy_Hue_ReturnsSortedPixels()
        {
            Rgba32 pixels = new(255, 0, 0);

            var result = PixelsorterClassLib.core.SortBy.Hue()(pixels);

            Assert.Equal(0f, result);
        }

        [Fact]
        public void SortBy_Saturation_ReturnsSortedPixels()
        {
            Rgba32 pixels = new(128, 128, 128);
            var result = PixelsorterClassLib.core.SortBy.Saturation()(pixels);
            Assert.Equal(0f, result);
        }

        [Fact]
        public void SortBy_Warmth_ReturnsSortedPixels()
        {
            Rgba32 pixels = new(255, 0, 0);
            var result = PixelsorterClassLib.core.SortBy.Warmth()(pixels);
            Assert.Equal(1f, result);
        }

        [Fact]
        public void SortBy_Coolness_ReturnsSortedPixels()
        {
            Rgba32 pixels = new(0, 0, 255);
            var result = PixelsorterClassLib.core.SortBy.Coolness()(pixels);
            Assert.Equal(1f, result);
        }

        [Fact]
        public void SortBy_Lightness_ReturnsSortedPixels()
        {
            Rgba32 pixels = new(0, 0, 0);
            var result = PixelsorterClassLib.core.SortBy.Lightness()(pixels);
            Assert.Equal(0f, result);
        }

        [Fact]
        public void SortBy_GetAllSortingCriteria_ReturnsAllCriteria()
        {
            var criteria = PixelsorterClassLib.core.SortBy.GetAllSortingCriteria();
            Assert.Contains("Brightness", criteria);
            Assert.Contains("Hue", criteria);
            Assert.Contains("Saturation", criteria);
            Assert.Contains("Warmth", criteria);
            Assert.Contains("Coolness", criteria);
            Assert.Contains("Lightness", criteria);
        }
    }
}
