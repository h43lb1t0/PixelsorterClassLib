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
        /// <summary>
        /// Gets a value indicating whether the object is ready to be used.
        /// For masks that require a model, this property should return true if the model is already downloaded and ready for use, 
        /// and false if the model needs to be downloaded or is not available. For masks that do not require a model, 
        /// this property can simply return true.
        /// </summary>
        public virtual bool IsReadyToUse => true;

        /// <summary>
        /// Initiates the download of the model asynchronously. If the mask does not require a model, 
        /// this method can return a completed task with a result of true.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the model
        /// was downloaded successfully; otherwise, <see langword="false"/>.</returns>
        public virtual async Task<bool> DownloadModel()
        {
            return true;
        }

        public abstract(NDArray mask, NDArray invertedMask) GetMask(String imagePath, int fadeWidth = 0);

        public abstract Task<(NDArray mask, NDArray invertedMask)> GetMaskAsync(String imagePath, int fadeWidth = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts a single-channel grayscale image mask to an NDArray with shape (height, width, 1).
        /// </summary>
        /// <remarks>The resulting NDArray can be used for further numerical processing or as input to
        /// machine learning models. The mask is flattened and reshaped to preserve spatial dimensions. The method
        /// assumes the input mask is of type L8; other types may result in incorrect conversion.</remarks>
        /// <param name="mask">The grayscale image mask to convert. Must be a single-channel image of type L8.</param>
        /// <returns>An NDArray representing the mask, with shape (height, width, 1) and pixel values as bytes.</returns>
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
