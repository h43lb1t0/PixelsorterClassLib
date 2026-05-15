using NumSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.ComponentModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using PixelsorterClassLib.Masks;
using PixelsorterClassLib.Core;

/// <summary>
/// Provides the entry point for the application, orchestrating the process of loading an image, applying multiple
/// sorting criteria, and saving the resulting images.
/// </summary>
/// <remarks>The application loads an image from a predefined file path, retrieves a set of sorting criteria, and
/// applies each criterion to sort the image data. Each sorted image is saved with a filename that reflects the sorting
/// criterion used. Ensure that the input image file exists and is accessible before running the application. This class
/// is intended for internal use as the main entry point and is not designed for direct instantiation or use by external
/// code.</remarks>
internal class Program
{
    private static void Main(string[] args)
    {
        String inputImagePath = "D:\\Documents\\codeing\\PixelsorterProject\\PixelsorterClassLib\\ConsoleApp\\examples\\alone-4480442.jpg";
        String outputDirectory = "D:\\Documents\\codeing\\PixelsorterProject\\PixelsorterClassLib\\ConsoleApp\\examples\\";

        var sortBy = SortBy.Saturation();
        var direction = SortDirections.RowRightToLeft;

        var warmup = PixelsorterClassLib.Core.Image.LoadImage(inputImagePath, KnownResamplers.NearestNeighbor);
        PixelsorterClassLib.Core.Sorter.SortImage(warmup.Item1, sortBy, direction);

        var downscaleWatch = System.Diagnostics.Stopwatch.StartNew();
        var (downscaledImage, downscaledSize) = PixelsorterClassLib.Core.Image.LoadImage(inputImagePath, KnownResamplers.NearestNeighbor);
        var downscaledSorted = PixelsorterClassLib.Core.Sorter.SortImage(downscaledImage, sortBy, direction);
        PixelsorterClassLib.Core.Image.SaveImage(downscaledSorted, $"{outputDirectory}downscaled_upscaled_sorted.png", KnownResamplers.NearestNeighbor, downscaledSize);

        downscaleWatch.Stop();

        PixelsorterClassLib.Core.Image.SaveImage(downscaledSorted, $"{outputDirectory}downscaled_sorted.png");

        var fullResWatch = System.Diagnostics.Stopwatch.StartNew();
        var (fullResImage, fullResSize) = PixelsorterClassLib.Core.Image.LoadImage(inputImagePath);
        var fullResSorted = PixelsorterClassLib.Core.Sorter.SortImage(fullResImage, sortBy, direction);
        PixelsorterClassLib.Core.Image.SaveImage(fullResSorted, $"{outputDirectory}full_res_sorted.png");
        fullResWatch.Stop();

        Console.WriteLine($"Full resolution sort: {fullResWatch.ElapsedMilliseconds} ms (loaded {fullResSize.width}x{fullResSize.height})");
        Console.WriteLine($"Downscaled and upscaled after sorting sort: {downscaleWatch.ElapsedMilliseconds} ms");
    }
}