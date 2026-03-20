using NumSharp;
using PixelsorterClassLib.Core;

namespace Pixelsorter.Tests.SorterTests
{
    public class SorterWithMaskTests
    {
        [Fact]
        public void Sorter_SortLeftToRight_WithMask_SortsPixelsCorrectly()
        {
            var imgData = SorterTestHelpers.CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.Core.SortBy.Saturation(), SortDirections.RowLeftToRight, CreateMask());

            var expectedData = np.array([
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation
            ]).reshape(4, 4, 3);

            Assert.Equal(expectedData.ToArray<float>(), sortedImage.ToArray<float>());
        }

        [Fact]
        public void Sorter_SortIntoMask_WithMask_SortsPixelsAlongMaskRays()
        {
            var baseImageData = SorterTestHelpers.CreateUnsortedImageData();
            var sourceData = baseImageData.ToArray<float>();

            SetPixel(sourceData, 1, 0, SorterTestHelpers.Gray);
            SetPixel(sourceData, 2, 0, SorterTestHelpers.HighSaturation);
            SetPixel(sourceData, 1, 1, SorterTestHelpers.Gray);
            SetPixel(sourceData, 1, 2, SorterTestHelpers.HighSaturation);

            var imgData = np.array(sourceData).reshape(4, 4, 3);

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.Core.SortBy.Saturation(), SortDirections.IntoMask, CreateMask());

            var expectedData = np.array([
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation
            ]).reshape(4, 4, 3);

            Assert.Equal(expectedData.ToArray<float>(), sortedImage.ToArray<float>());
        }

        [Fact]
        public void Sorter_SortRightToLeft_WithMask_SortsPixelsCorrectly()
        {
            var imgData = SorterTestHelpers.CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.Core.SortBy.Saturation(), SortDirections.RowRightToLeft, CreateMask());

            var expectedData = np.array([
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation
            ]).reshape(4, 4, 3);

            Assert.Equal(expectedData.ToArray<float>(), sortedImage.ToArray<float>());
        }

        [Fact]
        public void Sorter_SortTopToBottom_WithMask_SortsPixelsCorrectly()
        {
            var imgData = SorterTestHelpers.CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.Core.SortBy.Saturation(), SortDirections.ColumnTopToBottom, CreateMask());

            var expectedData = np.array([
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation
            ]).reshape(4, 4, 3);

            Assert.Equal(expectedData.ToArray<float>(), sortedImage.ToArray<float>());
        }

        [Fact]
        public void Sorter_SortBottomToTop_WithMask_SortsPixelsCorrectly()
        {
            var imgData = SorterTestHelpers.CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.Core.SortBy.Saturation(), SortDirections.ColumnBottomToTop, CreateMask());

            var expectedData = np.array([
                ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.Gray, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation
            ]).reshape(4, 4, 3);

            Assert.Equal(expectedData.ToArray<float>(), sortedImage.ToArray<float>());
        }

        private static NDArray CreateMask()
        {
            return np.array(new byte[] {
                255, 255, 255, 0,
                0, 255, 255, 0,
                0, 255, 255, 0,
                0, 0, 0, 0
            }).reshape(4, 4, 1);
        }

        private static void SetPixel(float[] data, int x, int y, float[] pixel)
        {
            Array.Copy(pixel, 0, data, (y * 4 + x) * 3, 3);
        }

    }
}
