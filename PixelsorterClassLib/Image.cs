using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PixelsorterClassLib;

/// <summary>
/// Provides methods for loading and saving images in various formats.
/// </summary>
public class Image
{

    /// <summary>
    /// Loads an image from the specified file path as an RGBA image.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static SixLabors.ImageSharp.Image<Rgba32> LoadImage(string path)
    {
        return SixLabors.ImageSharp.Image.Load<Rgba32>(path);
    }


    /// <summary>
    /// Saves an image file at the specified path.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="path"></param>
    public static void SaveImage<TPixel>(SixLabors.ImageSharp.Image<TPixel> data, string path)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        ArgumentNullException.ThrowIfNull(data);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var outputStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        data.SaveAsPng(outputStream);
        outputStream.Flush(true);
    }
}

