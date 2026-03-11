using NumSharp;

namespace Pixelsorter.Tests.SorterTests
{
    public class SorterTestHelpers
    {
        public static readonly byte[] Gray = [128, 128, 128, 255];
        public static readonly byte[] LowSaturation = [200, 150, 150, 255];
        public static readonly byte[] MidSaturation = [200, 100, 100, 255];
        public static readonly byte[] HighSaturation = [200, 0, 0, 255];

        public static NDArray CreateUnsortedImageData()
        {
            return np.array([
                ..HighSaturation, ..LowSaturation, ..Gray, ..MidSaturation,
                ..MidSaturation, ..HighSaturation, ..LowSaturation, ..Gray,
                ..LowSaturation, ..Gray, ..MidSaturation, ..HighSaturation,
                ..Gray, ..MidSaturation, ..HighSaturation, ..LowSaturation
            ]).reshape(4, 4, 4);
        }
    }
}
