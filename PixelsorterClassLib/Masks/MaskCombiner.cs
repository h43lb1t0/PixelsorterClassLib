using NumSharp;

namespace PixelsorterClassLib.Masks
{
    public class MaskCombiner
    {

        /// <summary>
        /// Subtracts one mask array from another and returns the resulting mask.
        /// </summary>
        /// <param name="minuendMask">The mask array from which values will be subtracted. Must be compatible in shape with <paramref
        /// name="subtrahendMask"/>.</param>
        /// <param name="subtrahendMask">The mask array to subtract from <paramref name="minuendMask"/>. Must be compatible in shape with <paramref
        /// name="minuendMask"/>.</param>
        /// <returns>An NDArray representing the result of subtracting <paramref name="subtrahendMask"/> from <paramref
        /// name="minuendMask"/>.</returns>
        public static NDArray SubtractMasks(NDArray minuendMask, NDArray subtrahendMask)
        {
            var combinedMask = minuendMask - subtrahendMask;
            return combinedMask;
        }

        /// <summary>
        /// Combines two masks by performing element-wise addition.
        /// </summary>
        /// <param name="mask1">The first mask to be combined. Must be an NDArray of compatible shape with <paramref name="mask2"/>.</param>
        /// <param name="mask2">The second mask to be combined. Must be an NDArray of compatible shape with <paramref name="mask1"/>.</param>
        /// <returns>An NDArray representing the element-wise sum of the two input masks.</returns>
        public static NDArray AddMasks(NDArray mask1, NDArray mask2)
        {
            var combinedMask = mask1 + mask2;
            return combinedMask;
        }
    }
}
