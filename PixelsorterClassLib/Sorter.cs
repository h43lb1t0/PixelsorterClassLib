using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PixelsorterClassLib;

/// <summary>
/// Provides methods for sorting image data based on specified criteria.
/// </summary>
public class Sorter
{

    public Sorter() { }

    /// <summary>
    /// Sorts the pixels in an image row by row based on the provided sorting criterion.
    /// </summary>
    /// <param name="imageData">Source image to sort.</param>
    /// <param name="sortingFunction">Function that extracts a comparable value from a pixel</param>
    /// <param name="sortDirections">Direction in which to sort the pixels (e.g., left-to-right, right-to-left)</param>
    /// <param name="mask">Optional binary mask to define sortable segments (same dimensions as <paramref name="imageData"/>).</param>
    /// <returns>Sorted image.</returns>
    public static Image<Rgba32> SortImage(Image<Rgba32> imageData, Func<Rgba32, float> sortingFunction, SortDirections sortDirections, Image<L8>? mask = null)
    {
        ArgumentNullException.ThrowIfNull(imageData);

        int height = imageData.Height;
        int width = imageData.Width;

        if (mask is not null && (mask.Width != width || mask.Height != height))
        {
            throw new ArgumentException("Mask dimensions must match image dimensions.", nameof(mask));
        }

        var sourceData = ExtractImageData(imageData);
        var resultData = new byte[sourceData.Length];

        // Unsorted pixels keep their original values
        Array.Copy(sourceData, resultData, sourceData.Length);

        byte[]? maskData = null;
        if (mask is not null)
        {
            maskData = ExtractMaskData(mask);
        }

        if (sortDirections == SortDirections.IntoMask)
        {
            if (maskData is null)
                throw new ArgumentException("A mask is required for IntoMask sorting.", nameof(mask));

            ApplyRadialMaskSort(sourceData, resultData, width, height, maskData, sortingFunction);
        }
        else if (sortDirections == SortDirections.RowRightToLeft || sortDirections == SortDirections.RowLeftToRight)
        {
            Parallel.For(0, height, y =>
            {
                int rowOffset = y * width * 4;
                int maskRowOffset = y * width;
                int x = 0;

                while (x < width)
                {
                    // Skip pixels that are outside the mask (black = 0 = foreground subject = leave in place)
                    if (maskData != null && maskData[maskRowOffset + x] < 128)
                    {
                        x++;
                        continue;
                    }

                    // Collect a contiguous run of mask-active pixels as one sortable chunk
                    int segStart = x;
                    while (x < width && (maskData == null || maskData[maskRowOffset + x] >= 128))
                        x++;
                    int segLen = x - segStart;

                    if (segLen <= 1) continue;

                    var pixelBuffer = new PixelSortData[segLen];
                    for (int i = 0; i < segLen; i++)
                    {
                        int pixelOffset = rowOffset + (segStart + i) * 4;
                        byte r = sourceData[pixelOffset];
                        byte g = sourceData[pixelOffset + 1];
                        byte b = sourceData[pixelOffset + 2];
                        byte a = sourceData[pixelOffset + 3];
                        pixelBuffer[i] = new PixelSortData(r, g, b, a, sortingFunction(new Rgba32(r, g, b, a)));
                    }

                    Array.Sort(pixelBuffer, 0, segLen);

                    for (int i = 0; i < segLen; i++)
                    {
                        int pixelOffset = rowOffset + (segStart + i) * 4;
                        int sourceIndex = sortDirections == SortDirections.RowRightToLeft ? segLen - 1 - i : i;
                        ref var pixel = ref pixelBuffer[sourceIndex];
                        resultData[pixelOffset] = pixel.R;
                        resultData[pixelOffset + 1] = pixel.G;
                        resultData[pixelOffset + 2] = pixel.B;
                        resultData[pixelOffset + 3] = pixel.A;
                    }
                }
            });
        }
        else
        {
            Parallel.For(0, width, x =>
            {
                int columnOffset = x * 4;
                int y = 0;

                while (y < height)
                {
                    // Skip pixels that are outside the mask
                    if (maskData != null && maskData[y * width + x] < 128)
                    {
                        y++;
                        continue;
                    }

                    // Collect a contiguous run of mask-active pixels as one sortable chunk
                    int segStart = y;
                    while (y < height && (maskData == null || maskData[y * width + x] >= 128))
                        y++;
                    int segLen = y - segStart;

                    if (segLen <= 1) continue;

                    var pixelBuffer = new PixelSortData[segLen];
                    for (int i = 0; i < segLen; i++)
                    {
                        int pixelOffset = columnOffset + (segStart + i) * width * 4;
                        byte r = sourceData[pixelOffset];
                        byte g = sourceData[pixelOffset + 1];
                        byte b = sourceData[pixelOffset + 2];
                        byte a = sourceData[pixelOffset + 3];
                        pixelBuffer[i] = new PixelSortData(r, g, b, a, sortingFunction(new Rgba32(r, g, b, a)));
                    }

                    Array.Sort(pixelBuffer, 0, segLen);

                    for (int i = 0; i < segLen; i++)
                    {
                        int pixelOffset = columnOffset + (segStart + i) * width * 4;
                        int sourceIndex = sortDirections == SortDirections.ColumnBottomToTop ? segLen - 1 - i : i;
                        ref var pixel = ref pixelBuffer[sourceIndex];
                        resultData[pixelOffset] = pixel.R;
                        resultData[pixelOffset + 1] = pixel.G;
                        resultData[pixelOffset + 2] = pixel.B;
                        resultData[pixelOffset + 3] = pixel.A;
                    }
                }
            });
        }

        return BuildImage(resultData, width, height);
    }

