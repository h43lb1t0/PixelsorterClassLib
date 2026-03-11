using PixelsorterClassLib;
using NumSharp;

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
        private static readonly byte[] Gray = [128, 128, 128, 255];
        private static readonly byte[] LowSaturation = [200, 150, 150, 255];
        private static readonly byte[] MidSaturation = [200, 100, 100, 255];
        private static readonly byte[] HighSaturation = [200, 0, 0, 255];

        private static NDArray CreateUnsortedImageData()
        {
            return np.array([
                ..HighSaturation, ..LowSaturation, ..Gray, ..MidSaturation,
                ..MidSaturation, ..HighSaturation, ..LowSaturation, ..Gray,
                ..LowSaturation, ..Gray, ..MidSaturation, ..HighSaturation,
                ..Gray, ..MidSaturation, ..HighSaturation, ..LowSaturation
            ]).reshape(4, 4, 4);
        }


        [Fact]
        public void Sorter_SortLeftToRight_SortsPixelsCorrectly()
        {
            var imgData = CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.RowLeftToRight);

            var expectedData = np.array([
                ..Gray, ..LowSaturation, ..MidSaturation, ..HighSaturation,
                ..Gray, ..LowSaturation, ..MidSaturation, ..HighSaturation,
                ..Gray, ..LowSaturation, ..MidSaturation, ..HighSaturation,
                ..Gray, ..LowSaturation, ..MidSaturation, ..HighSaturation
            ]).reshape(4, 4, 4);

            Assert.Equal(expectedData.ToArray<byte>(), sortedImage.ToArray<byte>());
        }

        [Fact]
        public void Sorter_SortRightToLeft_SortsPixelsCorrectly()
        {
            var imgData = CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.RowRightToLeft);

            var expectedData = np.array([
                ..HighSaturation, ..MidSaturation, ..LowSaturation, ..Gray,
                ..HighSaturation, ..MidSaturation, ..LowSaturation, ..Gray,
                ..HighSaturation, ..MidSaturation, ..LowSaturation, ..Gray,
                ..HighSaturation, ..MidSaturation, ..LowSaturation, ..Gray
            ]).reshape(4, 4, 4);

            Assert.Equal(expectedData.ToArray<byte>(), sortedImage.ToArray<byte>());
        }

        [Fact]
        public void Sorter_SortTopToBottom_SortsPixelsCorrectly()
        {
            var imgData = CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.ColumnTopToBottom);

            var expectedData = np.array([
                ..Gray, ..Gray, ..Gray, ..Gray,
                ..LowSaturation, ..LowSaturation, ..LowSaturation, ..LowSaturation,
                ..MidSaturation, ..MidSaturation, ..MidSaturation, ..MidSaturation,
                ..HighSaturation, ..HighSaturation, ..HighSaturation, ..HighSaturation
            ]).reshape(4, 4, 4);

            Assert.Equal(expectedData.ToArray<byte>(), sortedImage.ToArray<byte>());
        }

        [Fact]
        public void Sorter_SortBottomToTop_SortsPixelsCorrectly()
        {
            var imgData = CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.ColumnBottomToTop);

            var expectedData = np.array([
                ..HighSaturation, ..HighSaturation, ..HighSaturation, ..HighSaturation,
                ..MidSaturation, ..MidSaturation, ..MidSaturation, ..MidSaturation,
                ..LowSaturation, ..LowSaturation, ..LowSaturation, ..LowSaturation,
                ..Gray, ..Gray, ..Gray, ..Gray
            ]).reshape(4, 4, 4);

            Assert.Equal(expectedData.ToArray<byte>(), sortedImage.ToArray<byte>());
        }

    }
}
