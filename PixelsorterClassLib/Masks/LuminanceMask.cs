using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Text;

namespace PixelsorterClassLib.Masks
{
    public class LuminanceMask : Mask
    {

        private float thresholdMultiplier = 0.5f;

        private float GetLuminancThreashold(Image<L8> image)
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

        private (NDArray mask, NDArray invertedMask) CreateLuminanceMask(Image<L8> image)
        {
   
            image.Mutate(x => x.BinaryThreshold(GetLuminancThreashold(image)));

            var mask = image.Clone(x => x.Invert());
            return (ConvertMaskToNdArray(mask), ConvertMaskToNdArray(image));
        }

        private Image<L8> LoadImage(string inputImagePath)
        {
            return Image.Load<L8>(inputImagePath) ?? throw new InvalidOperationException("Failed to load the input image.");
        }

        public override (NDArray mask, NDArray invertedMask) GetMask(string imagePath, int fadeWidth)
        {
            using var image = LoadImage(imagePath);
            this.thresholdMultiplier = fadeWidth / 100f;
            return CreateLuminanceMask(image);
        }

        public override Task<(NDArray mask, NDArray invertedMask)> GetMaskAsync(string imagePath, int fadeWidth, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                using var image = LoadImage(imagePath);
                cancellationToken.ThrowIfCancellationRequested();
                this.thresholdMultiplier = fadeWidth / 100f;
                return CreateLuminanceMask(image);
            }, cancellationToken);
        }
    }
}
