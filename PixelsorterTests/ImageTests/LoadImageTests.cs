using PixelsorterClassLib.Core;

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
                var image = Image.LoadImage(path);

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

        public static TheoryData<string> GetExtensions()
        {
            var data = new TheoryData<string>();
            foreach (var ext in ImageTestHelpers.FileExtensions)
            {
                data.Add(ext);
            }
            return data;
        }

        [Fact]
        public void LoadImage_ShouldThrowOnCorruptedFile()
        {
            string path = ImageTestHelpers.CreateCoruptedImage(".png");
            try
            {
                Assert.ThrowsAny<Exception>(() => Image.LoadImage(path));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Theory]
        [MemberData(nameof(GetExtensions))]
        public void LoadImage_WithAlpha_ShouldLoadSuccessfully(string extension)
        {
            string path = ImageTestHelpers.CreateTestImageWithAlpha(extension);
            try
            {
                var image = Image.LoadImage(path);
                Assert.NotNull(image);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Theory]
        [MemberData(nameof(GetExtensions))]
        public void LoadImage_Grayscale_ShouldLoadSuccessfully(string extension)
        {
            string path = ImageTestHelpers.CreateGrayscaleTestImage(extension);
            try
            {
                var image = Image.LoadImage(path);
                Assert.NotNull(image);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void LoadImage_WithExifRotation_ShouldAutoOrient()
        {
            // Use the real test image that has EXIF orientation value 6 (90 degrees CCW)
            // Raw stored dimensions: 800x1000 -> After AutoOrient: 1000x800
            string testImagePath = ImageTestHelpers.GetTestImageWithExifRotation();

            // Get the raw stored dimensions WITHOUT AutoOrient
            var (rawWidth, rawHeight) = ImageTestHelpers.GetRawTestImageDimensions();

            // Load the image WITH AutoOrient (which Image.LoadImage applies)
            var image = Image.LoadImage(testImagePath);

            // Verify image is loaded successfully
            Assert.NotNull(image);

            // Verify the array has correct shape (height x width x channels)
            var shape = image.shape;
            Assert.Equal(3, shape.Length); // Should be 3D array

            int orientedHeight = (int)shape[0];
            int orientedWidth = (int)shape[1];
            int channels = (int)shape[2];

            // Verify channels are 3 (RGB converted to HSL)
            Assert.Equal(3, channels);

            // Verify dimensions are valid and non-zero
            Assert.True(orientedHeight > 0, "Image height should be positive");
            Assert.True(orientedWidth > 0, "Image width should be positive");

            // CRITICAL ASSERTION: Verify that AutoOrient actually rotated the image
            // For EXIF orientation 6 (90 degrees CCW), the raw dimensions should be swapped
            // Raw image: (rawWidth, rawHeight) -> After 90° rotation: (rawHeight, rawWidth)
            Assert.Equal(rawHeight, orientedWidth);
            Assert.True(rawHeight == orientedWidth, 
                $"After 90° CCW rotation (EXIF 6), oriented width should equal raw height. " +
                $"Raw: {rawWidth}x{rawHeight}, Oriented: {orientedWidth}x{orientedHeight}");

            Assert.Equal(rawWidth, orientedHeight);
            Assert.True(rawWidth == orientedHeight,
                $"After 90° CCW rotation (EXIF 6), oriented height should equal raw width. " +
                $"Raw: {rawWidth}x{rawHeight}, Oriented: {orientedWidth}x{orientedHeight}");

            // Verify pixel data is consistent with dimensions
            var pixelData = image.ToArray<float>();
            Assert.True(pixelData.Length == orientedHeight * orientedWidth * channels, 
                $"Pixel data length should match height*width*channels: {pixelData.Length} vs {orientedHeight * orientedWidth * channels}");

            // Verify the HSL values are in valid ranges
            for (int i = 0; i < pixelData.Length; i += 3)
            {
                // Hue should be 0-360
                Assert.InRange(pixelData[i], 0f, 360f);
                // Saturation should be 0-1
                Assert.InRange(pixelData[i + 1], 0f, 1f);
                // Lightness should be 0-1
                Assert.InRange(pixelData[i + 2], 0f, 1f);
            }
        }
    }
}
