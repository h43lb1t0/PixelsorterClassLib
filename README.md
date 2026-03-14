# PixelsorterClassLib

PixelsorterClassLib is a .NET class library designed for creating advanced pixel sorting effects. It provides extensive control over the sorting criteria and directions, and it even features on-device machine learning capabilities to automatically generate masks for selective sorting.

## Features

- **Custom Pixel Sorting:** Sort image pixels based on features like Hue, Brightness, Saturation, Lightness, Warmth, and Coolness.
- **Sorting Directions:** Order pixels by rows (left-to-right, right-to-left) or columns (top-to-bottom, bottom-to-top), or direct the sort into a mask.
- **Machine Learning Masking:** Generate binary image masks locally using ONNX models to isolate specific elements (like backgrounds or subjects) for selective sorting. 
- **NumSharp & ImageSharp Integration:** Interacts seamlessly with pixel data as multidimensional arrays and uses ImageSharp for modern image format parsing.

## Basic Usage

Below is a basic example demonstrating how to load an image, auto-generate a mask, sort the pixels by their warmth, and save the result.

```csharp
using NumSharp;
using PixelsorterClassLib.Core;
using PixelsorterClassLib.Masks;

string inputPath = "input.jpg";
string outputPath = "output.jpg";

// 1. Load the image into an NDArray
NDArray imageData = Image.LoadImage(inputPath);

// 2. Generate a binary mask using ML inference
NDArray mask = new Mask().GetMask(inputPath, 50);

// 3. Sort image by warmth, left to right, within the boundaries of the mask
NDArray sortedData = Sorter.SortImage(imageData, SortBy.Warmth(), SortDirections.RowLeftToRight, mask);

// 4. Save the sorted image to disk
Image.SaveImage(sortedData, outputPath);
```

## Available Sorting Criteria

You can easily supply your own callback function `Func<Rgba32, float>` or use one of the predefined sorting algorithms in the `SortBy` static class:

- `SortBy.Hue()`
- `SortBy.Brightness()`
- `SortBy.Saturation()`
- `SortBy.Lightness()`
- `SortBy.Warmth()`
- `SortBy.Coolness()`

You can also fetch all available sorting metrics programmatically using:
```csharp
var criteria = SortBy.GetAllSortingCriteria();
```

## Core Components

- **Image:** Handles reading and writing standard image formats directly to and from 3D `NumSharp` arrays.
- **Sorter:** Contains the logic to traverse pixels sequentially based on a given direction and sort segment data.
- **SortBy:** Supplies multiple algorithmic lens evaluations for analyzing a given pixel's characteristics.
- **SortDirections:** Defines the various directions in which pixels can be sorted (e.g., left-to-right, top-to-bottom).

## Masks Components

- **BackgroundMask:** Generates a binary mask isolating the background of an image.
- **CannyMask:** Creates a binary mask based on edge detection using a Canny like algorithm.
- **MaskCombiner:** Combines multiple binary masks into a single mask.
