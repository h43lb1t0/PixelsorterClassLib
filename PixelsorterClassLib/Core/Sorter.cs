using NumSharp;
using SixLabors.ImageSharp.ColorSpaces;
using System.Collections.Concurrent;

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
    private static readonly ConcurrentDictionary<(int Width, int Height, float Angle), ((int, int) start, (int, int) end)[]> _rayCache = new();

    public Sorter() { }

    /// <summary>
    /// Generates a list of rays using Bresenham's line algorithm, starting from the edges of the image and extending in the specified angle direction.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <param name="angle">The angle in degrees to extend the rays.</param>
    /// <returns>A list of rays represented as tuples of start and end points.</returns>
    private static ((int, int) start, (int, int) end)[] GetBresenhamRays(int width, int height, float angle)
    {
        var key = (width, height, angle);
        if (_rayCache.TryGetValue(key, out var cached))
            return cached;

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

        var result = rays.ToArray();
        _rayCache.TryAdd(key, result);
        return result;
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
    /// <param name="mask">Optional 3D NumSharp array representing a binary mask to define sortable segments</param>
    /// <param name="angle">The angle at which to sort the pixels (in degrees). This parameter is optional and defaults to -1, which indicates that the angle should be determined based on the sortDirections parameter. Will be ignored if sortDirections is not SortDirections.ArbitraryAngle.</param>
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

        // Data<T>() returns an ArraySlice<T> referencing unmanaged memory directly (zero-copy)
        var sourceData = imageData.Data<float>();

        // Allocate result directly in unmanaged memory — avoids both the managed float[]
        // allocation and the copy that NDArray(float[], Shape) would do internally
        var resultShape = new Shape(height, width, channels);
        var resultNdArray = new NDArray(typeof(float), resultShape);
        var resultData = resultNdArray.Data<float>();

        // Extract raw addresses — all hot-path access goes through float*/byte* pointers
        // instead of ArraySlice<T> indexers (critical for Mono/Android performance where
        // the indexer overhead is 5-10x worse than on CoreCLR)
        nint srcAddr;
        nint dstAddr;
        unsafe
        {
            srcAddr = (nint)sourceData.Address;
            dstAddr = (nint)resultData.Address;

            // Bulk copy via raw pointers — unsorted pixels keep their original values
            long byteCount = (long)sourceData.Count * sizeof(float);
            Buffer.MemoryCopy((void*)srcAddr, (void*)dstAddr, byteCount, byteCount);
        }


        // Mask setup — extract address for pointer access in hot loops
        nint maskAddr = 0;
        bool hasMask = mask is not null;
        int maskChannels = 4;
        if (hasMask)
        {
            var maskSlice = mask!.Data<byte>();
            unsafe { maskAddr = (nint)maskSlice.Address; }
            maskChannels = (int)mask.shape[2];
        }

        if (sortDirections == SortDirections.IntoMask)
        {
            if (!hasMask)
                throw new ArgumentException("A mask is required for IntoMask sorting.", nameof(mask));

            ApplyRadialMaskSort(srcAddr, dstAddr, width, height, channels, hasAlpha, maskAddr, maskChannels, sortingFunction);
        }
        else
        {
            float actualAngle = angle >= 0f ? angle : MapDirectionToAngle(sortDirections);
            ((int, int) start, (int, int) end)[] rays = GetBresenhamRays(width, height, actualAngle);

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
                    var runOffsets = threadBuffers.Offsets;
                    var runPixels = threadBuffers.Pixels;
                    int runLength = 0;

                    unsafe
                    {
                        // Raw pointer access bypasses ArraySlice indexer overhead
                        // (bounds checks + indirection per access) — critical for ARM/Mono
                        float* srcPtr = (float*)srcAddr;
                        float* dstPtr = (float*)dstAddr;
                        byte* maskPtr = (byte*)maskAddr;

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
                            bool insideMask = !hasMask || maskPtr[maskOffset] >= 128;

                            if (insideMask)
                            {
                                float h = srcPtr[pixelOffset];
                                float s = srcPtr[pixelOffset + 1];
                                float l = srcPtr[pixelOffset + 2];

                                runOffsets[runLength] = pixelOffset;
                                runPixels[runLength] = new PixelSortData(pixelOffset, sortingFunction(new Hsl(h, s, l)));
                                runLength++;
                            }
                            else
                            {
                                FlushSortRun(srcPtr, dstPtr, runOffsets, runPixels, ref runLength, hasAlpha);
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

                        FlushSortRun(srcPtr, dstPtr, runOffsets, runPixels, ref runLength, hasAlpha);
                    }

                    return threadBuffers;
                },

                // 3. TEARDOWN: Nothing to clean up
                (threadBuffers) => { }
            );
        }

        // Result NDArray was allocated directly in unmanaged memory — just return it
        return resultNdArray;
    }



    /// <summary>
    /// Struct to hold pixel data and sort value for efficient sorting
    /// </summary>
    private readonly struct PixelSortData : IComparable<PixelSortData>
    {
        public readonly int SourceOffset;
        public readonly float SortValue;

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
    /// Writes sorted pixel data back to the destination buffer using raw pointers.
    /// Shared by both the ray-based and radial mask sorting paths.
    /// </summary>
    private static unsafe void FlushSortRun(
        float* srcPtr, float* dstPtr,
        int[] runOffsets, PixelSortData[] runPixels,
        ref int runLength, bool hasAlpha)
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

            dstPtr[targetOffset] = srcPtr[srcOffset];
            dstPtr[targetOffset + 1] = srcPtr[srcOffset + 1];
            dstPtr[targetOffset + 2] = srcPtr[srcOffset + 2];
            if (hasAlpha)
                dstPtr[targetOffset + 3] = srcPtr[srcOffset + 3];
        }

        runLength = 0;
    }

    /// <summary>
    /// Sorts pixels within the masked region along radial lines pointing toward the mask centroid.
    /// </summary>
    private static void ApplyRadialMaskSort(nint srcAddr, nint dstAddr, int width, int height, int channels, bool hasAlpha, nint maskAddr, int maskChannels, Func<Hsl, float> sortingFunction)
    {
        var (centerX, centerY) = GetMaskCentroid(maskAddr, width, height, maskChannels);

        int angleBuckets = Math.Max(360, Math.Max(width, height));
        var buckets = new List<(int X, int Y, float Dist)>[angleBuckets];
        float cx = centerX + 0.5f;
        float cy = centerY + 0.5f;

        for (int i = 0; i < angleBuckets; i++)
        {
            buckets[i] = new List<(int X, int Y, float Dist)>();
        }

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

        // Pre-allocated arrays for run sorting
        int maxLineLength = width + height;
        var runOffsets = new int[maxLineLength];
        var runPixels = new PixelSortData[maxLineLength];
        int runLength = 0;

        unsafe
        {
            float* srcPtr = (float*)srcAddr;
            float* dstPtr = (float*)dstAddr;
            byte* maskPtr = (byte*)maskAddr;

            foreach (var bucket in buckets)
            {
                if (bucket.Count == 0) continue;

                bucket.Sort((a, b) => a.Dist.CompareTo(b.Dist));

                for (int j = bucket.Count - 1; j >= 0; j--)
                {
                    var point = bucket[j];
                    int maskIndex = (point.Y * width + point.X) * maskChannels;
                    bool insideMask = maskPtr[maskIndex] >= 128;

                    if (insideMask)
                    {
                        int pixelOffset = (point.Y * width + point.X) * channels;
                        float h = srcPtr[pixelOffset];
                        float s = srcPtr[pixelOffset + 1];
                        float l = srcPtr[pixelOffset + 2];

                        runOffsets[runLength] = pixelOffset;
                        runPixels[runLength] = new PixelSortData(pixelOffset, sortingFunction(new Hsl(h, s, l)));
                        runLength++;
                    }
                    else
                    {
                        FlushSortRun(srcPtr, dstPtr, runOffsets, runPixels, ref runLength, hasAlpha);
                    }
                }

                FlushSortRun(srcPtr, dstPtr, runOffsets, runPixels, ref runLength, hasAlpha);
            }
        }
    }

    /// <summary>
    /// Computes the centroid of the masked area, falling back to the image center if the mask is empty.
    /// </summary>
    private static unsafe (int X, int Y) GetMaskCentroid(nint maskAddr, int width, int height, int maskChannels)
    {
        long sumX = 0;
        long sumY = 0;
        long count = 0;

        byte* maskPtr = (byte*)maskAddr;
        int idx = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (maskPtr[idx] >= 128)
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