using PixelsorterClassLib.Core;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace Pixelsorter.Tests.ImageTests
{
    public class ImageScalingTests
    {
        [Fact]
        public void Scale4kLandscape_ShouldScaleTo1080p()
        {
            var inPath = ImageTestHelpers.CreateTestImage(".png", 3, 3840, 2160);
            try
            {
                var (image, _) = Image.LoadImage(inPath, KnownResamplers.Bicubic);
                var outImg = Image.NdarrayToImgData(image);
                Assert.Equal(1920, outImg.Width);
                Assert.Equal(1080, outImg.Height);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception thrown during test: {ex}");

            }
            finally
            {
                if (File.Exists(inPath)) File.Delete(inPath);

            }
        }

        [Fact]
        public void Scale4kImagdownAndUpAgain_ShouldBe4k()
        {
            var inPath = ImageTestHelpers.CreateTestImage(".png", 3, 3840, 2160);
            var outPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
            try
            {
                var (image, size) = Image.LoadImage(inPath, KnownResamplers.Bicubic);
                Image.SaveImage(image, outPath, KnownResamplers.Bicubic, size);

                var savedImage = SixLabors.ImageSharp.Image.Load<Rgb24>(outPath);
                Assert.Equal(3840, savedImage.Width);
                Assert.Equal(2160, savedImage.Height);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception thrown during test: {ex}");
            }
            finally
            {
                if (File.Exists(inPath)) File.Delete(inPath);
                if (File.Exists(outPath)) File.Delete(outPath);

            }
        }

        [Fact]
        public void Scale720pImage_ShouldNotScale()
        {
            var inPath = ImageTestHelpers.CreateTestImage(".png", 3, 1280, 720);
            try
            {
                var (image, _) = Image.LoadImage(inPath, KnownResamplers.Bicubic);
                var outImg = Image.NdarrayToImgData(image);
                Assert.Equal(1280, outImg.Width);
                Assert.Equal(720, outImg.Height);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception thrown during test: {ex}");
            }
            finally
            {
                if (File.Exists(inPath)) File.Delete(inPath);
            }
        }
    }
}
