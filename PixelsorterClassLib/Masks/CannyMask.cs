using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace PixelsorterClassLib.Masks
{
    public class CannyMask : Mask
    {

        private (Image<L8> mask, Image<L8> invertedMask) CreateCannyMask(Image<Rgba32> image)
        {
            var invertedMask = image.CloneAs<L8>();
            // 1. Apply Gaussian blur to reduce noise
            // 2. Use sobel gradient to detect edges
            // 3. Non-maximum suppression to thin edges
            // Hysteresis thresholding to connect edges
            invertedMask.Mutate(x => x.GaussianBlur(1f)
                             .DetectEdges()
                             .BinaryThreshold(0.3f));

            var mask = invertedMask.Clone(x => x.Invert());
            return (mask, invertedMask);
        }

        private Image<Rgba32> LoadImage(string inputImagePath)
        {
            return Image.Load<Rgba32>(inputImagePath) ?? throw new InvalidOperationException("Failed to load the input image.");
        }

        public override (NDArray mask, NDArray invertedMask) GetMask(string imagePath, int fadeWidth = 0)
        {
            using var image = LoadImage(imagePath);
            (var mask, var invertedMask) = CreateCannyMask(image);
            return (ConvertMaskToNdArray(mask), ConvertMaskToNdArray(invertedMask));
        }

        public override Task<(NDArray mask, NDArray invertedMask)> GetMaskAsync(string imagePath, int fadeWidth = 0, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var image = LoadImage(imagePath);
                cancellationToken.ThrowIfCancellationRequested();
                (var mask, var invertedMask) = CreateCannyMask(image);
                return (ConvertMaskToNdArray(mask), ConvertMaskToNdArray(invertedMask));
            }, cancellationToken);
        }
    }
}
