using NumSharp;
using PixelsorterClassLib;
using SixLabors.ImageSharp.PixelFormats;
using System.ComponentModel;

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
        string inputPath = "ConsoleApp/examples/alone-4480442.jpg";
        var imageData = Image.LoadImage(inputPath);

        NDArray mask = new Mask().GetMask(inputPath, 200);
        Image.SaveImage(mask, "ConsoleApp/examples/mask.png");

        var sortedData = Sorter.SortImage(imageData, SortBy.Warmth(), SortDirections.RowLeftToRight, mask);
        string outputPath = $"ConsoleApp/examples/output.jpg";
        Image.SaveImage(sortedData, outputPath);

        sortedData = Sorter.SortImage(imageData, SortBy.Saturation(), SortDirections.RowLeftToRight);
        outputPath = $"ConsoleApp/examples/output2.jpg";
        Image.SaveImage(sortedData, outputPath);


        /*
        var imageData = Image.LoadImage(inputPath);

        Dictionary<string, Func<Rgba32, float>> criteria = SortBy.GetAllSortingCriteria();

        foreach (var criterion in criteria)
        {
            Console.WriteLine($"Sorting by {criterion.Key}...");


            foreach (var direction in Enum.GetValues(typeof(SortDirections)).Cast<SortDirections>())
            {
                Console.WriteLine($"  Sorting direction: {direction}...");
                var sortedData = Sorter.SortImage(imageData, criterion.Value, direction);
                string outputPath = $"ConsoleApp/examples/output_{criterion.Key.ToLower()}_{direction}.jpg";
                Image.SaveImage(sortedData, outputPath);
            }


        }
        */

    }
}