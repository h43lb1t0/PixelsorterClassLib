using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Linq;
using System.Threading.Tasks;

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
    public static NDArray SortImage(NDArray imageData, Func<Rgba32, float> sortingFunction, SortDirections sortDirections)
    {
        var shape = imageData.shape;
        int height = shape[0];
        int width = shape[1];
        int channels = shape[2];

        // Get direct access to the underlying data as byte array
        var sourceData = imageData.ToArray<byte>();
        var resultData = new byte[sourceData.Length];

        if (sortDirections == SortDirections.RowRightToLeft || sortDirections == SortDirections.RowLeftToRight)
        {


            // Process rows in parallel for significant performance boost
            Parallel.For(0, height, y =>
            {
                // Each thread gets its own buffer to avoid race conditions
                var pixelBuffer = new PixelSortData[width];
                int rowOffset = y * width * channels;

                // Extract pixels from the row
                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = rowOffset + x * channels;

                    byte r = sourceData[pixelOffset];
                    byte g = sourceData[pixelOffset + 1];
                    byte b = sourceData[pixelOffset + 2];
                    byte a = channels > 3 ? sourceData[pixelOffset + 3] : (byte)255;

                    var pixel = new Rgba32(r, g, b, a);
                    float sortValue = sortingFunction(pixel);

                    pixelBuffer[x] = new PixelSortData(r, g, b, a, sortValue);
                }

                // Sort using Array.Sort which is faster than LINQ OrderBy
                Array.Sort(pixelBuffer, 0, width);

                // Write sorted pixels back to result
                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = rowOffset + x * channels;

                    // If right-to-left, reverse the order by reading from the end
                    int sourceIndex = sortDirections == SortDirections.RowRightToLeft ? width - 1 - x : x;
                    ref var pixel = ref pixelBuffer[sourceIndex];

                    resultData[pixelOffset] = pixel.R;
                    resultData[pixelOffset + 1] = pixel.G;
                    resultData[pixelOffset + 2] = pixel.B;
                    if (channels > 3)
                        resultData[pixelOffset + 3] = pixel.A;
                }
            });
        }

        // Create NDArray from the result data
        var result = np.array(resultData).reshape(shape);
        return result;
    }

    /// <summary>
    /// Struct to hold pixel data and sort value for efficient sorting
    /// </summary>
    private readonly struct PixelSortData : IComparable<PixelSortData>
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;
        public readonly float SortValue;

        public PixelSortData(byte r, byte g, byte b, byte a, float sortValue)
        {
            R = r;
            G = g;
            B = b;
            A = a;
            SortValue = sortValue;
        }

        public int CompareTo(PixelSortData other)
        {
            return SortValue.CompareTo(other.SortValue);
        }
    }

    
}
