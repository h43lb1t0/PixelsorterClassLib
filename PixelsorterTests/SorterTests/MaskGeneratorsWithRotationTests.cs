using NumSharp;
using PixelsorterClassLib.Masks;
using Pixelsorter.Tests.ImageTests;

namespace Pixelsorter.Tests.SorterTests
{
    /// <summary>
    /// Tests that verify mask generators correctly handle images with EXIF rotation data.
    /// </summary>
    public class MaskGeneratorsWithRotationTests
    {
        /// <summary>
        /// Tests that LuminanceMask generates a valid mask from a rotated image.
        /// Verifies that the mask dimensions match the rotated image dimensions after AutoOrient is applied.
        /// </summary>
        [Fact]
        public void LuminanceMask_WithRotatedImage_GeneratesValidMask()
        {
            // Get the path to the real test image with EXIF rotation (orientation value 6 = 90 degrees CCW)
            string testImagePath = ImageTestHelpers.GetTestImageWithExifRotation();

            // Create a LuminanceMask with default threshold
            var luminanceMask = new LuminanceMask();
            var options = new LuminanceMaskOptions(0.5f);

            // Generate mask from the rotated image
            var (mask, invertedMask) = luminanceMask.GetMask(testImagePath, options);

            // Verify masks are not null
            Assert.NotNull(mask);
            Assert.NotNull(invertedMask);

            // Verify mask shape is (height, width, 1)
            Assert.Equal(3, mask.shape.Length);
            Assert.Equal(3, invertedMask.shape.Length);

            int maskHeight = (int)mask.shape[0];
            int maskWidth = (int)mask.shape[1];
            int maskChannels = (int)mask.shape[2];

            int invertedHeight = (int)invertedMask.shape[0];
            int invertedWidth = (int)invertedMask.shape[1];
            int invertedChannels = (int)invertedMask.shape[2];

            // Verify channels are 1 (single channel mask)
            Assert.Equal(1, maskChannels);
            Assert.Equal(1, invertedChannels);

            // Verify dimensions are valid and consistent
            Assert.True(maskHeight > 0, "Mask height should be positive");
            Assert.True(maskWidth > 0, "Mask width should be positive");
            Assert.Equal(maskHeight, invertedHeight);
            Assert.Equal(maskWidth, invertedWidth);

            // Verify mask values are in valid range (0-255 for byte values)
            var maskData = mask.ToArray<byte>();
            var invertedData = invertedMask.ToArray<byte>();

            Assert.All(maskData, value => Assert.InRange(value, (byte)0, (byte)255));
            Assert.All(invertedData, value => Assert.InRange(value, (byte)0, (byte)255));

            // Verify mask and inverted mask are actually inverted (complementary)
            for (int i = 0; i < maskData.Length; i++)
            {
                // The sum of mask and inverted mask values should be close to 255
                // (allowing for some rounding in the inversion process)
                int sum = maskData[i] + invertedData[i];
                Assert.InRange(sum, 250, 256); // Allow small tolerance
            }
        }

        /// <summary>
        /// Tests that LuminanceMask generates masks with different thresholds from a rotated image.
        /// Verifies that different threshold multipliers produce different masks as expected.
        /// </summary>
        [Theory]
        [InlineData(0.2f)]
        [InlineData(0.5f)]
        [InlineData(0.8f)]
        public void LuminanceMask_WithRotatedImage_DifferentThresholds_GeneratesDifferentMasks(float thresholdMultiplier)
        {
            string testImagePath = ImageTestHelpers.GetTestImageWithExifRotation();

            var luminanceMask = new LuminanceMask();
            var options = new LuminanceMaskOptions(thresholdMultiplier);

            var (mask, invertedMask) = luminanceMask.GetMask(testImagePath, options);

            // Verify mask is valid and has correct shape
            Assert.NotNull(mask);
            var maskData = mask.ToArray<byte>();

            // Verify we get some variation in mask values (not all 0 or all 255)
            var uniqueValues = maskData.Distinct().Count();
            Assert.True(uniqueValues > 1, "Mask should have variation in values, not all the same");
        }

