using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;

namespace PixelsorterClassLib.Core;

/// <summary>
/// Defines static methods that return a lambda function to sort a 2D array of pixel (in HSL color space) data based on a specific criterion (e.g., brightness, hue, saturation).
/// </summary>
public class SortBy
{

    /// <summary>
    /// Creates a function that retrieves the hue component from an HSL color value.
    /// </summary>
    /// <returns>A function that, when given an HSL color, returns its hue component as a floating-point value.</returns>
    public static Func<Hsl, float> Hue()
    {
        return pixel => pixel.H;
    }

    /// <summary>
    /// Returns a function that retrieves the saturation component from an HSL color value.
    /// </summary>
    /// <returns>A function that, when given an HSL color, returns its saturation component as a floating-point value.</returns>
    public static Func<Hsl, float> Saturation()
    {
        return pixel => pixel.S;
    }

    /// <summary>
    /// Creates a function that retrieves the lightness component from an HSL color value.
    /// </summary>
    /// <returns>A function that, when given an HSL color, returns its lightness component as a floating-point value.</returns>
    public static Func<Hsl, float> Lightness()
    {
        return pixel => pixel.L;
    }


    /// <summary>
    /// Creates a function that evaluates how closely an HSL color matches a specified target hue, factoring in
    /// saturation and lightness.
    /// </summary>
    /// <remarks>The returned function gives higher scores to colors with hues near the target, high
    /// saturation, and mid-range lightness. This can be used for color filtering or ranking based on hue
    /// similarity.</remarks>
    /// <param name="targetHue">The target hue, in degrees, to compare against. Must be in the range 0 to 360.</param>
    /// <returns>A function that takes an HSL color and returns a score between 0 and 1 indicating the similarity to the target
    /// hue, with higher values representing a closer match.</returns>
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

    /// <summary>
    /// Creates a function that calculates the warmth value of a given HSL color.
    /// </summary>
    /// <returns>A function that takes an HSL color and returns a float representing its warmth, where higher values indicate
    /// warmer colors.</returns>
    public static Func<Hsl, float> Warmth()
    {
        return TempHelper(30f);
    }

    /// <summary>
    /// Returns a function that calculates the coolness value for a given HSL color based on a fixed hue reference.
    /// </summary>
    /// <returns>A function that takes an <see cref="Hsl"/> color and returns a <see langword="float"/> representing its coolness
    /// relative to a hue of 210 degrees.</returns>
    public static Func<Hsl, float> Coolness()
    {
        return TempHelper(210f);
    }


    /// <summary>
    /// Creates a function that calculates the vibrancy of a color represented in HSL (Hue, Saturation, Lightness) color
    /// space.
    /// </summary>
    /// <remarks>Vibrancy is computed based on the color's saturation and its distance from the midpoint of
    /// lightness. The returned value is higher for colors that are both saturated and not too close to pure black or
    /// white. This function can be used to assess the perceived vividness of a color.</remarks>
    /// <returns>A function that takes an HSL color and returns its vibrancy as a floating-point value between 0 and 1.</returns>
    public static Func<Hsl, float> Vibrancy()
    {
        return pixel =>
        {
            return pixel.S * (1f - 2f * Math.Abs(pixel.L - 0.5f));
        };
    }

    /// <summary>
    /// Creates a function that calculates the chroma value of an HSL color, adjusted by a hue-dependent weighting
    /// factor.
    /// </summary>
    /// <remarks>The chroma calculation is based on the HSL color model and incorporates a cosine-based
    /// adjustment to account for perceptual differences in chroma across hues.</remarks>
    /// <returns>A function that takes an <see cref="Hsl"/> color and returns its chroma as a <see langword="float"/>, weighted
    /// by the hue. The returned value represents the color's chroma intensity, where higher values indicate more vivid
    /// colors.</returns>
    public static Func<Hsl, float> Chroma()
    {
        return pixel =>
        {
            float chroma = (1f - Math.Abs(2f * pixel.L - 1f)) * pixel.S;

            float hueWeight = 0.675f + 0.325f * (float)Math.Cos(((pixel.H - 60f) / 360f) * 2.0 * Math.PI);

            return chroma * hueWeight;
        };
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
