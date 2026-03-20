using NumSharp;
using PixelsorterClassLib.Core;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Pixelsorter.Tests.ImageTests
{
    public class NDArrayToImageDataTests
    {
        [Fact]
        public void NdarrayToImageData_ConvertsColorImageCorrectly()
        {
            // 3 channels (HSL) - pure red and pure green
            var data = new float[] { 0f, 1f, 0.5f, 120f, 1f, 0.5f }; // 1x2 image
            var ndArray = np.array(data).reshape(1, 2, 3);

            using var image = Image.NdarrayToImgData(ndArray);

            Assert.Equal(2, image.Width);
            Assert.Equal(1, image.Height);

            Assert.Equal(new Rgba32(255, 0, 0, 255), image[0, 0]);
            Assert.Equal(new Rgba32(0, 255, 0, 255), image[1, 0]);
        }

        [Fact]
        public void NdarrayToImageData_ConvertsGrayscaleImageCorrectly()
        {
            // 1 channel (Grayscale) - float values 0-1
            var data = new float[] { 1.0f, 0.0f }; // 1x2 image: white and black
            var ndArray = np.array(data).reshape(1, 2, 1);

            using var image = Image.NdarrayToImgData(ndArray);

            Assert.Equal(2, image.Width);
            Assert.Equal(1, image.Height);

            Assert.Equal(new Rgba32(255, 255, 255, 255), image[0, 0]);
            Assert.Equal(new Rgba32(0, 0, 0, 255), image[1, 0]);
        }

        [Fact]
        public void NdarrayToImageData_ConvertsTransparentImageCorrectly()
        {
            // 4 channels (HSL + Alpha) - pure red with full alpha, pure green with zero alpha
            var data = new float[] { 0f, 1f, 0.5f, 1f, 120f, 1f, 0.5f, 0f }; // 1x2 image
            var ndArray = np.array(data).reshape(1, 2, 4);

            using var image = Image.NdarrayToImgData(ndArray);

            Assert.Equal(2, image.Width);
            Assert.Equal(1, image.Height);

            Assert.Equal(new Rgba32(255, 0, 0, 255), image[0, 0]);
            Assert.Equal(new Rgba32(0, 255, 0, 0), image[1, 0]);
        }

        [Fact]
        public void NdarrayToImageData_ThrowsOnInvalidChannelCount()
        {
            var data = new float[] { 1f, 0.5f }; // 1x1 image, 2 channels
            var ndArray = np.array(data).reshape(1, 1, 2);

            Assert.Throws<InvalidOperationException>(() => Image.NdarrayToImgData(ndArray));
        }
    }
}
