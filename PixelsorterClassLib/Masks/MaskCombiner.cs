using NumSharp;

namespace PixelsorterClassLib.Masks
{
    public class MaskCombiner
    {

        public static NDArray SubtractMasks(NDArray minuendMask, NDArray subthrahendMask)
        {
            var combinedMask = minuendMask - subthrahendMask;
            return combinedMask;
        }

        public static NDArray AddMasks(NDArray mask1, NDArray mask2)
        {
            var combinedMask = mask1 + mask2;
            return combinedMask;
        }
    }
}
