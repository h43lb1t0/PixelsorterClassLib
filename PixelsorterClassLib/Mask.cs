using Microsoft.ML.OnnxRuntime;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;
using HuggingfaceHub;
using NumSharp;


namespace PixelsorterClassLib
{
 
    /// <summary>
    /// Provides functionality to generate a mask from an input image using a pre-trained model.
    /// </summary>
    /// <remarks>This class internally manages the loading of the model and the processing of images to create
    /// masks. It requires an input image path and can apply a fade effect to the edges of the generated mask. The model
    /// input size is fixed at 1024x1024 pixels, and the class handles image normalization and tensor extraction for
    /// model inference.</remarks>
    public class Mask
    {
        private static InferenceSession? _session;
        private static string? _inputName;
        private static string? _outputName;
        private static readonly object _sessionLock = new();
        private const int ModelInputSize = 1024;

        /// <summary>
        /// Loads the ONNX model from a remote repository and initializes the inference session for model execution.
        /// </summary>
        /// <remarks>This method downloads the model file asynchronously and sets up the input and output
        /// names required for inference. Ensure that network connectivity is available and the repository is accessible
        /// before calling this method. The method must be called before performing inference operations with the
        /// session.</remarks>
        private void LoadModel()
        {
            if (_session != null) return;

            lock (_sessionLock)
            {
                if (_session != null) return;

                var modelPath = HFDownloader.DownloadFileAsync(
                    repoId: "briaai/RMBG-1.4",
                    filename: "onnx/model_quantized.onnx"
                ).GetAwaiter().GetResult();

                _session = new InferenceSession(modelPath);
                _inputName = _session.InputMetadata.Keys.First();
                _outputName = _session.OutputMetadata.Keys.First();
            }
        }

        /// <summary>
        /// Extracts and normalizes pixel values from the specified bitmap for use as model input.
        /// </summary>
        /// <remarks>This method assumes the input bitmap is not null and has the required dimensions.
        /// Pixel values are normalized using a mean of 0.5 and a standard deviation of 1.0 for each channel, matching
        /// the expected input format for certain models.</remarks>
        /// <param name="bitmap">The SKBitmap instance containing the image data to be processed. The bitmap must have dimensions equal to
        /// ModelInputSize by ModelInputSize.</param>
        /// <returns>A DenseTensor<float> containing the normalized RGB pixel values, structured as [1, 3, ModelInputSize,
        /// ModelInputSize]. Each channel value is normalized to the range [-0.5, 0.5].</returns>
        private DenseTensor<float> ExtractPixels(SKBitmap bitmap)
        {
            var tensor = new DenseTensor<float>(new[] { 1, 3, ModelInputSize, ModelInputSize });
            var pixmap = bitmap.PeekPixels() ?? throw new InvalidOperationException("Unable to access bitmap pixels.");

            var pixels = pixmap.GetPixelSpan<SKColor>();
            for (int y = 0; y < ModelInputSize; y++)
            {
                int rowOffset = y * ModelInputSize;
                for (int x = 0; x < ModelInputSize; x++)
                {
                    var color = pixels[rowOffset + x];
                    tensor[0, 0, y, x] = color.Red / 255f - 0.5f;
                    tensor[0, 1, y, x] = color.Green / 255f - 0.5f;
                    tensor[0, 2, y, x] = color.Blue / 255f - 0.5f;
                }
            }
            return tensor;
        }

     
        /// <summary>
        /// Determines the minimum and maximum values contained within the specified mask tensor.
        /// </summary>
        /// <remarks>This method iterates through all elements in the tensor to identify the extreme
        /// values. If the tensor is empty, the result will not be meaningful.</remarks>
        /// <param name="maskTensor">A tensor of floating-point values from which to calculate the minimum and maximum values. The tensor must
        /// not be empty.</param>
        /// <returns>A tuple containing the minimum and maximum float values found in the mask tensor.</returns>
        private (float min, float max) GetMaskRange(Tensor<float> maskTensor)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (var value in maskTensor)
            {
                if (value < min) min = value;
                if (value > max) max = value;
            }
            return (min, max);
        }

