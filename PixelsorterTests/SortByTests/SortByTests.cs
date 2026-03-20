using SixLabors.ImageSharp.ColorSpaces;
namespace Pixelsorter.Tests.SortBy
{
    public class SortByTests
    {

        [Fact]
        public void SortBy_Hue_ReturnsSortedPixels()
        {
            var pixel = new Hsl(0f, 1f, 0.5f); // Red hue = 0

            var result = PixelsorterClassLib.Core.SortBy.Hue()(pixel);

            Assert.Equal(0f, result);
        }

        [Fact]
        public void SortBy_Saturation_ReturnsSortedPixels()
        {
            var pixel = new Hsl(0f, 0f, 0.5f); // Gray, saturation = 0

            var result = PixelsorterClassLib.Core.SortBy.Saturation()(pixel);

            Assert.Equal(0f, result);
        }

        [Fact]
        public void SortBy_Warmth_ReturnsSortedPixels()
        {
            var pixel = new Hsl(30f, 1f, 0.5f); // Warm orange, exact target hue

            var result = PixelsorterClassLib.Core.SortBy.Warmth()(pixel);

            Assert.Equal(1f, result);
        }

        [Fact]
        public void SortBy_Coolness_ReturnsSortedPixels()
        {
            var pixel = new Hsl(210f, 1f, 0.5f); // Cool blue, exact target hue

            var result = PixelsorterClassLib.Core.SortBy.Coolness()(pixel);

            Assert.Equal(1f, result);
        }

        [Fact]
        public void SortBy_Lightness_ReturnsSortedPixels()
        {
            var pixel = new Hsl(0f, 0f, 0f); // Black, lightness = 0

            var result = PixelsorterClassLib.Core.SortBy.Lightness()(pixel);

            Assert.Equal(0f, result);
        }

        [Fact]
        public void SortBy_GetAllSortingCriteria_ReturnsAllCriteria()
        {
            var criteria = PixelsorterClassLib.Core.SortBy.GetAllSortingCriteria();
            Assert.Contains("Hue", criteria);
            Assert.Contains("Saturation", criteria);
            Assert.Contains("Lightness", criteria);
            Assert.Contains("Warmth", criteria);
            Assert.Contains("Coolness", criteria);
            Assert.Contains("Chroma", criteria);
            Assert.Contains("PerceivedVibrancy", criteria);
        }
    }
}
