using System;
using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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

		var array = np.zeros(new Shape(height, width, 4), NPTypeCode.Byte);

		image.ProcessPixelRows(accessor =>
		{
			for (int y = 0; y < height; y++)
			{
				var rowSpan = accessor.GetRowSpan(y);
				for (int x = 0; x < width; x++)
				{
					var pixel = rowSpan[x];
					array[y, x, 0] = pixel.R;
					array[y, x, 1] = pixel.G;
					array[y, x, 2] = pixel.B;
					array[y, x, 3] = pixel.A;
				}
			}
		});

		return array;
	}

	/// <summary>
	/// Saves a 3D NumSharp array (height x width x channels) as an image file at the specified path. 
	/// The method should handle the conversion from the NumSharp array format back to an image format and support various output formats based on the file extension provided in the path.
	/// </summary>
	/// <param name="data"></param>
	/// <param name="path"></param>
	public static void SaveImage(NDArray data, string path)
	{
		var shape = data.shape;
		int height = shape[0];
		int width = shape[1];

		using var image = new SixLabors.ImageSharp.Image<Rgba32>(width, height);

		image.ProcessPixelRows(accessor =>
		{
			for (int y = 0; y < height; y++)
			{
				var rowSpan = accessor.GetRowSpan(y);
				for (int x = 0; x < width; x++)
				{
					byte r = data[y, x, 0];
					byte g = data[y, x, 1];
					byte b = data[y, x, 2];
					byte a = shape.Length > 2 && shape[2] > 3 ? (byte)data[y, x, 3] : (byte)255;

					rowSpan[x] = new Rgba32(r, g, b, a);
				}
			}
		});

		image.Save(path);
	}
}

