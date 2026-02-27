using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PixelsorterClassLib;

/// <summary>
/// Defines static methods that return a lambda function to sort a 2D array of pixel data based on a specific criterion (e.g., brightness, hue, saturation).
/// </summary>
public class SortBy
{
    /// <summary>
    /// Returns a comparison function that sorts pixels by their hue value.
    /// </summary>
    public static Func<Rgba32, float> Hue()
    {
        return pixel =>
        {
            float r = pixel.R / 255f;
            float g = pixel.G / 255f;
            float b = pixel.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            if (delta == 0)
                return 0f;

            float hue;
            if (max == r)
                hue = ((g - b) / delta) % 6;
            else if (max == g)
                hue = (b - r) / delta + 2;
            else
                hue = (r - g) / delta + 4;

            hue *= 60;
            if (hue < 0)
                hue += 360;

            return hue;
        };
    }

    /// <summary>
    /// Returns a comparison function that sorts pixels by their brightness (luminance).
    /// </summary>
    public static Func<Rgba32, float> Brightness()
    {
        return pixel => 0.299f * pixel.R + 0.587f * pixel.G + 0.114f * pixel.B;
    }

    /// <summary>
    /// Returns a comparison function that sorts pixels by their saturation value.
    /// </summary>
    public static Func<Rgba32, float> Saturation()
    {
        return pixel =>
        {
            float r = pixel.R / 255f;
            float g = pixel.G / 255f;
            float b = pixel.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            if (max == 0)
                return 0f;

            return delta / max;
        };
    }

    public static Func<Rgba32, float> Lightness()
    {
        return pixel =>
        {
            float r = pixel.R / 255f;
            float g = pixel.G / 255f;
            float b = pixel.B / 255f;
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            return (max + min) / 2f;
        };
    }

    public static Func<Rgba32, float> Warmth()
    {
        return pixel =>
        {
            float r = pixel.R / 255f;
            float g = pixel.G / 255f;
            float b = pixel.B / 255f;
            // Simple warmth calculation based on the red and blue channels
            return r - b;
        };
    }

    public static Func<Rgba32, float> Coolness()
    {
        return pixel =>
        {
            float r = pixel.R / 255f;
            float g = pixel.G / 255f;
            float b = pixel.B / 255f;
            // Simple coolness calculation based on the blue and red channels
            return b - r;
        };
    }

    /// <summary>
    /// A method that dynamcly return all the available sorting criteria as a dictionary of name and function pairs.
    /// </summary>
    public static Dictionary<string, Func<Rgba32, float>> GetAllSortingCriteria()
    {
        var result = new Dictionary<string, Func<Rgba32, float>>();
        var type = typeof(SortBy);

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(Func<Rgba32, float>)
                     && m.GetParameters().Length == 0
                     && m.Name != nameof(GetAllSortingCriteria));

        foreach (var method in methods)
        {
            var func = (Func<Rgba32, float>)method.Invoke(null, null);
            result[method.Name] = func;
        }

        return result;
    }
}
