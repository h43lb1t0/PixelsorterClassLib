using NumSharp;
using NumSharp.Backends.Unmanaged;
using SixLabors.ImageSharp.ColorSpaces;

namespace PixelsorterClassLib.Core;

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
    /// Generates a list of rays using Bresenham's line algorithm, starting from the edges of the image and extending in the specified angle direction.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <param name="angle">The angle in degrees to extend the rays.</param>
    /// <returns>A list of rays represented as tuples of start and end points.</returns>
    private static List<((int X, int Y) start, (int X, int Y) end)> GetBresenhamRays(int width, int height, float angle)
    {
        var rays = new List<((int X, int Y) start, (int X, int Y) end)>();

        // Normalize angle to 0-360 degrees
        angle = (angle % 360f + 360f) % 360f;
        float angleRad = angle * MathF.PI / 180.0f;

        float dx = MathF.Cos(angleRad);
        float dy = MathF.Sin(angleRad);

        // Snap to pure vertical/horizontal if very close, to avoid floating point overlap anomalies
        if (MathF.Abs(dx) < 1e-5f) dx = 0f;
        if (MathF.Abs(dy) < 1e-5f) dy = 0f;

        // Helper to find where the ray exits the image bounding box
        (int, int) GetEndPoint(int startX, int startY)
        {
            float tx = float.PositiveInfinity;
            float ty = float.PositiveInfinity;

            if (dx > 0) tx = (width - 1 - startX) / dx;
            else if (dx < 0) tx = (0 - startX) / dx;

            if (dy > 0) ty = (height - 1 - startY) / dy;
            else if (dy < 0) ty = (0 - startY) / dy;

            float t = MathF.Min(tx, ty);
            int endX = (int)MathF.Round(startX + t * dx);
            int endY = (int)MathF.Round(startY + t * dy);

            return (Math.Clamp(endX, 0, width - 1), Math.Clamp(endY, 0, height - 1));
        }

        // Generate start points on the "incoming" edges based on the direction vector
        if (dx == 0) // Vertical
        {
            int startY = dy > 0 ? 0 : height - 1;
            for (int x = 0; x < width; x++) rays.Add(((x, startY), GetEndPoint(x, startY)));
        }
        else if (dy == 0) // Horizontal
        {
            int startX = dx > 0 ? 0 : width - 1;
            for (int y = 0; y < height; y++) rays.Add(((startX, y), GetEndPoint(startX, y)));
        }
        else if (dx > 0 && dy > 0) // 0 to 90 degrees (Down-Right)
        {
            for (int y = 0; y < height; y++) rays.Add(((0, y), GetEndPoint(0, y)));
            for (int x = 1; x < width; x++) rays.Add(((x, 0), GetEndPoint(x, 0)));
        }
        else if (dx < 0 && dy > 0) // 90 to 180 degrees (Down-Left)
        {
            for (int y = 0; y < height; y++) rays.Add(((width - 1, y), GetEndPoint(width - 1, y)));
            for (int x = 0; x < width - 1; x++) rays.Add(((x, 0), GetEndPoint(x, 0)));
        }
        else if (dx < 0 && dy < 0) // 180 to 270 degrees (Up-Left)
        {
            for (int y = 0; y < height; y++) rays.Add(((width - 1, y), GetEndPoint(width - 1, y)));
            for (int x = 0; x < width - 1; x++) rays.Add(((x, height - 1), GetEndPoint(x, height - 1)));
        }
        else // 270 to 360 degrees (Up-Right)
        {
            for (int y = 0; y < height; y++) rays.Add(((0, y), GetEndPoint(0, y)));
            for (int x = 1; x < width; x++) rays.Add(((x, height - 1), GetEndPoint(x, height - 1)));
        }

        return rays;
    }

    /// <summary>
    /// Maps the specified SortDirections enum value to a corresponding angle in degrees.
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static float MapDirectionToAngle(SortDirections direction)
    {
        return direction switch
        {
            SortDirections.RowLeftToRight => 0f,
            SortDirections.RowRightToLeft => 180f,
            SortDirections.ColumnTopToBottom => 90f,
            SortDirections.ColumnBottomToTop => 270f,
            _ => throw new ArgumentException("Invalid sort direction for angle mapping.", nameof(direction)),
        };
    }

    /// <summary>
    /// Sorts the pixels in an image row by row based on the provided sorting criterion.
    /// </summary>
    /// <param name="imageData">3D NumSharp array representing the image in HSL (height x width x channels)</param>
    /// <param name="sortingFunction">Function that extracts a comparable value from an HSL pixel</param>
    /// <param name="sortDirections">Direction in which to sort the pixels (e.g., left-to-right, right-to-left)</param>
    /// <param name="angle">The angle at which to sort the pixels (in degrees). This parameter is optional and defaults to -1, which indicates that the angle should be determined based on the sortDirections parameter.</param>
    /// <param name="mask">Optional 3D NumSharp array representing a binary mask to define sortable segments</param>
    /// <returns>Sorted image as a 3D NumSharp array</returns>
    public static NDArray SortImage(NDArray imageData, Func<Hsl, float> sortingFunction, SortDirections sortDirections, NDArray? mask = null, float angle = -1f)
    {
        if (sortDirections == SortDirections.ArbitraryAngle && (angle < 0f || angle > 360f))
        {
            throw new ArgumentException("Angle must be between 0 and 360 degrees for arbitrary angle sorting.", nameof(angle));
        }
        var shape = imageData.shape;
        int height = (int)shape[0];
        int width = (int)shape[1];
        int channels = (int)shape[2];
        bool hasAlpha = channels > 3;

        // HSL requires float precision (H: 0-360, S: 0-1, L: 0-1)
        // Data<T>() returns an ArraySlice<T> referencing unmanaged memory directly (zero-copy)
        var sourceData = imageData.Data<float>();
        var halfSourceData = imageData.Data<Half>();
        int totalElements = (int)sourceData.Count;
        var resultData = new Half[totalElements];

        List<((int, int) start, (int, int) end)> rays = [];

        // Unsorted pixels keep their original values — copy directly from unmanaged source
        halfSourceData.CopyTo(resultData.AsSpan());

        // Mask remains byte data since it evaluates thresholds (0-255)
        ArraySlice<byte> maskData = default;
        bool hasMask = mask is not null;
        int maskChannels = 4;
        if (hasMask)
        {
            maskData = mask!.Data<byte>();
            maskChannels = (int)mask.shape[2];
        }

        if (sortDirections == SortDirections.IntoMask)
        {
            if (!hasMask)
                throw new ArgumentException("A mask is required for IntoMask sorting.", nameof(mask));

            ApplyRadialMaskSort(sourceData, resultData, width, height, channels, maskData, maskChannels, sortingFunction);
        }
        else
        {
            float actualAngle = angle >= 0f ? angle : MapDirectionToAngle(sortDirections);
            rays = GetBresenhamRays(width, height, actualAngle);

            int maxLineLength = width + height;

            Parallel.ForEach(
                rays,
                // 1. INITIALIZATION: Runs exactly ONCE per CPU thread.
                // This uses a ValueTuple to allocate the buffers just once per thread.
                () => (
                    Offsets: new int[maxLineLength],
                    Pixels: new PixelSortData[maxLineLength]
                ),

                // 2. LOOP BODY: Runs for every ray, recycling the thread's buffers.
                (ray, loopState, threadBuffers) =>
                {
                    // Grab the recycled arrays out of our thread tuple
                    var runOffsets = threadBuffers.Offsets;
                    var runPixels = threadBuffers.Pixels;
                    int runLength = 0;

                    void FlushRun()
                    {
                        if (runLength <= 1)
                        {
                            runLength = 0;
                            return;
                        }

                        runPixels.AsSpan(0, runLength).Sort();

                        for (int i = 0; i < runLength; i++)
                        {
                            int targetOffset = runOffsets[i];
                            int srcOffset = runPixels[i].SourceOffset;

                            resultData[targetOffset] = (Half)sourceData[srcOffset];
                            resultData[targetOffset + 1] = (Half)sourceData[srcOffset + 1];
                            resultData[targetOffset + 2] = (Half)sourceData[srcOffset + 2];
                            if (hasAlpha) resultData[targetOffset + 3] = (Half)sourceData[srcOffset + 3];
                        }

                        runLength = 0;
                    }

                    int x0 = ray.start.Item1;
                    int y0 = ray.start.Item2;
                    int x1 = ray.end.Item1;
                    int y1 = ray.end.Item2;

                    int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
                    int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
                    int err = dx + dy, e2;

                    int pixelOffset = (y0 * width + x0) * channels;
                    int maskOffset = (y0 * width + x0) * maskChannels;

                    int sxPixelStep = sx * channels;
                    int syPixelStep = sy * width * channels;
                    int sxMaskStep = sx * maskChannels;
                    int syMaskStep = sy * width * maskChannels;

                    while (true)
                    {
                        bool insideMask = !hasMask || maskData[maskOffset] >= 128;

                        if (insideMask)
                        {
                            float h = sourceData[pixelOffset];
                            float s = sourceData[pixelOffset + 1];
                            float l = sourceData[pixelOffset + 2];

                            runOffsets[runLength] = pixelOffset;
                            runPixels[runLength] = new PixelSortData(pixelOffset, sortingFunction(new Hsl(h, s, l)));
                            runLength++;
                        }
                        else
                        {
                            FlushRun();
                        }

                        if (x0 == x1 && y0 == y1) break;

                        e2 = 2 * err;
                        if (e2 >= dy)
                        {
                            err += dy;
                            x0 += sx;
                            pixelOffset += sxPixelStep;
                            maskOffset += sxMaskStep;
                        }
                        if (e2 <= dx)
                        {
                            err += dx;
                            y0 += sy;
                            pixelOffset += syPixelStep;
                            maskOffset += syMaskStep;
                        }
                    }

                    FlushRun();

                    // Return the recycled buffers to be used by the next ray on this thread!
                    return threadBuffers;
                },

                // 3. TEARDOWN: Nothing to clean up
                (threadBuffers) => { }
            );
        }

        // Wrap resultData directly — no copy, NDArray references the existing array
        return new NDArray(resultData, new Shape(height, width, channels));
    }



    /// <summary>
    /// Struct to hold pixel data and sort value for efficient sorting
    /// </summary>
    private struct PixelSortData : IComparable<PixelSortData>
    {
        public int SourceOffset;
        public float SortValue;

        public PixelSortData(int sourceOffset, float sortValue)
        {
            SourceOffset = sourceOffset;
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
    private static void ApplyRadialMaskSort(ArraySlice<float> sourceData, Half[] resultData, int width, int height, int channels, ArraySlice<byte> maskData, int maskChannels, Func<Hsl, float> sortingFunction)
    {
        var (centerX, centerY) = GetMaskCentroid(maskData, width, height, maskChannels);

        int angleBuckets = Math.Max(360, Math.Max(width, height));
        var buckets = new List<(int X, int Y, float Dist)>[angleBuckets];
        float cx = centerX + 0.5f;
        float cy = centerY + 0.5f;

        for (int i = 0; i < angleBuckets; i++)
        {
            buckets[i] = new List<(int X, int Y, float Dist)>();
        }

        // Pre-calculate alpha requirement
        bool hasAlpha = channels > 3;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x + 0.5f - cx;
                float dy = y + 0.5f - cy;
                float angle = MathF.Atan2(dy, dx);
                int bucket = (int)MathF.Round(((angle + MathF.PI) / (2f * MathF.PI)) * (angleBuckets - 1));
                float dist = dx * dx + dy * dy;
                buckets[bucket].Add((x, y, dist));
            }
        }

        // OPTIMIZATION: Use pre-allocated arrays instead of List<T> to avoid GC allocations
        int maxLineLength = width + height; // Safe upper bound for any radial line
        var runOffsets = new int[maxLineLength];
        var runPixels = new PixelSortData[maxLineLength];
        int runLength = 0;

        void FlushRun()
        {
            if (runLength <= 1)
            {
                runLength = 0;
                return;
            }

            // OPTIMIZATION: Span sort for faster execution
            runPixels.AsSpan(0, runLength).Sort();

            for (int i = 0; i < runLength; i++)
            {
                int targetOffset = runOffsets[i];
                int srcOffset = runPixels[i].SourceOffset;

                // Read directly from sourceData using the sorted original offsets
                resultData[targetOffset] = (Half)sourceData[srcOffset];
                resultData[targetOffset + 1] = (Half)sourceData[srcOffset + 1];
                resultData[targetOffset + 2] = (Half)sourceData[srcOffset + 2];
                if (hasAlpha) resultData[targetOffset + 3] = (Half)sourceData[srcOffset + 3];
            }

            runLength = 0;
        }

        foreach (var bucket in buckets)
        {
            if (bucket.Count == 0) continue;

            bucket.Sort((a, b) => a.Dist.CompareTo(b.Dist));

            for (int j = bucket.Count - 1; j >= 0; j--)
            {
                var point = bucket[j];
                int maskIndex = (point.Y * width + point.X) * maskChannels;
                bool insideMask = maskData[maskIndex] >= 128;

                if (insideMask)
                {
                    int pixelOffset = (point.Y * width + point.X) * channels;
                    float h = sourceData[pixelOffset];
                    float s = sourceData[pixelOffset + 1];
                    float l = sourceData[pixelOffset + 2];

                    // Use the diet struct!
                    runOffsets[runLength] = pixelOffset;
                    runPixels[runLength] = new PixelSortData(pixelOffset, sortingFunction(new Hsl(h, s, l)));
                    runLength++;
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
    private static (int X, int Y) GetMaskCentroid(ArraySlice<byte> maskData, int width, int height, int maskChannels)
    {
        long sumX = 0;
        long sumY = 0;
        long count = 0;

        int idx = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (maskData[idx] >= 128)
                {
                    sumX += x;
                    sumY += y;
                    count++;
                }
                idx += maskChannels;
            }
        }

        if (count == 0)
        {
            return (width / 2, height / 2);
        }

        return ((int)(sumX / count), (int)(sumY / count));
    }
}