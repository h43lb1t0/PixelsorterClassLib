# PixelsorterClassLib

PixelsorterClassLib is a .NET class library designed for creating advanced pixel sorting effects. It provides extensive control over the sorting criteria and directions, and it even features on-device machine learning capabilities to automatically generate masks for selective sorting.

## Features

- **Custom Pixel Sorting:** Sort image pixels based on features like Hue, Brightness, Saturation, Lightness, Warmth, and Coolness.
- **Sorting Directions:** Order pixels by rows (left-to-right, right-to-left) or columns (top-to-bottom, bottom-to-top), or direct the sort into a mask.
- **Machine Learning Masking:** Generate binary image masks locally using ONNX models to isolate specific elements (like backgrounds or subjects) for selective sorting. 
- **ImageSharp-Native Pipeline:** Uses strongly typed `Image<Rgba32>` and `Image<L8>` objects end-to-end to avoid array serialization overhead.

## Basic Usage

Below is a basic example demonstrating how to load an image, auto-generate a mask, sort the pixels by their warmth, and save the result.

```csharp
using PixelsorterClassLib;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;

string inputPath = "input.jpg";
string outputPath = "output.jpg";

// 1. Load the image as RGBA pixels
using ImageSharpImage<Rgba32> imageData = PixelsorterClassLib.Image.LoadImage(inputPath);

// 2. Generate a binary mask using ML inference
using ImageSharpImage<L8> mask = new Mask().GetMask(inputPath, 50).mask;

// 3. Sort image by warmth, left to right, within the boundaries of the mask
using ImageSharpImage<Rgba32> sortedData = Sorter.SortImage(imageData, SortBy.Warmth(), SortDirections.RowLeftToRight, mask);

// 4. Save the sorted image to disk
PixelsorterClassLib.Image.SaveImage(sortedData, outputPath);
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

- **Image:** Handles loading and saving strongly typed ImageSharp image objects.
- **Sorter:** Contains the logic to traverse pixels sequentially based on a given direction and sort segment data using `Image<Rgba32>` input/output.
- **Mask:** Communicates with local Hugging Face ONNX models to produce `Image<L8>` masks for region-specific sorting without requiring manual rotoscoping.
- **SortBy:** Supplies multiple algorithmic lens evaluations for analyzing a given pixel's characteristics.

## Breaking Changes

The public API now uses strongly typed ImageSharp images instead of `NumSharp.NDArray`.

- `Image.LoadImage(string path)`
  - Before: returned `NDArray`
  - Now: returns `Image<Rgba32>`

- `Image.SaveImage(...)`
  - Before: `SaveImage(NDArray data, string path)`
  - Now: `SaveImage<TPixel>(Image<TPixel> data, string path)`

- `Sorter.SortImage(...)`
  - Before: `SortImage(NDArray imageData, Func<Rgba32,float> sortingFunction, SortDirections sortDirections, NDArray? mask = null)`
  - Now: `SortImage(Image<Rgba32> imageData, Func<Rgba32,float> sortingFunction, SortDirections sortDirections, Image<L8>? mask = null)`

- `Mask.GetMask(...)`
  - Before: returned `(NDArray mask, NDArray invertMask)`
  - Now: returns `(Image<L8> mask, Image<L8> invertMask)`

- `Mask.GetMaskAsync(...)`
  - Before: `Task<(NDArray mask, NDArray invertMask)>`
  - Now: `Task<(Image<L8> mask, Image<L8> invertMask)>`
