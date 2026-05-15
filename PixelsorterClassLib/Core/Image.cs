using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System.Runtime.CompilerServices;
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

    const int MaxLongSide = 1920;
    const int MaxShortSide = 1080;


    /// <summary>
    /// Loads an image from the specified file path and returns it as a 3D NumSharp array (height x width x channels) in HSL color space. 
    /// The method should handle various image formats and convert them to a consistent format (e.g., RGBA) for processing.
    /// </summary>
    /// <param name="path">The file path of the image to load.</param>
    /// <param name="resampler">Optional resampler used to resize the image during loading if it is larger then 1080p.</param>
    /// <returns>A 3D NumSharp array representing the image in HSL color space and a tuple containing the image's width and height.</returns>
    public static (NDArray, (int width, int height)) LoadImage(string path, IResampler? resampler = null)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(path);
        image.Mutate(x => x.AutoOrient());
        int height = image.Height;
        int width = image.Width;

        if (resampler != null && (Math.Max(width, height) > MaxLongSide || Math.Min(width, height) > MaxShortSide))
        {
            bool isLandscape = width >= height;
            Size targetSize = isLandscape
                ? new Size(MaxLongSide, MaxShortSide)
                : new Size(MaxShortSide, MaxLongSide);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = targetSize,
                Sampler = resampler,
                Compand = true
            }));
        }

        // Allocate flat byte array for direct access
        float[] data = new float[height * width * 3];

        int index = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                Span<Rgb24> row = accessor.GetRowSpan(y);

                for (int x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    var rgb = new Rgb(pixel.R / 255f, pixel.G / 255f, pixel.B / 255f);
                    var hsl = ColorSpaceConverter.ToHsl(rgb);

                    data[index++] = hsl.H; // Hue 0-360
                    data[index++] = hsl.S; // Saturation 0-1
                    data[index++] = hsl.L; // Lightness 0-1
                }
            }
        });

        // Create NDArray from byte array and reshape to 3D
        return (np.array(data).reshape(new Shape(height, width, 3)), (width, height));
    }


    public static Image<Rgba32> NdarrayToImgData(NDArray data)
    {
        var shape = data.shape;
        int height = shape[0];
        int width = shape[1];
        int channels = shape[2];

        var sourceData = data.ToArray<float>();

        var image = new SixLabors.ImageSharp.Image<Rgba32>(width, height);

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var rowSpan = accessor.GetRowSpan(y);
                int rowOffset = y * width * channels;

                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = rowOffset + x * channels;

                    byte r;
                    byte g;
                    byte b;
                    byte a = 255;

                    if (channels >= 3)
                    {
                        float h = sourceData[pixelOffset];
                        float s = sourceData[pixelOffset + 1];
                        float l = sourceData[pixelOffset + 2];

                        var rgb = ColorSpaceConverter.ToRgb(new Hsl(h, s, l));
                        r = (byte)Math.Clamp(rgb.R * 255f, 0, 255);
                        g = (byte)Math.Clamp(rgb.G * 255f, 0, 255);
                        b = (byte)Math.Clamp(rgb.B * 255f, 0, 255);

                        if (channels > 3)
                        {
                            a = (byte)Math.Clamp(sourceData[pixelOffset + 3] * 255f, 0, 255);
                        }
                    }
                    else if (channels == 1)
                    {
                        float gray = sourceData[pixelOffset];
                        r = g = b = (byte)Math.Clamp(gray * 255f, 0, 255);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported channel count: {channels}");
                    }

                    rowSpan[x] = new Rgba32(r, g, b, a);
                }
            }
        });

        return image;
    }

    /// <summary>
    /// Saves a 3D NumSharp array (height x width x channels) as an image file at the specified path. 
    /// The method should handle the conversion from the NumSharp array format back to an image format and support various output formats based on the file extension provided in the path.
    /// </summary>
    /// <param name="data">The NumSharp array containing image data.</param>
    /// <param name="path">The file path where the image will be saved.</param>
    /// <param name="resampler">The resampler to use for resizing the image.</param>
    /// <param name="size">The target size for resizing the image.</param>
    public static void SaveImage(NDArray data, string path, IResampler? resampler, (int width, int height)? size)
    {


        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var image = NdarrayToImgData(data);

            if (resampler != null && size.HasValue)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(size.Value.width, size.Value.height),
                    Sampler = resampler,
                    Compand = true
                }));
            } else if (resampler == null ^ size.HasValue)
            {
                throw new ArgumentException("Both resampler and size must be provided together or both must be null.");
            }

        using var outputStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        image.SaveAsPng(outputStream);
        outputStream.Flush(true);
    }
}

