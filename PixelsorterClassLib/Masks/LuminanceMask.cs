using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Text;

namespace PixelsorterClassLib.Masks
{


    /// <summary>
    /// Represents options for generating a mask based on luminance values, using a specified threshold multiplier.
    /// </summary>
    /// <param name="ThresholdMultiplier">The multiplier applied to the luminance threshold to determine which pixels are included in the mask. Must be a
    /// non-negative value.</param>
    public record LuminanceMaskOptions : MaskOptions
    {
        public float ThresholdMultiplier {  get; init; }

        public LuminanceMaskOptions(float thresholdMultiplier)
        {
            if (thresholdMultiplier < 0 || thresholdMultiplier >= 1) throw new ArgumentException("ThresholdMultiplier needs to be betwenn range (0, 1] (0,100 %)");
            ThresholdMultiplier = thresholdMultiplier;
        }

        
    } 

    /// <summary>
    /// Provides a mask based on the luminance values of an image, generating binary masks by thresholding pixel
    /// intensities.
    /// </summary>
    /// <remarks>The luminance threshold is dynamically calculated based on the minimum and maximum pixel
    /// values in the image and is influenced by the <see cref="LuminanceMaskOptions.ThresholdMultiplier"/>. 
    /// Thread safety is not guaranteed; concurrent use of the same instance is not supported.</remarks>
    public class LuminanceMask : Mask<LuminanceMaskOptions>
    {

        /// <summary>
        /// Float multiplier resp. divider for the mean luminace calculation
        /// </summary>
        private float thresholdMultiplier = 0.5f;

        /// <summary>
        /// Calculates a luminance threshold value based on the minimum and maximum pixel values in the specified
        /// grayscale image.
        /// </summary>
        /// <remarks>The threshold is determined by analyzing the range of pixel intensities in the image.
        /// </remarks>
        /// <param name="image">The grayscale image from which to compute the luminance threshold. Must not be null.</param>
        /// <returns>A normalized threshold value between 0.0 and 1.0, representing a computed luminance level for the image.</returns>
        private float GetLuminanceThreshold(Image<L8> image)
        {
            byte min = 255;
            byte max = 0;

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<L8> row = accessor.GetRowSpan(y);
                    foreach (ref L8 pixel in row)
                    {
                        byte val = pixel.PackedValue;
                        if (val < min) min = val;
                        if (val > max) max = val;
                    }
                }
            });

            return (min + (max - min) * thresholdMultiplier) / 255.0f;
        }

        /// <summary>
        /// Creates a luminance-based binary mask and its inverse from the specified grayscale image.
        /// </summary>
        /// <remarks>The method applies a binary threshold to the input image based on its luminance, then
        /// generates both the mask and its inverse as NDArrays. The input image is mutated during processing.</remarks>
        /// <param name="image">The grayscale image to process. Must not be null.</param>
        /// <returns>A tuple containing the luminance mask as an NDArray and the inverted mask as an NDArray.</returns>
        private (NDArray mask, NDArray invertedMask) CreateLuminanceMask(Image<L8> image)
        {
   
            image.Mutate(x => x.BinaryThreshold(GetLuminanceThreshold(image)));

            var mask = image.Clone(x => x.Invert());
            return (ConvertMaskToNdArray(mask), ConvertMaskToNdArray(image));
        }

        private Image<L8> LoadImage(string inputImagePath)
        {
            return Image.Load<L8>(inputImagePath) ?? throw new InvalidOperationException("Failed to load the input image.");
        }

        public override (NDArray mask, NDArray invertedMask) GetMask(string imagePath, LuminanceMaskOptions options)
        {
            using var image = LoadImage(imagePath);
            this.thresholdMultiplier = options.ThresholdMultiplier;
            return CreateLuminanceMask(image);
        }

        public override Task<(NDArray mask, NDArray invertedMask)> GetMaskAsync(string imagePath, LuminanceMaskOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                using var image = LoadImage(imagePath);
                cancellationToken.ThrowIfCancellationRequested();
                this.thresholdMultiplier = options.ThresholdMultiplier;
                return CreateLuminanceMask(image);
            }, cancellationToken);
        }
    }
}
