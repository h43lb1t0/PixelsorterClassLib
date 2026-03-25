using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace PixelsorterClassLib.Masks
{

    /// <summary>
    /// Specifies options for generating a mask using the Canny edge detection algorithm.
    /// </summary>
    /// <param name="Threshold">The threshold value used to determine edge sensitivity. Higher values result in fewer detected edges. Must be
    /// non-negative.</param>
    public record CannyMaskOptions : MaskOptions
    {
        public float Threshold { get; init; }

        public CannyMaskOptions(float threshold)
        {
            if (threshold < 0 || threshold >= 1) throw new ArgumentException("Threshold needs to be betwenn range (0, 1] (0,100 %)");
        }
    }
    
    public class CannyMask : Mask<CannyMaskOptions>
    {

        /// <summary>
        /// The threshold value for the binary thresholding step in the Canny edge detection process. 
        /// This value determines the sensitivity of edge detection, with lower values resulting in more edges being detected 
        /// and higher values resulting in fewer edges. The threshold is set via <see cref="CannyMaskOptions.Threshold"/> 
        /// and should be in the range (0, 1].
        /// </summary>
        private float threshold = 0.3f;

        /// <summary>
        /// Generates a binary mask and its inverted counterpart from the specified image using Canny-like edge
        /// detection and thresholding.
        /// </summary>
        /// <remarks>The returned masks can be used for further image processing tasks such as
        /// segmentation or region selection. The method applies Gaussian blur, edge detection, and binary thresholding
        /// to produce the masks.</remarks>
        /// <param name="image">The source image to process. Must be a non-null image in the Rgba32 pixel format.</param>
        /// <returns>A tuple containing the binary mask and the inverted mask as images in the L8 pixel format. The first item is
        /// the mask, and the second item is the inverted mask.</returns>
        private (Image<L8> mask, Image<L8> invertedMask) CreateCannyMask(Image<Rgba32> image)
        {
            var invertedMask = image.CloneAs<L8>();
            invertedMask.Mutate(x => x.GaussianBlur(1f)
                             .DetectEdges()
                             .BinaryThreshold(threshold));

            var mask = invertedMask.Clone(x => x.Invert());
            return (mask, invertedMask);
        }

        /// <summary>
        /// Loads an image from the specified file path using the RGBA32 pixel format.
        /// </summary>
        /// <param name="inputImagePath">The path to the image file to load. Must refer to a valid image file; cannot be null or empty.</param>
        /// <returns>An instance of Image<Rgba32> representing the loaded image.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the image cannot be loaded from the specified path.</exception>
        private Image<Rgba32> LoadImage(string inputImagePath)
        {
            return Image.Load<Rgba32>(inputImagePath) ?? throw new InvalidOperationException("Failed to load the input image.");
        }

        /// <summary>
        /// Generates a mask and an inverted mask from the specified image using edge detection.
        /// </summary>
        /// <remarks>The method uses Canny edge detection to create the masks.</remarks>
        /// <param name="imagePath">The file path to the image from which the mask will be generated. Cannot be null or empty.</param>
        /// <param name="options">The options controlling mask generation, including the edge detection threshold.</param>
        /// <returns>A tuple containing two NDArray objects: the first is the mask representing detected edges, and the second is
        /// the inverted mask. Both arrays will have the same dimensions as the input image.</returns>
        public override (NDArray mask, NDArray invertedMask) GetMask(string imagePath, CannyMaskOptions options)
        {
            this.threshold = options.Threshold;
            using var image = LoadImage(imagePath);
            (var mask, var invertedMask) = CreateCannyMask(image);
            return (ConvertMaskToNdArray(mask), ConvertMaskToNdArray(invertedMask));
        }

        /// <summary>
        /// Generates a binary mask and its inverted mask for the specified image using edge detection.
        /// </summary>
        /// <remarks>The mask is generated using a Canny edge detection algorithm. The operation is
        /// performed asynchronously and supports cancellation.</remarks>
        /// <param name="imagePath">The file path of the image to process. Must refer to a valid image file.</param>
        /// <param name="options">The options controlling mask generation, including the edge detection threshold.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the mask generation operation.</param>
        /// <returns>A task that represents the asynchronous operation. The result contains a tuple with the mask and inverted
        /// mask as NDArray objects.</returns>
        public override Task<(NDArray mask, NDArray invertedMask)> GetMaskAsync(string imagePath, CannyMaskOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                this.threshold = options.Threshold;
                cancellationToken.ThrowIfCancellationRequested();
                using var image = LoadImage(imagePath);
                cancellationToken.ThrowIfCancellationRequested();
                (var mask, var invertedMask) = CreateCannyMask(image);
                return (ConvertMaskToNdArray(mask), ConvertMaskToNdArray(invertedMask));
            }, cancellationToken);
        }
    }
}
