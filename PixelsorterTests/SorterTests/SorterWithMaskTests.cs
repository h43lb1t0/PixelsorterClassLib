using PixelsorterClassLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Pixelsorter.Tests.SorterTests
{
    public class SorterWithMaskTests
    {
        [Fact]
        public void Sorter_SortLeftToRight_WithMask_SortsPixelsCorrectly()
        {
            using var imgData = SorterTestHelpers.CreateUnsortedImageData();
            using var mask = CreateMask();

            using var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.RowLeftToRight, mask);

            byte[] expectedData = [
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation
            ];

            Assert.Equal(expectedData, SorterTestHelpers.GetImageBytes(sortedImage));
        }

        [Fact]
        public void Sorter_SortIntoMask_WithMask_SortsPixelsAlongMaskRays()
        {
            using var baseImageData = SorterTestHelpers.CreateUnsortedImageData();
            var sourceData = SorterTestHelpers.GetImageBytes(baseImageData);

            SetPixel(sourceData, 1, 0, SorterTestHelpers.Gray);
            SetPixel(sourceData, 2, 0, SorterTestHelpers.HighSaturation);
            SetPixel(sourceData, 1, 1, SorterTestHelpers.Gray);
            SetPixel(sourceData, 1, 2, SorterTestHelpers.HighSaturation);

            using var imgData = SorterTestHelpers.CreateImageFromBytes(sourceData, 4, 4);
            using var mask = CreateMask();

            using var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.IntoMask, mask);

            byte[] expectedData = [
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation
            ];

            Assert.Equal(expectedData, SorterTestHelpers.GetImageBytes(sortedImage));
        }

        [Fact]
        public void Sorter_SortRightToLeft_WithMask_SortsPixelsCorrectly()
        {
            using var imgData = SorterTestHelpers.CreateUnsortedImageData();
            using var mask = CreateMask();

            using var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.RowRightToLeft, mask);

            byte[] expectedData = [
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation
            ];

            Assert.Equal(expectedData, SorterTestHelpers.GetImageBytes(sortedImage));
        }

        [Fact]
        public void Sorter_SortTopToBottom_WithMask_SortsPixelsCorrectly()
        {
            using var imgData = SorterTestHelpers.CreateUnsortedImageData();
            using var mask = CreateMask();

            using var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.ColumnTopToBottom, mask);

            byte[] expectedData = [
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation
            ];

            Assert.Equal(expectedData, SorterTestHelpers.GetImageBytes(sortedImage));
        }

        [Fact]
        public void Sorter_SortBottomToTop_WithMask_SortsPixelsCorrectly()
        {
            using var imgData = SorterTestHelpers.CreateUnsortedImageData();
            using var mask = CreateMask();

            using var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.SortBy.Saturation(), SortDirections.ColumnBottomToTop, mask);

            byte[] expectedData = [
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation
            ];

            Assert.Equal(expectedData, SorterTestHelpers.GetImageBytes(sortedImage));
        }

        private static Image<L8> CreateMask()
        {
            var image = new Image<L8>(4, 4);
            byte[] maskData = [
                255, 255, 255, 0,
                0, 255, 255, 0,
                0, 255, 255, 0,
                0, 0, 0, 0
            ];

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < 4; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    int rowOffset = y * 4;
                    for (int x = 0; x < 4; x++)
                    {
                        row[x] = new L8(maskData[rowOffset + x]);
                    }
                }
            });

            return image;
        }

        private static void SetPixel(byte[] data, int x, int y, byte[] pixel)
        {
            Array.Copy(pixel, 0, data, (y * 4 + x) * 4, 4);
        }

    }
}
