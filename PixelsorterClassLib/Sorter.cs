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
    /// <param name="sortDirections">Direction in which to sort the pixels (e.g., left-to-right, right-to-left)</param>
    /// <param name="mask">Optional 3D NumSharp array representing a binary mask to define sortable segments (same height and width as imageData, with 1 or 4 channels)</param>
    /// <returns>Sorted image as a 3D NumSharp array</returns>
    public static NDArray SortImage(NDArray imageData, Func<Rgba32, float> sortingFunction, SortDirections sortDirections, NDArray? mask = null)
    {
        var shape = imageData.shape;
        int height = shape[0];
        int width = shape[1];
        int channels = shape[2];

        var sourceData = imageData.ToArray<byte>();
        var resultData = new byte[sourceData.Length];

        // Unsorted pixels keep their original values
        Array.Copy(sourceData, resultData, sourceData.Length);

        // Load mask if an image path was provided.
        // White pixels (>= 128) in the mask are the segments to be sorted;
        // black pixels (< 128) act as break points and are left in place.
        // The faded edges produced by FadeEdges are already baked into the
        // binary mask via Bayer dithering, so no extra handling is needed.
        byte[]? maskData = null;
        int maskChannels = 4;
        if (mask is not null)
        {
            maskData = mask.ToArray<byte>();
            maskChannels = mask.shape[2];
        }

        if (sortDirections == SortDirections.RowRightToLeft || sortDirections == SortDirections.RowLeftToRight)
        {
            Parallel.For(0, height, y =>
            {
                int rowOffset = y * width * channels;
                int maskRowOffset = y * width * maskChannels;
                int x = 0;

                while (x < width)
                {
                    // Skip pixels that are outside the mask (black = 0 = foreground subject = leave in place)
                    if (maskData != null && maskData[maskRowOffset + x * maskChannels] < 128)
                    {
                        x++;
                        continue;
                    }

                    // Collect a contiguous run of mask-active pixels as one sortable chunk
                    int segStart = x;
                    while (x < width && (maskData == null || maskData[maskRowOffset + x * maskChannels] >= 128))
                        x++;
                    int segLen = x - segStart;

                    if (segLen <= 1) continue;

                    var pixelBuffer = new PixelSortData[segLen];
                    for (int i = 0; i < segLen; i++)
                    {
                        int pixelOffset = rowOffset + (segStart + i) * channels;
                        byte r = sourceData[pixelOffset];
                        byte g = sourceData[pixelOffset + 1];
                        byte b = sourceData[pixelOffset + 2];
                        byte a = channels > 3 ? sourceData[pixelOffset + 3] : (byte)255;
                        pixelBuffer[i] = new PixelSortData(r, g, b, a, sortingFunction(new Rgba32(r, g, b, a)));
                    }

                    Array.Sort(pixelBuffer, 0, segLen);

                    for (int i = 0; i < segLen; i++)
                    {
                        int pixelOffset = rowOffset + (segStart + i) * channels;
                        int sourceIndex = sortDirections == SortDirections.RowRightToLeft ? segLen - 1 - i : i;
                        ref var pixel = ref pixelBuffer[sourceIndex];
                        resultData[pixelOffset] = pixel.R;
                        resultData[pixelOffset + 1] = pixel.G;
                        resultData[pixelOffset + 2] = pixel.B;
                        if (channels > 3) resultData[pixelOffset + 3] = pixel.A;
                    }
                }
            });
        }
        else
        {
            Parallel.For(0, width, x =>
            {
                int columnOffset = x * channels;
                int maskColumnOffset = x * maskChannels;
                int y = 0;

                while (y < height)
                {
                    // Skip pixels that are outside the mask
                    if (maskData != null && maskData[y * width * maskChannels + maskColumnOffset] < 128)
                    {
                        y++;
                        continue;
                    }

                    // Collect a contiguous run of mask-active pixels as one sortable chunk
                    int segStart = y;
                    while (y < height && (maskData == null || maskData[y * width * maskChannels + maskColumnOffset] >= 128))
                        y++;
                    int segLen = y - segStart;

                    if (segLen <= 1) continue;

                    var pixelBuffer = new PixelSortData[segLen];
                    for (int i = 0; i < segLen; i++)
                    {
                        int pixelOffset = columnOffset + (segStart + i) * width * channels;
                        byte r = sourceData[pixelOffset];
                        byte g = sourceData[pixelOffset + 1];
                        byte b = sourceData[pixelOffset + 2];
                        byte a = channels > 3 ? sourceData[pixelOffset + 3] : (byte)255;
                        pixelBuffer[i] = new PixelSortData(r, g, b, a, sortingFunction(new Rgba32(r, g, b, a)));
                    }

                    Array.Sort(pixelBuffer, 0, segLen);

                    for (int i = 0; i < segLen; i++)
                    {
                        int pixelOffset = columnOffset + (segStart + i) * width * channels;
                        int sourceIndex = sortDirections == SortDirections.ColumnBottomToTop ? segLen - 1 - i : i;
                        ref var pixel = ref pixelBuffer[sourceIndex];
                        resultData[pixelOffset] = pixel.R;
                        resultData[pixelOffset + 1] = pixel.G;
                        resultData[pixelOffset + 2] = pixel.B;
                        if (channels > 3) resultData[pixelOffset + 3] = pixel.A;
                    }
                }
            });
        }

        return np.array(resultData).reshape(shape);
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
