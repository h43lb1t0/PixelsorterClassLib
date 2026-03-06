using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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

        if (sortDirections == SortDirections.IntoMask)
        {
            if (maskData is null)
                throw new ArgumentException("A mask is required for IntoMask sorting.", nameof(mask));

            ApplyRadialMaskSort(sourceData, resultData, width, height, channels, maskData, maskChannels, sortingFunction);
        }
        else if (sortDirections == SortDirections.RowRightToLeft || sortDirections == SortDirections.RowLeftToRight)
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

    /// <summary>
    /// Sorts pixels within the masked region along radial lines pointing toward the mask centroid.
    /// </summary>
    /// <param name="sourceData">Flattened source image byte array.</param>
    /// <param name="resultData">Flattened destination byte array to write sorted pixels into.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="channels">Channel count of the image (e.g., 4 for RGBA).</param>
    /// <param name="maskData">Flattened mask byte array.</param>
    /// <param name="maskChannels">Channel count of the mask (1 or 4).</param>
    /// <param name="sortingFunction">Function producing the sortable value from a pixel.</param>
    private static void ApplyRadialMaskSort(byte[] sourceData, byte[] resultData, int width, int height, int channels, byte[] maskData, int maskChannels, Func<Rgba32, float> sortingFunction)
    {
        var (centerX, centerY) = GetMaskCentroid(maskData, width, height, maskChannels);

        int angleBuckets = Math.Max(360, Math.Max(width, height));
        var buckets = new List<(int X, int Y, double Dist)>[angleBuckets];
        double cx = centerX + 0.5;
        double cy = centerY + 0.5;

        for (int i = 0; i < angleBuckets; i++)
        {
            buckets[i] = new List<(int X, int Y, double Dist)>();
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double dx = x + 0.5 - cx;
                double dy = y + 0.5 - cy;
                double angle = Math.Atan2(dy, dx);
                int bucket = (int)Math.Round(((angle + Math.PI) / (2 * Math.PI)) * (angleBuckets - 1));
                double dist = dx * dx + dy * dy;
                buckets[bucket].Add((x, y, dist));
            }
        }

        var runOffsets = new List<int>();
        var runPixels = new List<PixelSortData>();

        void FlushRun()
        {
            if (runOffsets.Count <= 1)
            {
                runOffsets.Clear();
                runPixels.Clear();
                return;
            }

            var buffer = runPixels.ToArray();
            Array.Sort(buffer, 0, buffer.Length);

            for (int i = 0; i < runOffsets.Count; i++)
            {
                ref var pixel = ref buffer[i];
                int pixelOffset = runOffsets[i];
                resultData[pixelOffset] = pixel.R;
                resultData[pixelOffset + 1] = pixel.G;
                resultData[pixelOffset + 2] = pixel.B;
                if (channels > 3) resultData[pixelOffset + 3] = pixel.A;
            }

            runOffsets.Clear();
            runPixels.Clear();
        }

        foreach (var bucket in buckets)
        {
            if (bucket.Count == 0) continue;

            bucket.Sort((a, b) => a.Dist.CompareTo(b.Dist));

            var sequence = bucket.AsEnumerable().Reverse();

            foreach (var point in sequence)
            {
                int maskIndex = (point.Y * width + point.X) * maskChannels;
                bool insideMask = maskData[maskIndex] >= 128;

                if (insideMask)
                {
                    int pixelOffset = (point.Y * width + point.X) * channels;
                    byte r = sourceData[pixelOffset];
                    byte g = sourceData[pixelOffset + 1];
                    byte b = sourceData[pixelOffset + 2];
                    byte a = channels > 3 ? sourceData[pixelOffset + 3] : (byte)255;
                    runOffsets.Add(pixelOffset);
                    runPixels.Add(new PixelSortData(r, g, b, a, sortingFunction(new Rgba32(r, g, b, a))));
                }
                else
                {
                    FlushRun();
                }
            }

            FlushRun();
        }
    }

    /// <summary>
    /// Computes the centroid of the masked area, falling back to the image center if the mask is empty.
    /// </summary>
    /// <param name="maskData">Flattened mask byte array.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="maskChannels">Channel count of the mask (1 or 4).</param>
    /// <returns>Centroid coordinates (x, y) within image bounds.</returns>
    private static (int X, int Y) GetMaskCentroid(byte[] maskData, int width, int height, int maskChannels)
    {
        long sumX = 0;
        long sumY = 0;
        long count = 0;

        for (int y = 0; y < height; y++)
        {
            int maskRowOffset = y * width * maskChannels;
            for (int x = 0; x < width; x++)
            {
                if (maskData[maskRowOffset + x * maskChannels] >= 128)
                {
                    sumX += x;
                    sumY += y;
                    count++;
                }
            }
        }

        if (count == 0)
        {
            return (width / 2, height / 2);
        }

        return ((int)(sumX / count), (int)(sumY / count));
    }
}