    private static byte[] ExtractImageData(Image<Rgba32> image)
    {
        var data = new byte[image.Width * image.Height * 4];
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                int rowOffset = y * image.Width * 4;
                for (int x = 0; x < image.Width; x++)
                {
                    int pixelOffset = rowOffset + (x * 4);
                    var pixel = row[x];
                    data[pixelOffset] = pixel.R;
                    data[pixelOffset + 1] = pixel.G;
                    data[pixelOffset + 2] = pixel.B;
                    data[pixelOffset + 3] = pixel.A;
                }
            }
        });
        return data;
    }

    private static byte[] ExtractMaskData(Image<L8> mask)
    {
        var data = new byte[mask.Width * mask.Height];
        mask.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < mask.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                int rowOffset = y * mask.Width;
                for (int x = 0; x < mask.Width; x++)
                {
                    data[rowOffset + x] = row[x].PackedValue;
                }
            }
        });
        return data;
    }

    private static Image<Rgba32> BuildImage(byte[] data, int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                int rowOffset = y * width * 4;
                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = rowOffset + (x * 4);
                    row[x] = new Rgba32(
                        data[pixelOffset],
                        data[pixelOffset + 1],
                        data[pixelOffset + 2],
                        data[pixelOffset + 3]);
                }
            }
        });

        return image;
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
    /// <param name="maskData">Flattened mask byte array.</param>
    /// <param name="sortingFunction">Function producing the sortable value from a pixel.</param>
    private static void ApplyRadialMaskSort(byte[] sourceData, byte[] resultData, int width, int height, byte[] maskData, Func<Rgba32, float> sortingFunction)
    {
        var (centerX, centerY) = GetMaskCentroid(maskData, width, height);

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
                resultData[pixelOffset + 3] = pixel.A;
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
                int maskIndex = point.Y * width + point.X;
                bool insideMask = maskData[maskIndex] >= 128;

                if (insideMask)
                {
                    int pixelOffset = (point.Y * width + point.X) * 4;
                    byte r = sourceData[pixelOffset];
                    byte g = sourceData[pixelOffset + 1];
                    byte b = sourceData[pixelOffset + 2];
                    byte a = sourceData[pixelOffset + 3];
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
    /// <returns>Centroid coordinates (x, y) within image bounds.</returns>
    private static (int X, int Y) GetMaskCentroid(byte[] maskData, int width, int height)
    {
        long sumX = 0;
        long sumY = 0;
        long count = 0;

        for (int y = 0; y < height; y++)
        {
            int maskRowOffset = y * width;
            for (int x = 0; x < width; x++)
            {
                if (maskData[maskRowOffset + x] >= 128)
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
