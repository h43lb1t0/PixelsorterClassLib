using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Linq;

namespace PixelsorterClassLib;

/// <summary>
/// Provides methods for sorting image data based on specified criteria.
/// </summary>
/// <remarks>The Sorter class includes functionality to sort the pixels of an image row by row, allowing for
/// custom sorting based on a provided function that extracts a comparable value from each pixel. Sorting is performed
/// in a way that maintains the original image structure, making it suitable for image processing tasks where row-wise
/// ordering is required.</remarks>
public class Sorter
{
    public Sorter() { }

    /// <summary>
    /// Sorts the pixels in an image row by row based on the provided sorting criterion.
    /// </summary>
    /// <param name="imageData">3D NumSharp array representing the image (height x width x channels)</param>
    /// <param name="sortingFunction">Function that extracts a comparable value from a pixel</param>
    /// <returns>Sorted image as a 3D NumSharp array</returns>
    public static NDArray SortImage(NDArray imageData, Func<Rgba32, float> sortingFunction)
    {
        var shape = imageData.shape;
        int height = shape[0];
        int width = shape[1];
        int channels = shape[2];

        var result = np.zeros(shape, dtype: typeof(byte));

        for (int y = 0; y < height; y++)
        {
            var pixels = new (Rgba32 pixel, float sortValue)[width];

            for (int x = 0; x < width; x++)
            {
                byte r = imageData[y, x, 0];
                byte g = imageData[y, x, 1];
                byte b = imageData[y, x, 2];
                byte a = channels > 3 ? (byte)imageData[y, x, 3] : (byte)255;

                var pixel = new Rgba32(r, g, b, a);
                float sortValue = sortingFunction(pixel);
                pixels[x] = (pixel, sortValue);
            }

            var sortedPixels = pixels.OrderBy(p => p.sortValue).ToArray();

            for (int x = 0; x < width; x++)
            {
                var pixel = sortedPixels[x].pixel;
                result[y, x, 0] = pixel.R;
                result[y, x, 1] = pixel.G;
                result[y, x, 2] = pixel.B;
                if (channels > 3)
                    result[y, x, 3] = pixel.A;
            }
        }

        return result;
    }

    
}
