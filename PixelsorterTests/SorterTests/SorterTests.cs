using PixelsorterClassLib;

namespace Pixelsorter.Tests.SorterTests
{

    /// <summary>
    /// Tests for the sorting direction functionality of the PixelsorterClassLib using Saturation as the standdart sorting method. 
    /// The sorting methods are NOT tested here, only the sorting directions. 
    /// The sorting methods are tested in the SortByTests class.
    /// All tests use a 4x4 NDArray of Rgba32 pixels with varying saturation values to ensure that the sorting direction is correctly applied.
    /// </summary>
    public class SorterTests
    {
        


        [Fact]
        public void Sorter_SortLeftToRight_SortsPixelsCorrectly()
        {
            using var imgData = SorterTestHelpers.CreateUnsortedImageData();

            using var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.RowLeftToRight);

            byte[] expectedData = [
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation
            ];

            Assert.Equal(expectedData, SorterTestHelpers.GetImageBytes(sortedImage));
        }

        [Fact]
        public void Sorter_SortRightToLeft_SortsPixelsCorrectly()
        {
            using var imgData = SorterTestHelpers.CreateUnsortedImageData();

            using var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.RowRightToLeft);

            byte[] expectedData = [
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray
            ];

            Assert.Equal(expectedData, SorterTestHelpers.GetImageBytes(sortedImage));
        }

        [Fact]
        public void Sorter_SortTopToBottom_SortsPixelsCorrectly()
        {
            using var imgData = SorterTestHelpers.CreateUnsortedImageData();

            using var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.ColumnTopToBottom);

            byte[] expectedData = [
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation
            ];

            Assert.Equal(expectedData, SorterTestHelpers.GetImageBytes(sortedImage));
        }

        [Fact]
        public void Sorter_SortBottomToTop_SortsPixelsCorrectly()
        {
            using var imgData = SorterTestHelpers.CreateUnsortedImageData();

            using var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.ColumnBottomToTop);

            byte[] expectedData = [
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray
            ];

            Assert.Equal(expectedData, SorterTestHelpers.GetImageBytes(sortedImage));
        }

        [Fact]
        public void Sorter_IntoMask_ExceptionThrown()
        {
            using var imgData = SorterTestHelpers.CreateUnsortedImageData();

            Assert.Throws<ArgumentException>(() => Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.IntoMask));
        }
    }
}
