using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorProfiles;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace PixelsorterClassLib.Core;

/// <summary>
/// Provides methods for loading and saving images in various formats using a 3D NumSharp array representation.
/// </summary>
/// <remarks>The Image class enables conversion between image files and NumSharp NDArray objects, facilitating
/// image processing workflows that require manipulation of pixel data in array form. All images are handled in a
/// consistent channel order (RGBA) to ensure compatibility across different formats.</remarks>
public class Image
{

    /// <summary>
    /// Loads an image from the specified file path and returns it as a 3D NumSharp array (height x width x channels) in HSL color space. 
    /// The method should handle various image formats and convert them to a consistent format (e.g., RGBA) for processing.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static NDArray LoadImage(string path)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(path);
        image.Mutate(x => x.AutoOrient());

        int height = image.Height;
        int width = image.Width;

        float[] data = new float[height * width * 3];
        int index = 0;

        var converter = new ColorProfileConverter();
        var rgbRow = new Rgb[width];
        var hslRow = new Hsl[width];

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                Span<Rgb24> row = accessor.GetRowSpan(y);

                for (int x = 0; x < row.Length; x++)
                {
                    Rgb24 p = row[x];
                    rgbRow[x] = new Rgb(p.R / 255f, p.G / 255f, p.B / 255f);
                }

                // Convert the whole row in one call
                converter.Convert<Rgb, Hsl>(rgbRow, hslRow);

                for (int x = 0; x < hslRow.Length; x++)
                {
                    data[index++] = hslRow[x].H; // Hue 0-360
                    data[index++] = hslRow[x].S; // Saturation 0-1
                    data[index++] = hslRow[x].L; // Lightness 0-1
                }
            }
        });

        return np.array(data).reshape(new Shape(height, width, 3));
    }


    public static Image<Rgba32> NdarrayToImgData(NDArray data)
    {
        var shape = data.shape;
        int height = (int)shape[0];
        int width = (int)shape[1];
        int channels = (int)shape[2];

        if (channels != 1 && channels < 3)
            throw new InvalidOperationException($"Unsupported channel count: {channels}");

        var sourceData = data.ToArray<float>();

        var image = new Image<Rgba32>(width, height);

        var converter = new ColorProfileConverter();
        var hslRow = new Hsl[width];
        var rgbRow = new Rgb[width];

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                Span<Rgba32> rowSpan = accessor.GetRowSpan(y);
                int rowOffset = y * width * channels;

                if (channels >= 3)
                {
                    // Gather the row's HSL values, then convert them in one call
                    for (int x = 0; x < width; x++)
                    {
                        int o = rowOffset + x * channels;
                        hslRow[x] = new Hsl(sourceData[o], sourceData[o + 1], sourceData[o + 2]);
                    }

                    converter.Convert<Hsl, Rgb>(hslRow, rgbRow);

                    for (int x = 0; x < width; x++)
                    {
                        byte r = (byte)Math.Clamp(rgbRow[x].R * 255f, 0, 255);
                        byte g = (byte)Math.Clamp(rgbRow[x].G * 255f, 0, 255);
                        byte b = (byte)Math.Clamp(rgbRow[x].B * 255f, 0, 255);
                        byte a = 255;

                        if (channels > 3)
                        {
                            int o = rowOffset + x * channels;
                            a = (byte)Math.Clamp(sourceData[o + 3] * 255f, 0, 255);
                        }

                        rowSpan[x] = new Rgba32(r, g, b, a);
                    }
                }
                else // channels == 1
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte v = (byte)Math.Clamp(sourceData[rowOffset + x] * 255f, 0, 255);
                        rowSpan[x] = new Rgba32(v, v, v, 255);
                    }
                }
            }
        });

        return image;
    }

    /// <summary>
    /// Saves a 3D NumSharp array (height x width x channels) as an image file at the specified path. 
    /// The method should handle the conversion from the NumSharp array format back to an image format and support various output formats based on the file extension provided in the path.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="path"></param>
    public static void SaveImage(NDArray data, string path)
    {


        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var image = NdarrayToImgData(data);

        using var outputStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        image.SaveAsPng(outputStream);
        outputStream.Flush(true);
    }
}

