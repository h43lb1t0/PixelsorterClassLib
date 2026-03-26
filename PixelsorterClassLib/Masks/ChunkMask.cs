using NumSharp;
using PixelsorterClassLib.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace PixelsorterClassLib.Masks
{
    /// <summary>
    /// Represents configuration options for a chunk-based mask, including chunk size limits, sorting direction, and
    /// mask thickness.
    /// </summary>
    /// <remarks>Use this type to specify how chunks are generated and ordered when applying a chunk mask. The
    /// chunk size and thickness parameters control the granularity and appearance of the mask, while the sort direction
    /// determines the order in which chunks are processed.</remarks>
    public record ChunkMaskOptions : MaskOptions
    {
        public int MinChunkSize { get; init; }
        public int MaxChunkSize { get; init; }

        public int Thickness { get; init; }
        public SortDirections SortDirection { get; init; }

        public ChunkMaskOptions(int minChunkSize, int maxChunkSize, SortDirections sortDirection, int thickness = 10)
        {
            if (minChunkSize <= 1) throw new ArgumentException("Chunk size must be greater then 1");
            if (maxChunkSize <= minChunkSize) throw new ArgumentException("Max chunk size must be greater then min chunk size");
            if (sortDirection is SortDirections.IntoMask) throw new ArgumentException("Into mask sort direction is not suported by this mask type");
            if (thickness <= 0 || thickness > 50) throw new ArgumentException("Thickness must be in range (0,50]");
            MinChunkSize = minChunkSize;
            MaxChunkSize = maxChunkSize;
            Thickness = thickness;
            SortDirection = sortDirection;
        }
    }

    public class ChunkMask : Mask<ChunkMaskOptions>
    {

        private int minChunksize;
        private int maxChunksize;
        private bool isColumnWise;

        private int thickness;

        private int width;
        private int height;

        /// <summary>
        /// Sets the image dimensions and validates the provided chunk mask options for compatibility with the image
        /// size and sort direction.
        /// </summary>
        /// <param name="image">The image whose dimensions are used to determine valid chunk mask options. The image is disposed after use.</param>
        /// <param name="options">The chunk mask options to validate against the image dimensions and sort direction. Specifies chunk sizes,
        /// thickness, and sort direction.</param>
        /// <exception cref="ArgumentException">Thrown if the maximum chunk size specified in <paramref name="options"/> exceeds the corresponding image
        /// dimension for the selected sort direction.</exception>
        private void SetDimAndCheckOptionsValidity(Image<L8> image, ChunkMaskOptions options)
        {
            width = image.Width;
            height = image.Height;
            image.Dispose();

            switch (options.SortDirection)
            {
                case SortDirections.ColumnBottomToTop:
                case SortDirections.ColumnTopToBottom:
                    if (options.MaxChunkSize > height) throw new ArgumentException("Max chunk size can't be greater then image height");
                    this.isColumnWise = true;
                    break;
                case SortDirections.RowLeftToRight:
                case SortDirections.RowRightToLeft:
                    if (options.MaxChunkSize > width) throw new ArgumentException("Max chunk size can't be greater then image width");
                    this.isColumnWise = false;
                    break;
            }
            this.minChunksize = options.MinChunkSize;
            this.maxChunksize = options.MaxChunkSize;
            this.thickness = options.Thickness;

        }

        /// <summary>
        /// Generates a pair of chunk masks as NDArray objects, representing segmented regions and their inverses based
        /// on the current configuration.
        /// </summary>
        /// <remarks>The chunk masks are created according to the configured orientation, chunk size
        /// range, and thickness. The method uses randomization to determine chunk positions, resulting in different
        /// masks on each invocation.</remarks>
        /// <returns>A tuple containing two NDArray objects: the first is the generated chunk mask, and the second is its
        /// inverted mask.</returns>
        private (NDArray mask, NDArray invertedMask) CreateChunks()
        {
            var chunkMask = new Image<L8>(width, height);
            Random rnd = new Random();

            int previousStart = 0;
            int previousEnd = 0;
            int length;

            int limitPrimary = isColumnWise ? this.width : this.height;
            int limitSecondary = isColumnWise ? this.height : this.width;


            for (int p = 0; p < limitPrimary; p += thickness)
            {
                length = rnd.Next(minChunksize, maxChunksize + 1);

                int currentStart;
                int currentEnd;

                if (p == 0)
                {
                    currentStart = rnd.Next(0, limitSecondary - length + 1);
                }
                else
                {
                    int earliestStart = Math.Max(0, previousStart - length + 1);
                    int latestStart = Math.Min(previousEnd, limitSecondary - length);

                    if (earliestStart > latestStart) earliestStart = latestStart;

                    currentStart = rnd.Next(earliestStart, latestStart + 1);
                }

                currentEnd = currentStart + length - 1;

                for (int s = 0; s < limitSecondary; s++)
                {
                    for (int pMod = 0; pMod < thickness && p + pMod < limitPrimary; pMod++)
                    {
                        int x = isColumnWise ? p + pMod : s;
                        int y = isColumnWise ? s : p + pMod;

                        chunkMask[x, y] = new L8((s >= currentStart && s <= currentEnd) ? (byte)255 : (byte)0);
                    }
                }

                previousStart = currentStart;
                previousEnd = currentEnd;
            }

            var inverted = chunkMask.Clone(x => x.Invert());

            return (ConvertMaskToNdArray(chunkMask), ConvertMaskToNdArray(inverted));
        }

        public override (NDArray mask, NDArray invertedMask) GetMask(string imagePath, ChunkMaskOptions options)
        {
            SetDimAndCheckOptionsValidity(LoadL8Image(imagePath), options);
            return CreateChunks();
        }


        public override Task<(NDArray mask, NDArray invertedMask)> GetMaskAsync(string imagePath, ChunkMaskOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                SetDimAndCheckOptionsValidity(LoadL8Image(imagePath), options);
                cancellationToken.ThrowIfCancellationRequested();
                return CreateChunks();
            }, cancellationToken);
        }
    }
}
