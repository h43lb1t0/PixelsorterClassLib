using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace PixelsorterClassLib.Masks
{
    public abstract class Mask
    {
        public virtual bool IsReadyToUse => true;
        public virtual async Task<bool> DownloadModel()
        {
            return true;
        }

        public abstract(NDArray mask, NDArray invertedMask) GetMask(String imagePath, int fadeWidth = 0);

        public abstract Task<(NDArray mask, NDArray invertedMask)> GetMaskAsync(String imagePath, int fadeWidth = 0, CancellationToken cancellationToken = default);

        internal static NDArray ConvertMaskToNdArray(Image<L8> mask)
        {
            var data = new byte[mask.Width * mask.Height];

            mask.ProcessPixelRows(accessor =>
            {
                int index = 0;
                for (int y = 0; y < mask.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < mask.Width; x++)
                    {
                        data[index++] = row[x].PackedValue;
                    }
                }
            });
            return np.array(data).reshape(new Shape(mask.Height, mask.Width, 1));
        }
    }
}
