using NumSharp;
using PixelsorterClassLib.core;

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
            var imgData = SorterTestHelpers.CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.core.SortBy.Saturation(), SortDirections.RowLeftToRight);

            var expectedData = np.array([
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation
            ]).reshape(4, 4, 4);

            Assert.Equal(expectedData.ToArray<byte>(), sortedImage.ToArray<byte>());
        }

        [Fact]
        public void Sorter_SortRightToLeft_SortsPixelsCorrectly()
        {
            var imgData = SorterTestHelpers.CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.core.SortBy.Saturation(), SortDirections.RowRightToLeft);

            var expectedData = np.array([
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray
            ]).reshape(4, 4, 4);

            Assert.Equal(expectedData.ToArray<byte>(), sortedImage.ToArray<byte>());
        }

        [Fact]
        public void Sorter_SortTopToBottom_SortsPixelsCorrectly()
        {
            var imgData = SorterTestHelpers.CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.core.SortBy.Saturation(), SortDirections.ColumnTopToBottom);

            var expectedData = np.array([
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation
            ]).reshape(4, 4, 4);

            Assert.Equal(expectedData.ToArray<byte>(), sortedImage.ToArray<byte>());
        }

        [Fact]
        public void Sorter_SortBottomToTop_SortsPixelsCorrectly()
        {
            var imgData = SorterTestHelpers.CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.core.SortBy.Saturation(), SortDirections.ColumnBottomToTop);

            var expectedData = np.array([
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray
            ]).reshape(4, 4, 4);

            Assert.Equal(expectedData.ToArray<byte>(), sortedImage.ToArray<byte>());
        }

        [Fact]
        public void Sorter_IntoMask_ExceptionThrown()
        {
            var imgData = SorterTestHelpers.CreateUnsortedImageData();

            Assert.Throws<ArgumentException>(() => Sorter.SortImage(imgData, PixelsorterClassLib.core.SortBy.Saturation(), SortDirections.IntoMask));
        }
    }
}
