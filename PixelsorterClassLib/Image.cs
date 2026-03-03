using System;
using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;

namespace PixelsorterClassLib;

/// <summary>
/// Provides methods for loading and saving images in various formats using a 3D NumSharp array representation.
/// </summary>
/// <remarks>The Image class enables conversion between image files and NumSharp NDArray objects, facilitating
/// image processing workflows that require manipulation of pixel data in array form. All images are handled in a
/// consistent channel order (RGBA) to ensure compatibility across different formats.</remarks>
public class Image
{

	/// <summary>
	/// Loads an image from the specified file path and returns it as a 3D NumSharp array (height x width x channels). 
	/// The method should handle various image formats and convert them to a consistent format (e.g., RGBA) for processing.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public static NDArray LoadImage(string path)
	{
		using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
		int height = image.Height;
		int width = image.Width;

		// Allocate flat byte array for direct access
		var data = new byte[height * width * 4];

		image.ProcessPixelRows(accessor =>
		{
			for (int y = 0; y < height; y++)
			{
				var rowSpan = accessor.GetRowSpan(y);
				int rowOffset = y * width * 4;

				for (int x = 0; x < width; x++)
				{
					var pixel = rowSpan[x];
					int pixelOffset = rowOffset + x * 4;

					data[pixelOffset] = pixel.R;
					data[pixelOffset + 1] = pixel.G;
					data[pixelOffset + 2] = pixel.B;
					data[pixelOffset + 3] = pixel.A;
				}
			}
		});

		// Create NDArray from byte array and reshape to 3D
		return np.array(data).reshape(new Shape(height, width, 4));
	}


	public static Image<Rgba32> NdarrayToImgData(NDArray data)
	{
        var shape = data.shape;
        int height = shape[0];
        int width = shape[1];
        int channels = shape[2];

        var sourceData = data.ToArray<byte>();

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
                        r = sourceData[pixelOffset];
                        g = sourceData[pixelOffset + 1];
                        b = sourceData[pixelOffset + 2];
                        if (channels > 3)
                        {
                            a = sourceData[pixelOffset + 3];
                        }
                    }
                    else if (channels == 1)
                    {
                        byte gray = sourceData[pixelOffset];
                        r = g = b = gray;
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