        /// <summary>
        /// Normalizes the specified value to a range between 0 and 1 based on the provided minimum and maximum bounds.
        /// </summary>
        /// <remarks>If maxValue is less than or equal to minValue, the method clamps the input value
        /// directly to the range [0, 1] without normalization.</remarks>
        /// <param name="value">The value to normalize. If outside the range defined by minValue and maxValue, it will be clamped to the
        /// nearest bound.</param>
        /// <param name="minValue">The minimum value of the normalization range. Serves as the lower bound for the normalized result.</param>
        /// <param name="maxValue">The maximum value of the normalization range. Must be greater than minValue for proper normalization;
        /// otherwise, value is clamped directly to [0, 1].</param>
        /// <returns>A floating-point value representing the normalized input, constrained to the range [0, 1].</returns>
        private float NormalizeMaskValue(float value, float minValue, float maxValue)
        {
            if (maxValue > minValue)
            {
                return Math.Clamp((value - minValue) / (maxValue - minValue), 0f, 1f);
            }
            return Math.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Retrieves the mask value from the specified tensor at the given coordinates.
        /// </summary>
        /// <param name="maskTensor">The tensor containing mask values. Must have a rank of 2, 3, or 4.</param>
        /// <param name="y">The row index of the mask value to retrieve. Must be within the bounds of the tensor's dimensions.</param>
        /// <param name="x">The column index of the mask value to retrieve. Must be within the bounds of the tensor's dimensions.</param>
        /// <returns>The mask value at the specified coordinates as a floating-point number.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the rank of the mask tensor is not 2, 3, or 4.</exception>
        private float GetMaskValue(Tensor<float> maskTensor, int y, int x)
        {
            var rank = maskTensor.Rank;
            return rank switch
            {
                4 => maskTensor[0, 0, y, x],
                3 => maskTensor[0, y, x],
                2 => maskTensor[y, x],
                _ => throw new InvalidOperationException($"Unsupported mask tensor rank: {rank}.")
            };
        }

        /// <summary>
        /// Calculates the bilinear interpolated mask value from the specified tensor based on normalized coordinates.
        /// </summary>
        /// <param name="maskTensor">The tensor containing mask values to be interpolated.</param>
        /// <param name="maskHeight">The height of the mask tensor, representing the number of rows in the tensor.</param>
        /// <param name="maskWidth">The width of the mask tensor, representing the number of columns in the tensor.</param>
        /// <param name="normalizedY">The normalized vertical coordinate, ranging from 0 to 1, used to determine the interpolation position along
        /// the height.</param>
        /// <param name="normalizedX">The normalized horizontal coordinate, ranging from 0 to 1, used to determine the interpolation position
        /// along the width.</param>
        /// <param name="minValue">The minimum value for normalizing the mask values.</param>
        /// <param name="maxValue">The maximum value for normalizing the mask values.</param>
        /// <returns>The interpolated mask value at the specified normalized coordinates, normalized between the provided minimum
        /// and maximum values.</returns>
        private float GetMaskValueBilinear(Tensor<float> maskTensor, int maskHeight, int maskWidth, float normalizedY, float normalizedX, float minValue, float maxValue)
        {
            float fy = normalizedY * (maskHeight - 1);
            float fx = normalizedX * (maskWidth - 1);

            int y0 = Math.Clamp((int)fy, 0, maskHeight - 1);
            int y1 = Math.Clamp(y0 + 1, 0, maskHeight - 1);
            int x0 = Math.Clamp((int)fx, 0, maskWidth - 1);
            int x1 = Math.Clamp(x0 + 1, 0, maskWidth - 1);

            float dy = fy - y0;
            float dx = fx - x0;

            float v00 = NormalizeMaskValue(GetMaskValue(maskTensor, y0, x0), minValue, maxValue);
            float v01 = NormalizeMaskValue(GetMaskValue(maskTensor, y0, x1), minValue, maxValue);
            float v10 = NormalizeMaskValue(GetMaskValue(maskTensor, y1, x0), minValue, maxValue);
            float v11 = NormalizeMaskValue(GetMaskValue(maskTensor, y1, x1), minValue, maxValue);

            return v00 * (1 - dy) * (1 - dx) +
                   v01 * (1 - dy) * dx +
                   v10 * dy * (1 - dx) +
                   v11 * dy * dx;
        }

        /// <summary>
        /// Applies a fading effect to the edges of the specified bitmap, using the original bitmap as a reference for
        /// color information.
        /// </summary>
        /// <remarks>The method uses a Bayer matrix for dithering to achieve a smooth fade transition at
        /// the edges. The fade effect is based on a blurred mask, and the alpha channel is set according to a threshold
        /// derived from the mask and the Bayer matrix.</remarks>
        /// <param name="bitmap">The bitmap to which the fading effect is applied. Must have the same dimensions as the original bitmap.</param>
        /// <param name="original">The original bitmap used as a reference for color values. Must match the dimensions of the bitmap parameter.</param>
        /// <param name="fadeWidth">The width, in pixels, of the fade effect applied to the edges. Determines how far the fade extends from the
        /// bitmap's borders.</param>
        /// <returns>A new SKBitmap containing the original image with faded edges, where the alpha channel is adjusted to create
        /// a smooth transition.</returns>
        private SKBitmap FadeEgdes(SKBitmap bitmap, int fadeWidth)
        {
            SKBitmap finalMask = new SKBitmap(new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Gray8, SKAlphaType.Opaque));
            using var canvas = new SKCanvas(finalMask);
            using var paint = new SKPaint();
            paint.ImageFilter = SKImageFilter.CreateBlur(fadeWidth, fadeWidth);
            canvas.DrawBitmap(bitmap, 0, 0, paint);

            var output = new SKBitmap(new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Gray8, SKAlphaType.Opaque));

