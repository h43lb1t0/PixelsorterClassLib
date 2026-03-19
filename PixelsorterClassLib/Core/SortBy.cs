using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;

namespace PixelsorterClassLib.Core;

/// <summary>
/// Defines static methods that return a lambda function to sort a 2D array of pixel data based on a specific criterion (e.g., brightness, hue, saturation).
/// </summary>
public class SortBy
{
    /// <summary>
    /// Returns a comparison function that sorts pixels by their hue value.
    /// </summary>
    public static Func<Hsl, float> Hue()
    {
        return pixel => pixel.H;
    }

    /// <summary>
    /// Returns a comparison function that sorts pixels by their brightness (luminance).
    /// </summary>
    public static Func<Hsl, float> Brightness()
    {
        return pixel =>
        {
            var rgbPixel = ColorSpaceConverter.ToRgb(pixel);
            return ((0.2126f * rgbPixel.R) + (0.7152f * rgbPixel.G) + (0.0722f * rgbPixel.B));
        };
    }

    /// <summary>
    /// Returns a comparison function that sorts pixels by their saturation value.
    /// </summary>
    public static Func<Hsl, float> Saturation()
    {
        return pixel => pixel.S;
    }

    public static Func<Hsl, float> Lightness()
    {
        return pixel => pixel.L;
    }


    private static Func<Hsl, float> TempHelper(float targetHue)
    {
        return pixel =>
        {
            float hueDiff = Math.Abs(pixel.H - targetHue);
            float shortestHueDist = Math.Min(hueDiff, 360f - hueDiff);

            float hueFactor = 1f - (shortestHueDist / 180f);

            float lightnessWeight = 1f - 2f * Math.Abs(pixel.L - 0.5f);

            return Math.Max(0f, hueFactor * pixel.S * lightnessWeight);
        };
    }

    public static Func<Hsl, float> Warmth()
    {
        return TempHelper(30f);
    }

    public static Func<Hsl, float> Coolness()
    {
        return TempHelper(210f);
    }

    /// <summary>
    /// A method that dynamcly return all the available sorting criteria as a dictionary of name and function pairs.
    /// </summary>
    public static Dictionary<string, Func<Hsl, float>> GetAllSortingCriteria()
    {
        var result = new Dictionary<string, Func<Hsl, float>>();
        var type = typeof(SortBy);

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(Func<Hsl, float>)
                     && m.GetParameters().Length == 0
                     && m.Name != nameof(GetAllSortingCriteria));

        foreach (var method in methods)
        {
            var func = (Func<Hsl, float>)method.Invoke(null, null);
            result[method.Name] = func;
        }

        return result;
    }
}
