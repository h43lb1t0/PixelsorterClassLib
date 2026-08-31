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

        String inputImagePath = "E:\\Bilder\\pixel_input\\before_3.jpg";
        String outputDirectory = "D:\\Documents\\codeing\\PixelsorterProject\\PixelsorterClassLib\\ConsoleApp\\examples\\";

        var img = PixelsorterClassLib.Core.Image.LoadImage(inputImagePath);


        var masker = new BackgroundMask();


        (var j, var k) = masker.GetMask(inputImagePath, new BackgroundMaskOptions(1));

        var watchB = System.Diagnostics.Stopwatch.StartNew();

        var foo = Sorter.SortImageB(img, SortBy.Warmth(), SortDirections.RowLeftToRight, j);
        watchB.Stop();
        var watchA = System.Diagnostics.Stopwatch.StartNew();
        var voo = Sorter.SortImageA(img, SortBy.Warmth(), SortDirections.RowLeftToRight, j);
        watchA.Stop();

        Console.WriteLine($"Time taken for SortImageB: {watchB.ElapsedMilliseconds} ms");
        Console.WriteLine($"Time taken for SortImageA: {watchA.ElapsedMilliseconds} ms");



        PixelsorterClassLib.Core.Image.SaveImage(foo, $"{outputDirectory}_B.jpg");
        PixelsorterClassLib.Core.Image.SaveImage(voo, $"{outputDirectory}_A.jpg");


    }
}