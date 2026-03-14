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
        Mask mask;

        String inputImagePath = "D:\\Documents\\codeing\\PixelsorterProject\\PixelsorterClassLib\\ConsoleApp\\examples\\alone-4480442.jpg";
        String outputDirectory = "D:\\Documents\\codeing\\PixelsorterProject\\PixelsorterClassLib\\ConsoleApp\\examples\\";

        for (int i = 0; i < 2; i++) {

            if (i == 1)
            {
                mask = new CannyMask();
            }
            else
            {
                mask = new BackgroundMask();
            }

            if (!mask.IsReadyToUse)
            {
                Console.WriteLine($"Mask of type {mask.GetType().Name} is not ready to use. Attempting to download model...");
                if (!mask.DownloadModel().Result)
                {
                    Console.WriteLine($"Failed to download model for mask of type {mask.GetType().Name}. Skipping this mask.");
                    continue;
                }
            }

            var (maskArray, invertedMaskArray) = mask.GetMask(inputImagePath, 30);

            //PixelsorterClassLib.core.Image.SaveImage(maskArray, $"{outputDirectory}mask_{mask.GetType().Name}.png");
            PixelsorterClassLib.Core.Image.SaveImage(invertedMaskArray, $"{outputDirectory}inverted_mask_{mask.GetType().Name}.png");

            var img = PixelsorterClassLib.Core.Image.LoadImage(inputImagePath);
            var sortedImg = PixelsorterClassLib.Core.Sorter.SortImage(img, SortBy.Brightness(), SortDirections.ColumnBottomToTop, invertedMaskArray);

            PixelsorterClassLib.Core.Image.SaveImage(sortedImg, $"{outputDirectory}sorted_{mask.GetType().Name}.png");
        }
    }
}