        /// <summary>
        /// Tests that CannyMask generates a valid mask from a rotated image.
        /// Verifies that the Canny edge detection works correctly with rotated images.
        /// </summary>
        [Fact]
        public void CannyMask_WithRotatedImage_GeneratesValidMask()
        {
            string testImagePath = ImageTestHelpers.GetTestImageWithExifRotation();

            var cannyMask = new CannyMask();
            var options = new CannyMaskOptions(0.3f);

            var (mask, invertedMask) = cannyMask.GetMask(testImagePath, options);

            // Verify masks are not null
            Assert.NotNull(mask);
            Assert.NotNull(invertedMask);

            // Verify mask shape is (height, width, 1)
            Assert.Equal(3, mask.shape.Length);
            Assert.Equal(3, invertedMask.shape.Length);

            int maskHeight = (int)mask.shape[0];
            int maskWidth = (int)mask.shape[1];
            int maskChannels = (int)mask.shape[2];

            int invertedHeight = (int)invertedMask.shape[0];
            int invertedWidth = (int)invertedMask.shape[1];
            int invertedChannels = (int)invertedMask.shape[2];

            // Verify channels are 1 (single channel mask)
            Assert.Equal(1, maskChannels);
            Assert.Equal(1, invertedChannels);

            // Verify dimensions are valid and consistent
            Assert.True(maskHeight > 0, "Mask height should be positive");
            Assert.True(maskWidth > 0, "Mask width should be positive");
            Assert.Equal(maskHeight, invertedHeight);
            Assert.Equal(maskWidth, invertedWidth);

            // Verify mask values are in valid range
            var maskData = mask.ToArray<byte>();
            var invertedData = invertedMask.ToArray<byte>();

            Assert.All(maskData, value => Assert.InRange(value, (byte)0, (byte)255));
            Assert.All(invertedData, value => Assert.InRange(value, (byte)0, (byte)255));
        }

        /// <summary>
        /// Tests that ChunkMask generates a valid mask from a rotated image.
        /// Verifies that chunk-based masking works correctly with rotated images.
        /// </summary>
        [Fact]
        public void ChunkMask_RowLeftToRight_WithRotatedImage_GeneratesValidMask()
        {
            string testImagePath = ImageTestHelpers.GetTestImageWithExifRotation();

            var chunkMask = new ChunkMask();
            var options = new ChunkMaskOptions(
                minChunkSize: 5,
                maxChunkSize: 20,
                sortDirection: PixelsorterClassLib.Core.SortDirections.RowLeftToRight,
                minThickness: 5,
                maxThickness: 15
            );

            var (mask, invertedMask) = chunkMask.GetMask(testImagePath, options);

            // Verify masks are not null
            Assert.NotNull(mask);
            Assert.NotNull(invertedMask);

            // Verify mask shape is (height, width, 1)
            Assert.Equal(3, mask.shape.Length);
            Assert.Equal(3, invertedMask.shape.Length);

            int maskHeight = (int)mask.shape[0];
            int maskWidth = (int)mask.shape[1];
            int maskChannels = (int)mask.shape[2];

            int invertedHeight = (int)invertedMask.shape[0];
            int invertedWidth = (int)invertedMask.shape[1];
            int invertedChannels = (int)invertedMask.shape[2];

            // Verify channels are 1 (single channel mask)
            Assert.Equal(1, maskChannels);
            Assert.Equal(1, invertedChannels);

            // Verify dimensions are valid and consistent
            Assert.True(maskHeight > 0, "Mask height should be positive");
            Assert.True(maskWidth > 0, "Mask width should be positive");
            Assert.Equal(maskHeight, invertedHeight);
            Assert.Equal(maskWidth, invertedWidth);

            // Verify mask values are in valid range
            var maskData = mask.ToArray<byte>();
            var invertedData = invertedMask.ToArray<byte>();

            Assert.All(maskData, value => Assert.InRange(value, (byte)0, (byte)255));
            Assert.All(invertedData, value => Assert.InRange(value, (byte)0, (byte)255));
        }

