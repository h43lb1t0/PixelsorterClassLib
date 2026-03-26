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
        private bool sortDirectionIsRow;

        private int maxLenght;
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
                    this.sortDirectionIsRow = false;
                    this.maxLenght = height;
                    break;
                case SortDirections.RowLeftToRight:
                case SortDirections.RowRightToLeft:
                    if (options.MaxChunkSize > width) throw new ArgumentException("Max chunk size can't be greater then image width");
                    this.sortDirectionIsRow = true;
                    this.maxLenght = width;
                    break;
            }
            this.minChunksize = options.MinChunkSize;
            this.maxChunksize = options.MaxChunkSize;
            this.thickness = options.Thickness;

        }

        private (NDArray mask, NDArray invertedMask) CreateChunks()
        {

            var chunkMask = new Image<L8>(width, height);

            Random rnd = new Random();

            int previousStart = 0;
            int previousEnd = 0;
            int length;

            for (int y = 0; y < this.height; y += thickness)
            {
                length = rnd.Next(minChunksize, maxChunksize + 1);

                int currentStart;
                int currentEnd;

                if (y == 0)
                {
                    currentStart = rnd.Next(0, this.width - length + 1);
                }
                else
                {
                    int earliestStart = Math.Max(0, previousStart - length + 1);

                    int latestStart = Math.Min(previousEnd, this.width - length);

                    if (earliestStart > latestStart) earliestStart = latestStart;

                    currentStart = rnd.Next(earliestStart, latestStart + 1);
                }

                currentEnd = currentStart + length - 1;

                for (int x = 0; x < this.width; x++)
                {
                    for (int yMod = 0; yMod < thickness && y + yMod < this.height; yMod++)
                    {
                        chunkMask[x, y + yMod] = new L8((x >= currentStart && x <= currentEnd) ? (byte)255 : (byte)0);
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
