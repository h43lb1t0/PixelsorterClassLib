using HuggingfaceHub;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PixelsorterClassLib.Masks
{

    /// <summary>
    /// Represents options for configuring a background mask with a specified fade width.
    /// </summary>
    /// <param name="fadeWidth">The width, in pixels, over which the background mask fades. Must be a non-negative integer.</param>
    public record BackgroundMaskOptions(int fadeWidth) : MaskOptions;

    /// <summary>
    /// Provides functionality to generate a mask from an input image using a pre-trained model.
    /// </summary>
    /// <remarks>This class internally manages the loading of the model and the processing of images to create
    /// masks. It requires an input image path and can apply a fade effect to the edges of the generated mask. The model
    /// input size is fixed at 1024x1024 pixels, and the class handles image normalization and tensor extraction for
    /// model inference.</remarks>
    public class BackgroundMask : Mask<BackgroundMaskOptions>
    {
        private static InferenceSession? _session;
        private static string? _inputName;
        private static string? _outputName;
        private static readonly object _sessionLock = new();
        private const int ModelInputSize = 1024;
        private static readonly string ModelCachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PixelsorterClassLib",
            "Models",
            "briaai",
            "RMBG-1.4",
            "model_quantized.onnx");

        /// <summary>
        /// Gets a value indicating whether the model has been downloaded and is available in the cache.
        /// </summary>
        /// <remarks>This property checks for the existence of the model file at the specified cache path.
        /// If the file exists, it indicates that the model has been successfully downloaded and cached for
        /// use.</remarks>
        public override bool IsReadyToUse => File.Exists(ModelCachePath);

        /// <summary>
        /// Downloads the model file from a remote repository and caches it locally if it has not been downloaded yet.
        /// </summary>
        /// <remarks>This method creates the necessary directory for caching the model if it does not
        /// already exist. It checks if the model has already been downloaded before attempting to download it
        /// again.</remarks>
        /// <exception cref="FileNotFoundException">Thrown if the model download fails and the downloaded file does not exist at the expected path.</exception>
        public override async Task<bool> DownloadModel()
        {
            if (!IsReadyToUse)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ModelCachePath)!);
                var downloadedPath = await HFDownloader.DownloadFileAsync(
                    repoId: "briaai/RMBG-1.4",
                    filename: "onnx/model_quantized.onnx"
                );

                if (!File.Exists(downloadedPath))
                {
                    throw new FileNotFoundException("Model download failed.", downloadedPath);
                }

                if (!string.Equals(downloadedPath, ModelCachePath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(downloadedPath, ModelCachePath, overwrite: true);
                }
            }
            return true;
        }

        private Image<Rgba32> LoadImage(String inputImagePath)
        {
            return Image.Load<Rgba32>(inputImagePath) ?? throw new InvalidOperationException("Failed to load the input image.");
        }


        /// <summary>
        /// Initializes the inference session for the model if it has not already been loaded.
        /// </summary>
        /// <remarks>This method ensures that the model is downloaded and the session is created with
        /// optimized settings. It is thread-safe and uses a lock to prevent multiple initializations.</remarks>
        private void LoadModel()
        {
            if (_session != null) return;

            lock (_sessionLock)
            {
                if (_session != null) return;

                _ = DownloadModel();

                var options = new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                    EnableMemoryPattern = false,
                    InterOpNumThreads = 1,
                    IntraOpNumThreads = Math.Max(1, Environment.ProcessorCount - 1)
                };

                _session = new InferenceSession(ModelCachePath, options);
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
        /// <param name="image">The Image instance containing the image data to be processed. The image must have dimensions equal to
        /// ModelInputSize by ModelInputSize.</param>
        /// <returns>A DenseTensor<float> containing the normalized RGB pixel values, structured as [1, 3, ModelInputSize,
        /// ModelInputSize]. Each channel value is normalized to the range [-0.5, 0.5].</returns>
        private DenseTensor<float> ExtractPixels(Image<Rgba32> image)
        {
            var tensor = new DenseTensor<float>(new[] { 1, 3, ModelInputSize, ModelInputSize });

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < ModelInputSize; y++)
                {
                    var pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < ModelInputSize; x++)
                    {
                        var pixel = pixelRow[x];
                        tensor[0, 0, y, x] = pixel.R / 255f - 0.5f;
                        tensor[0, 1, y, x] = pixel.G / 255f - 0.5f;
                        tensor[0, 2, y, x] = pixel.B / 255f - 0.5f;
                    }
                }
            });

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
        /// <param name="maskImage">The grayscale mask image to which the fading effect is applied.</param>
        /// <param name="fadeWidth">The width, in pixels, of the fade effect applied to the edges. Determines how far the fade extends from the
        /// bitmap's borders.</param>
        /// <returns>A new Image containing the mask with faded edges, where values are adjusted to create
        /// a smooth transition.</returns>
        private Image<L8> FadeEgdes(Image<L8> maskImage, int fadeWidth)
        {
            var finalMask = maskImage.Clone();
            finalMask.Mutate(x => x.GaussianBlur(fadeWidth));

            var output = new Image<L8>(maskImage.Width, maskImage.Height);

            int[,] bayerMatrix = new int[4, 4]
            {
                {  0,  8,  2, 10 },
                { 12,  4, 14,  6 },
                {  3, 11,  1,  9 },
                { 15,  7, 13,  5 }
            };

            maskImage.ProcessPixelRows(finalMask, output, (originalAccessor, blurredAccessor, outputAccessor) =>
            {
                for (int y = 0; y < maskImage.Height; y++)
                {
                    var originalRow = originalAccessor.GetRowSpan(y);
                    var blurredRow = blurredAccessor.GetRowSpan(y);
                    var outputRow = outputAccessor.GetRowSpan(y);

                    for (int x = 0; x < maskImage.Width; x++)
                    {
                        float originalMaskValue = originalRow[x].PackedValue / 255f;
                        float blurredMaskValue = blurredRow[x].PackedValue / 255f;
                        float maskValue = Math.Max(originalMaskValue, blurredMaskValue);

                        float threshold = bayerMatrix[y % 4, x % 4] / 16f;

                        byte maskColor = maskValue > threshold ? (byte)255 : (byte)0;
                        outputRow[x] = new L8(maskColor);
                    }
                }
            });

            finalMask.Dispose();
            return output;
        }

        /// <summary>
        /// Generates a mask bitmap from the specified input bitmap by applying a machine learning model to extract
        /// pixel-level mask information.
        /// </summary>
        /// <remarks>The input bitmap is resized to a fixed size before processing. The mask is created
        /// based on the output of a machine learning model, and an optional fade effect can be applied to smooth the
        /// mask's edges.</remarks>
        /// <param name="inputImage">The image from which the mask is generated. This parameter must not be null.</param>
        /// <param name="fadeWidth">The width, in pixels, of the fade effect applied to the edges of the mask. Specify a value greater than zero
        /// to enable edge fading.</param>
        /// <returns>A tuple containing the generated mask and inverted mask as Image<L8>, with the same dimensions as the input image.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the input bitmap cannot be resized or if the output tensor from the model has an unexpected
        /// shape.</exception>
        private (Image<L8> mask, Image<L8> invertedMask) CreateMask(Image<Rgba32> inputImage, int fadeWidth)
        {
            ArgumentNullException.ThrowIfNull(inputImage);

            int originalWidth = inputImage.Width;
            int originalHeight = inputImage.Height;

            using var resizedImage = inputImage.Clone(ctx => ctx.Resize(ModelInputSize, ModelInputSize));

            var inputTensor = ExtractPixels(resizedImage);

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

            int maskWidth = dimensions[rank - 1];
            int maskHeight = dimensions[rank - 2];

            var (min, max) = GetMaskRange(outputTensor);

            var maskImage = new Image<L8>(originalWidth, originalHeight);
            var invertedMaskImage = new Image<L8>(originalWidth, originalHeight);

            maskImage.ProcessPixelRows(invertedMaskImage, (maskAccessor, invertedAccessor) =>
            {
                for (int y = 0; y < originalHeight; y++)
                {
                    var maskRow = maskAccessor.GetRowSpan(y);
                    var invertedMaskRow = invertedAccessor.GetRowSpan(y);
                    float normalizedY = originalHeight > 1 ? y / (float)(originalHeight - 1) : 0f;

                    for (int x = 0; x < originalWidth; x++)
                    {
                        float normalizedX = originalWidth > 1 ? x / (float)(originalWidth - 1) : 0f;
                        var maskValue = GetMaskValueBilinear(outputTensor, maskHeight, maskWidth, normalizedY, normalizedX, min, max);
                        byte grayValue = maskValue < 0.5f ? (byte)255 : (byte)0;
                        maskRow[x] = new L8(grayValue);
                        invertedMaskRow[x] = new L8(grayValue == 255 ? (byte)0 : (byte)255);
                    }
                }
            });

            if (fadeWidth > 0)
            {
                var fadedMask = FadeEgdes(maskImage, fadeWidth);
                var fadedInvertedMask = FadeEgdes(invertedMaskImage, fadeWidth);
                maskImage.Dispose();
                invertedMaskImage.Dispose();
                return (fadedMask, fadedInvertedMask);
            }

            return (maskImage, invertedMaskImage);
        }

        /// <summary>
        /// Generates a mask image from the specified input image and returns it as an NDArray.
        /// </summary>
        /// <param name="inputImagePath">The file path of the input image from which the mask will be generated. This must reference a valid image
        /// file.</param>
        /// <param name="options">The options controlling mask generation, including the fade width applied to mask edges.</param>
        /// <returns>A tuple containing the generated mask and inverted mask as NDArrays.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the input image cannot be loaded from the specified path.</exception>
        public override (NDArray mask, NDArray invertedMask) GetMask(String inputImagePath, BackgroundMaskOptions options)
        {
            using var inputImage = LoadImage(inputImagePath);
            LoadModel();
            (var mask, var invertedMask) = CreateMask(inputImage, options.fadeWidth);
            return (ConvertMaskToNdArray(mask), ConvertMaskToNdArray(invertedMask));
        }

        /// <summary>
        /// Asynchronously generates a mask image from the specified input image and returns it as an NDArray.
        /// </summary>
        /// <param name="inputImagePath">Path to the image file to process.</param>
        /// <param name="options">The options controlling mask generation, including the fade width applied to mask edges.</param>
        /// <param name="cancellationToken">Token to cancel the work.</param>
        /// <returns>A task returning a tuple containing the generated mask and inverted mask as NDArrays.</returns>
        public override Task<(NDArray mask, NDArray invertedMask)> GetMaskAsync(string inputImagePath, BackgroundMaskOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var inputImage = LoadImage(inputImagePath);
                cancellationToken.ThrowIfCancellationRequested();
                LoadModel();
                cancellationToken.ThrowIfCancellationRequested();
                (var mask, var invertedMask) = CreateMask(inputImage, options.fadeWidth);
                return (ConvertMaskToNdArray(mask), ConvertMaskToNdArray(invertedMask));
            }, cancellationToken);
        }

    }
}