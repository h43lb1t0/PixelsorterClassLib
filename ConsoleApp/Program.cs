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

        var img = PixelsorterClassLib.Core.Image.LoadImage(inputImagePath);


        var masker = new ChunkMask();


        (var j, var k) = masker.GetMask(inputImagePath, new ChunkMaskOptions(1500, 3500, SortDirections.RowLeftToRight));

        var foo = Sorter.SortImage(img, SortBy.Warmth(), SortDirections.RowLeftToRight, j);
        var voo = Sorter.SortImage(img, SortBy.Warmth(), SortDirections.RowLeftToRight, k);



        PixelsorterClassLib.Core.Image.SaveImage(foo, $"{outputDirectory}_j.jpg");
        PixelsorterClassLib.Core.Image.SaveImage(voo, $"{outputDirectory}_k.jpg");


    }
}