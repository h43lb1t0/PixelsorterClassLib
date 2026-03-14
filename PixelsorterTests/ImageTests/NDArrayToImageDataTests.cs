using NumSharp;
using PixelsorterClassLib.Core;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Pixelsorter.Tests.ImageTests
{
    public class NDArrayToImageDataTests
    {
        [Fact]
        public void NdarrayToImageData_ConvertsColorImageCorrectly()
        {
            // 3 channels (RGB)
            var data = new byte[] { 255, 128, 64, 10, 20, 30 }; // 1x2 image
            var ndArray = np.array(data).reshape(1, 2, 3);

            using var image = Image.NdarrayToImgData(ndArray);

            Assert.Equal(2, image.Width);
            Assert.Equal(1, image.Height);

            Assert.Equal(new Rgba32(255, 128, 64, 255), image[0, 0]);
            Assert.Equal(new Rgba32(10, 20, 30, 255), image[1, 0]);
        }

        [Fact]
        public void NdarrayToImageData_ConvertsGrayscaleImageCorrectly()
        {
            // 1 channel (Grayscale)
            var data = new byte[] { 255, 128 }; // 1x2 image
            var ndArray = np.array(data).reshape(1, 2, 1);

            using var image = Image.NdarrayToImgData(ndArray);

            Assert.Equal(2, image.Width);
            Assert.Equal(1, image.Height);

            Assert.Equal(new Rgba32(255, 255, 255, 255), image[0, 0]);
            Assert.Equal(new Rgba32(128, 128, 128, 255), image[1, 0]);
        }

        [Fact]
        public void NdarrayToImageData_ConvertsTransparentImageCorrectly()
        {
            // 4 channels (RGBA)
            var data = new byte[] { 255, 128, 64, 100, 10, 20, 30, 0 }; // 1x2 image
            var ndArray = np.array(data).reshape(1, 2, 4);

            using var image = Image.NdarrayToImgData(ndArray);

            Assert.Equal(2, image.Width);
            Assert.Equal(1, image.Height);

            Assert.Equal(new Rgba32(255, 128, 64, 100), image[0, 0]);
            Assert.Equal(new Rgba32(10, 20, 30, 0), image[1, 0]);
        }

        [Fact]
        public void NdarrayToImageData_ThrowsOnInvalidChannelCount()
        {
            var data = new byte[] { 255, 128 }; // 1x1 image, 2 channels
            var ndArray = np.array(data).reshape(1, 1, 2);

            Assert.Throws<InvalidOperationException>(() => Image.NdarrayToImgData(ndArray));
        }
    }
}
