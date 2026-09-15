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

        [Fact]
        public void Sorter_SortArbitraryAngle_45Degrees_WithMask_SortsPixelsCorrectly()
        {
            var imgData = SorterTestHelpers.CreateUnsortedImageData();

            var sortedImage = Sorter.SortImage(imgData, PixelsorterClassLib.Core.SortBy.Saturation(), SortDirections.ArbitraryAngle, CreateMask(), 45f);

            var expectedData = np.array([
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation,
                ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray,
                ..SorterTestHelpers.LowSaturation, ..SorterTestHelpers.Gray, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.HighSaturation,
                ..SorterTestHelpers.Gray, ..SorterTestHelpers.MidSaturation, ..SorterTestHelpers.HighSaturation, ..SorterTestHelpers.LowSaturation
            ]).reshape(4, 4, 3);

            Assert.Equal(expectedData.ToArray<float>(), sortedImage.ToArray<float>());
        }

        /// <summary>
        /// Attempts to trigger a buffer overflow in ApplyRadialMaskSort.
        /// The internal runOffsets/runPixels arrays are sized to (width + height).
        /// A single radial angle bucket can theoretically hold more pixels than that,
        /// especially when the centroid is off-center, causing an IndexOutOfRangeException.
        ///
        /// Scenario 1: Large fully-masked square image.
        /// Max bucket ≈ πN/2 ≈ 1.57N, buffer = 2N → ratio ~0.785, should NOT overflow.
        /// </summary>
        [Fact]
        public void Sorter_IntoMask_LargeFullyMaskedImage_DoesNotOverflow()
        {
            const int size = 500;
            var pixelData = new float[size * size * 3];
            var imgData = np.array(pixelData).reshape(size, size, 3);

            // Fully-masked image → centroid at center → max bucket ≈ πN/2
            var maskData = new byte[size * size];
            Array.Fill(maskData, (byte)255);
            var mask = np.array(maskData).reshape(size, size, 1);

            // Should NOT throw IndexOutOfRangeException
            var result = Sorter.SortImage(imgData, PixelsorterClassLib.Core.SortBy.Lightness(), SortDirections.IntoMask, mask);

            Assert.NotNull(result);
        }

        /// <summary>
        /// Scenario 2: Asymmetric mask to push the centroid toward the top-left corner.
        /// Only the top-left quadrant is masked, concentrating the centroid near (W/4, H/4).
        /// All W×H pixels still get bucketed from that centroid, so buckets pointing toward
        /// the far corner are large, but only mask-inside pixels contribute to runs.
        /// </summary>
        [Fact]
        public void Sorter_IntoMask_AsymmetricMask_CentroidNearCorner_DoesNotOverflow()
        {
            const int size = 500;
            var pixelData = new float[size * size * 3];
            var imgData = np.array(pixelData).reshape(size, size, 3);

            // Mask only top-left quadrant → centroid ≈ (size/4, size/4)
            var maskData = new byte[size * size];
            for (int y = 0; y < size / 2; y++)
                for (int x = 0; x < size / 2; x++)
                    maskData[y * size + x] = 255;

            var mask = np.array(maskData).reshape(size, size, 1);

            var result = Sorter.SortImage(imgData, PixelsorterClassLib.Core.SortBy.Lightness(), SortDirections.IntoMask, mask);

            Assert.NotNull(result);
        }

        /// <summary>
        /// Scenario 3: Thin edge mask to push the centroid as close to an edge as possible
        /// while still having enough mask pixels to form meaningful runs.
        /// Mask only the first row → centroid at (W/2, 0).
        /// </summary>
        [Fact]
        public void Sorter_IntoMask_EdgeMask_CentroidAtEdge_DoesNotOverflow()
        {
            const int size = 500;
            var pixelData = new float[size * size * 3];
            var imgData = np.array(pixelData).reshape(size, size, 3);

            // Mask only the first row → centroid at (size/2, 0)
            var maskData = new byte[size * size];
            for (int x = 0; x < size; x++)
                maskData[x] = 255; // y=0 row

            var mask = np.array(maskData).reshape(size, size, 1);

            var result = Sorter.SortImage(imgData, PixelsorterClassLib.Core.SortBy.Lightness(), SortDirections.IntoMask, mask);

            Assert.NotNull(result);
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
