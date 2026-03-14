namespace PixelsorterClassLib.Core
{

    /// <summary>
    /// Specifies the direction in which sorting is applied, either horizontally or vertically.
    /// </summary>
    /// <remarks>Use this enumeration to define the order in which data is sorted, such as when processing
    /// rows or columns in a two-dimensional structure. The values allow for flexible control over sorting orientation,
    /// supporting both left-to-right and right-to-left for rows, as well as top-to-bottom and bottom-to-top for
    /// columns.</remarks>
    public enum SortDirections
    {
        RowLeftToRight,
        RowRightToLeft,
        ColumnTopToBottom,
        ColumnBottomToTop,
        IntoMask
    }
}
