using NumSharp;
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
    /// Generates the points of a line between two coordinates using Bresenham's line algorithm.
    /// </summary>
    /// <param name="x0">The x-coordinate of the first point.</param>
    /// <param name="y0">The y-coordinate of the first point.</param>
    /// <param name="x1">The x-coordinate of the second point.</param>
    /// <param name="y1">The y-coordinate of the second point.</param>
    /// <returns>An enumerable collection of points that form the line between the two coordinates.</returns>
    private static IEnumerable<(int X, int Y)> GetBresenhamLine(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2;

        while (true)
        {
            yield return (x0, y0);
            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    private static List<((int X, int Y) start, (int X, int Y) end)> GetBresenhamRays(int width, int height, float angle)
    {
        var rays = new List<((int X, int Y) start, (int X, int Y) end)>();

        // Normalize angle to 0-360 degrees
        angle = (angle % 360f + 360f) % 360f;
        double angleRad = angle * Math.PI / 180.0;

        double dx = Math.Cos(angleRad);
        double dy = Math.Sin(angleRad);

        // Snap to pure vertical/horizontal if very close, to avoid floating point overlap anomalies
        if (Math.Abs(dx) < 1e-5) dx = 0;
        if (Math.Abs(dy) < 1e-5) dy = 0;

        // Helper to find where the ray exits the image bounding box
        (int, int) GetEndPoint(int startX, int startY)
        {
            double tx = double.PositiveInfinity;
            double ty = double.PositiveInfinity;

            if (dx > 0) tx = (width - 1 - startX) / dx;
            else if (dx < 0) tx = (0 - startX) / dx;

            if (dy > 0) ty = (height - 1 - startY) / dy;
            else if (dy < 0) ty = (0 - startY) / dy;

            double t = Math.Min(tx, ty);
            int endX = (int)Math.Round(startX + t * dx);
            int endY = (int)Math.Round(startY + t * dy);

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
    public static NDArray SortImageB(NDArray imageData, Func<Hsl, float> sortingFunction, SortDirections sortDirections, NDArray? mask = null, float angle = -1f)
    {
        var shape = imageData.shape;
        int height = (int)shape[0];
        int width = (int)shape[1];
        int channels = (int)shape[2];

        // HSL requires float precision (H: 0-360, S: 0-1, L: 0-1)
        var sourceData = imageData.ToArray<float>();
        var resultData = new float[sourceData.Length];

        List<((int, int) start, (int, int) end)> rays = [];

        // Unsorted pixels keep their original values
        Array.Copy(sourceData, resultData, sourceData.Length);

        // Mask remains byte data since it evaluates thresholds (0-255)
        byte[]? maskData = null;
        int maskChannels = 4;
        if (mask is not null)
        {
            maskData = mask.ToArray<byte>();
            maskChannels = (int)mask.shape[2];
        }

        if (sortDirections == SortDirections.IntoMask)
        {
            if (maskData is null)
                throw new ArgumentException("A mask is required for IntoMask sorting.", nameof(mask));

            ApplyRadialMaskSort(sourceData, resultData, width, height, channels, maskData, maskChannels, sortingFunction);
        }
        else {
            float actualAngle = angle >= 0f ? angle : MapDirectionToAngle(sortDirections);
            rays = GetBresenhamRays(width, height, actualAngle);

            Parallel.ForEach(rays, (ray) =>
            {
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
                        resultData[pixelOffset] = pixel.H;
                        resultData[pixelOffset + 1] = pixel.S;
                        resultData[pixelOffset + 2] = pixel.L;
                        if (channels > 3) resultData[pixelOffset + 3] = pixel.A;
                    }
                    runOffsets.Clear();
                    runPixels.Clear();
                }

                foreach (var (x, y) in GetBresenhamLine(ray.start.Item1, ray.start.Item2, ray.end.Item1, ray.end.Item2))
                {
                    // Basic bounds safety check
                    if (x < 0 || x >= width || y < 0 || y >= height) continue;
                    bool insideMask = true;
                    if (maskData != null)
                    {
                        int maskIndex = (y * width + x) * maskChannels;
                        insideMask = maskData[maskIndex] >= 128;
                    }
                    if (insideMask)
                    {
                        int pixelOffset = (y * width + x) * channels;
                        float h = sourceData[pixelOffset];
                        float s = sourceData[pixelOffset + 1];
                        float l = sourceData[pixelOffset + 2];
                        float a = channels > 3 ? sourceData[pixelOffset + 3] : 1f;
                        runOffsets.Add(pixelOffset);
                        runPixels.Add(new PixelSortData(h, s, l, a, sortingFunction(new Hsl(h, s, l))));
                    }
                    else
                    {
                        FlushRun();
                    }
                }

                // Flush anything left at the end of the line
                FlushRun();
            } );
        }

        return np.array(resultData).reshape(shape);
    }


    public static NDArray SortImageA(NDArray imageData, Func<Hsl, float> sortingFunction, SortDirections sortDirections, NDArray? mask = null)
    {
        var shape = imageData.shape;
        int height = (int)shape[0];
        int width = (int)shape[1];
        int channels = (int)shape[2];

        // HSL requires float precision (H: 0-360, S: 0-1, L: 0-1)
        var sourceData = imageData.ToArray<float>();
        var resultData = new float[sourceData.Length];

        // Unsorted pixels keep their original values
        Array.Copy(sourceData, resultData, sourceData.Length);

        // Mask remains byte data since it evaluates thresholds (0-255)
        byte[]? maskData = null;
        int maskChannels = 4;
        if (mask is not null)
        {
            maskData = mask.ToArray<byte>();
            maskChannels = (int)mask.shape[2];
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
                    if (maskData != null && maskData[maskRowOffset + x * maskChannels] < 128)
                    {
                        x++;
                        continue;
                    }

                    int segStart = x;
                    while (x < width && (maskData == null || maskData[maskRowOffset + x * maskChannels] >= 128))
                        x++;
                    int segLen = x - segStart;

                    if (segLen <= 1) continue;

                    var pixelBuffer = new PixelSortData[segLen];
                    for (int i = 0; i < segLen; i++)
                    {
                        int pixelOffset = rowOffset + (segStart + i) * channels;
                        float h = sourceData[pixelOffset];
                        float s = sourceData[pixelOffset + 1];
                        float l = sourceData[pixelOffset + 2];
                        float a = channels > 3 ? sourceData[pixelOffset + 3] : 1f;

                        pixelBuffer[i] = new PixelSortData(h, s, l, a, sortingFunction(new Hsl(h, s, l)));
                    }

                    Array.Sort(pixelBuffer, 0, segLen);

                    for (int i = 0; i < segLen; i++)
                    {
                        int pixelOffset = rowOffset + (segStart + i) * channels;
                        int sourceIndex = sortDirections == SortDirections.RowRightToLeft ? segLen - 1 - i : i;
                        ref var pixel = ref pixelBuffer[sourceIndex];

                        resultData[pixelOffset] = pixel.H;
                        resultData[pixelOffset + 1] = pixel.S;
                        resultData[pixelOffset + 2] = pixel.L;
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
                    if (maskData != null && maskData[y * width * maskChannels + maskColumnOffset] < 128)
                    {
                        y++;
                        continue;
                    }

                    int segStart = y;
                    while (y < height && (maskData == null || maskData[y * width * maskChannels + maskColumnOffset] >= 128))
                        y++;
                    int segLen = y - segStart;

                    if (segLen <= 1) continue;

                    var pixelBuffer = new PixelSortData[segLen];
                    for (int i = 0; i < segLen; i++)
                    {
                        int pixelOffset = columnOffset + (segStart + i) * width * channels;
                        float h = sourceData[pixelOffset];
                        float s = sourceData[pixelOffset + 1];
                        float l = sourceData[pixelOffset + 2];
                        float a = channels > 3 ? sourceData[pixelOffset + 3] : 1f;

                        pixelBuffer[i] = new PixelSortData(h, s, l, a, sortingFunction(new Hsl(h, s, l)));
                    }

                    Array.Sort(pixelBuffer, 0, segLen);

                    for (int i = 0; i < segLen; i++)
                    {
                        int pixelOffset = columnOffset + (segStart + i) * width * channels;
                        int sourceIndex = sortDirections == SortDirections.ColumnBottomToTop ? segLen - 1 - i : i;
                        ref var pixel = ref pixelBuffer[sourceIndex];

                        resultData[pixelOffset] = pixel.H;
                        resultData[pixelOffset + 1] = pixel.S;
                        resultData[pixelOffset + 2] = pixel.L;
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
        public readonly float H;
        public readonly float S;
        public readonly float L;
        public readonly float A;
        public readonly float SortValue;

        public PixelSortData(float h, float s, float l, float a, float sortValue)
        {
            H = h;
            S = s;
            L = l;
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
    private static void ApplyRadialMaskSort(float[] sourceData, float[] resultData, int width, int height, int channels, byte[] maskData, int maskChannels, Func<Hsl, float> sortingFunction)
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
                resultData[pixelOffset] = pixel.H;
                resultData[pixelOffset + 1] = pixel.S;
                resultData[pixelOffset + 2] = pixel.L;
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
                    float h = sourceData[pixelOffset];
                    float s = sourceData[pixelOffset + 1];
                    float l = sourceData[pixelOffset + 2];
                    float a = channels > 3 ? sourceData[pixelOffset + 3] : 1f;

                    runOffsets.Add(pixelOffset);
                    runPixels.Add(new PixelSortData(h, s, l, a, sortingFunction(new Hsl(h, s, l))));
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