            var originalPixmap = bitmap.PeekPixels() ?? throw new InvalidOperationException("Unable to access mask pixels.");
            var blurredPixmap = finalMask.PeekPixels() ?? throw new InvalidOperationException("Unable to access blurred mask pixels.");
            var outputPixmap = output.PeekPixels() ?? throw new InvalidOperationException("Unable to access output mask pixels.");

            var originalSpan = originalPixmap.GetPixelSpan<byte>();
            var blurredSpan = blurredPixmap.GetPixelSpan<byte>();
            var outputSpan = outputPixmap.GetPixelSpan<byte>();
            int originalRowBytes = originalPixmap.RowBytes;
            int blurredRowBytes = blurredPixmap.RowBytes;
            int outputRowBytes = outputPixmap.RowBytes;

            int[,] bayerMatrix = new int[4, 4]
            {
                {  0,  8,  2, 10 },
                { 12,  4, 14,  6 },
                {  3, 11,  1,  9 },
                { 15,  7, 13,  5 }
            };

            for (int y = 0; y < bitmap.Height; y++)
            {
                int originalRow = y * originalRowBytes;
                int blurredRow = y * blurredRowBytes;
                int outputRow = y * outputRowBytes;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    float originalMaskValue = originalSpan[originalRow + x] / 255f;
                    float blurredMaskValue = blurredSpan[blurredRow + x] / 255f;
                    float maskValue = Math.Max(originalMaskValue, blurredMaskValue);

                    float threshold = bayerMatrix[y % 4, x % 4] / 16f;

                    byte maskColor = maskValue > threshold ? (byte)255 : (byte)0;
                    outputSpan[outputRow + x] = maskColor;
                }
            }

            return output;
        }

        /// <summary>
        /// Generates a mask bitmap from the specified input bitmap by applying a machine learning model to extract
        /// pixel-level mask information.
        /// </summary>
        /// <remarks>The input bitmap is resized to a fixed size before processing. The mask is created
        /// based on the output of a machine learning model, and an optional fade effect can be applied to smooth the
        /// mask's edges.</remarks>
        /// <param name="inputBitmap">The bitmap image from which the mask is generated. This parameter must not be null.</param>
        /// <param name="fadeWidth">The width, in pixels, of the fade effect applied to the edges of the mask. Specify a value greater than zero
        /// to enable edge fading.</param>
        /// <returns>An SKBitmap containing the generated mask, with the same dimensions as the input bitmap.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the input bitmap cannot be resized or if the output tensor from the model has an unexpected
        /// shape.</exception>
        private SKBitmap CreateMask(SKBitmap inputBitmap, int fadeWidth)
        {
            ArgumentNullException.ThrowIfNull(inputBitmap);

            var resizedBitmap = inputBitmap.Resize(new SKImageInfo(ModelInputSize, ModelInputSize), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)) ?? throw new InvalidOperationException("Failed to resize the input bitmap.");

            var inputTensor = ExtractPixels(resizedBitmap);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputName!, inputTensor)
            };

            using var results = _session!.Run(inputs);
            var outputTensor = results.First(r => r.Name == _outputName).AsTensor<float>();

            var dimensions = outputTensor.Dimensions;
            var rank = outputTensor.Rank;
            if (rank < 2)
            {
                throw new InvalidOperationException("Unexpected mask tensor shape.");
            }

            int maskWidth = dimensions[rank -1];
            int maskHeight = dimensions[rank - 2];

            var (min, max) = GetMaskRange(outputTensor);

            var maskBitmap = new SKBitmap(new SKImageInfo(inputBitmap.Width, inputBitmap.Height, SKColorType.Gray8, SKAlphaType.Opaque));

            var maskPixmap = maskBitmap.PeekPixels() ?? throw new InvalidOperationException("Unable to access mask pixels.");

            var maskSpan = maskPixmap.GetPixelSpan<byte>();
            int maskRowBytes = maskPixmap.RowBytes;

            for (int y = 0; y < inputBitmap.Height; y++)
            {
                int maskRow = y * maskRowBytes;
                float normalizedY = inputBitmap.Height > 1 ? y / (float)(inputBitmap.Height - 1) : 0f;
                for (int x = 0; x < inputBitmap.Width; x++)
                {
                    float normalizedX = inputBitmap.Width > 1 ? x / (float)(inputBitmap.Width - 1) : 0f;
                    var maskValue = GetMaskValueBilinear(outputTensor, maskHeight, maskWidth, normalizedY, normalizedX, min, max);
                    byte grayValue = maskValue < 0.5f ? (byte)255 : (byte)0;
                    maskSpan[maskRow + x] = grayValue;
                }
            }

            if (fadeWidth > 0)
            {
                return FadeEgdes(maskBitmap, fadeWidth);
            }

            return maskBitmap;

        }

        /// <summary>
        /// Generates a mask image from the specified input image and returns it as an NDArray.
        /// </summary>
        /// <remarks>The method creates a temporary mask image file named 'mask.png', which is deleted
        /// after the mask is loaded and returned.</remarks>
        /// <param name="inputImagePath">The file path of the input image from which the mask will be generated. This must reference a valid image
        /// file.</param>
        /// <param name="fadeWidth">The width, in pixels, of the fade effect applied to the mask. Must be a non-negative integer. The default
        /// value is 30.</param>
        /// <returns>An NDArray containing the generated mask image.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the input image cannot be loaded from the specified path.</exception>
        public NDArray GetMask(String inputImagePath, int fadeWidth = 30)
        {
            using var inputBitmap = SKBitmap.Decode(inputImagePath) ?? throw new InvalidOperationException("Failed to load the input image.");
            LoadModel();
            SKBitmap mask = CreateMask(inputBitmap, fadeWidth);
            return ConvertMaskToNdArray(mask);
        }

        private static NDArray ConvertMaskToNdArray(SKBitmap mask)
        {
            var pixmap = mask.PeekPixels() ?? throw new InvalidOperationException("Unable to access mask pixels.");
            var span = pixmap.GetPixelSpan<byte>();

            // Mask is Gray8, so one channel. Create NDArray with shape [height, width, 1]
            var data = span.ToArray();
            return np.array(data).reshape(new Shape(mask.Height, mask.Width, 1));
        }
    }
}
