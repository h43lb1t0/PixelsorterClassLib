using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Pixelsorter.Tests.ImageTests
{
    public class NDArrayToImageDataTests
    {
        [Fact]
        public void SaveImage_SavesL8MaskImage()
        {
            string outPath = Path.Combine(Path.GetTempPath(), $"test_save_mask_image_{Guid.NewGuid()}.png");

            try
            {
                using var mask = new Image<L8>(2, 1);
                mask[0, 0] = new L8(255);
                mask[1, 0] = new L8(0);

                PixelsorterClassLib.Image.SaveImage(mask, outPath);

                Assert.True(File.Exists(outPath));
                Assert.True(new FileInfo(outPath).Length > 0);
            }
            finally
            {
                if (File.Exists(outPath)) File.Delete(outPath);
            }
        }

        [Fact]
        public void LoadImage_LoadsRgbaPixels()
        {
            string path = Path.Combine(Path.GetTempPath(), $"test_load_rgba_{Guid.NewGuid()}.png");

            try
            {
                using (var original = new Image<Rgba32>(2, 1))
                {
                    original[0, 0] = new Rgba32(255, 128, 64, 100);
                    original[1, 0] = new Rgba32(10, 20, 30, 200);
                    original.Save(path);
                }

                using var loaded = PixelsorterClassLib.Image.LoadImage(path);

                Assert.Equal(2, loaded.Width);
                Assert.Equal(1, loaded.Height);
                Assert.Equal(new Rgba32(255, 128, 64, 100), loaded[0, 0]);
                Assert.Equal(new Rgba32(10, 20, 30, 200), loaded[1, 0]);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