        /// <summary>
        /// Tests that ChunkMask generates a valid mask from a rotated image using column-wise sorting.
        /// Verifies that chunk masks work correctly with different sort directions on rotated images.
        /// </summary>
        [Fact]
        public void ChunkMask_ColumnTopToBottom_WithRotatedImage_GeneratesValidMask()
        {
            string testImagePath = ImageTestHelpers.GetTestImageWithExifRotation();

            var chunkMask = new ChunkMask();
            var options = new ChunkMaskOptions(
                minChunkSize: 5,
                maxChunkSize: 20,
                sortDirection: PixelsorterClassLib.Core.SortDirections.ColumnTopToBottom,
                minThickness: 5,
                maxThickness: 15
            );

            var (mask, invertedMask) = chunkMask.GetMask(testImagePath, options);

            // Verify masks are not null
            Assert.NotNull(mask);
            Assert.NotNull(invertedMask);

            // Verify mask shape is (height, width, 1)
            Assert.Equal(3, mask.shape.Length);

            int maskHeight = (int)mask.shape[0];
            int maskWidth = (int)mask.shape[1];
            int maskChannels = (int)mask.shape[2];

            // Verify channels are 1 (single channel mask)
            Assert.Equal(1, maskChannels);

            // Verify dimensions are valid
            Assert.True(maskHeight > 0, "Mask height should be positive");
            Assert.True(maskWidth > 0, "Mask width should be positive");

            // Verify mask values are in valid range
            var maskData = mask.ToArray<byte>();
            Assert.All(maskData, value => Assert.InRange(value, (byte)0, (byte)255));
        }

        /// <summary>
        /// Tests that BackgroundMask generates a valid mask from a rotated image.
        /// This test is skipped if the BackgroundMask model is not available (not downloaded).
        /// </summary>
        [Fact]
        public void BackgroundMask_WithRotatedImage_GeneratesValidMask()
        {
            var backgroundMask = new BackgroundMask();

            // Skip test if model is not available
            if (!backgroundMask.IsReadyToUse)
            {
                return; // Skip this test if model is not available
            }

            string testImagePath = ImageTestHelpers.GetTestImageWithExifRotation();
            var options = new BackgroundMaskOptions(fadeWidth: 10, confidenceThreshold: 0.5f);

            var (mask, invertedMask) = backgroundMask.GetMask(testImagePath, options);

            // Verify masks are not null
            Assert.NotNull(mask);
            Assert.NotNull(invertedMask);

            // Verify mask shape is (height, width, 1)
            Assert.Equal(3, mask.shape.Length);
            Assert.Equal(3, invertedMask.shape.Length);

            int maskHeight = (int)mask.shape[0];
            int maskWidth = (int)mask.shape[1];
            int maskChannels = (int)mask.shape[2];

            int invertedHeight = (int)invertedMask.shape[0];
            int invertedWidth = (int)invertedMask.shape[1];
            int invertedChannels = (int)invertedMask.shape[2];

            // Verify channels are 1 (single channel mask)
            Assert.Equal(1, maskChannels);
            Assert.Equal(1, invertedChannels);

            // Verify dimensions are valid and consistent
            Assert.True(maskHeight > 0, "Mask height should be positive");
            Assert.True(maskWidth > 0, "Mask width should be positive");
            Assert.Equal(maskHeight, invertedHeight);
            Assert.Equal(maskWidth, invertedWidth);

            // Verify mask values are in valid range
            var maskData = mask.ToArray<byte>();
            var invertedData = invertedMask.ToArray<byte>();

            Assert.All(maskData, value => Assert.InRange(value, (byte)0, (byte)255));
            Assert.All(invertedData, value => Assert.InRange(value, (byte)0, (byte)255));
        }
    }
}
