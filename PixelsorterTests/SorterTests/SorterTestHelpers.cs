using NumSharp;

namespace Pixelsorter.Tests.SorterTests
{
    public class SorterTestHelpers
    {
        // HSL pixel definitions (H: 0-360, S: 0-1, L: 0-1)
        // Saturation ordering: Gray (0) < LowSaturation (0.3) < MidSaturation (0.5) < HighSaturation (1.0)
        public static readonly float[] Gray =           [0f, 0.0f, 0.5f];
        public static readonly float[] LowSaturation =  [0f, 0.3f, 0.7f];
        public static readonly float[] MidSaturation =  [0f, 0.5f, 0.6f];
        public static readonly float[] HighSaturation = [0f, 1.0f, 0.4f];

        public static NDArray CreateUnsortedImageData()
        {
            return np.array([
                ..HighSaturation, ..LowSaturation, ..Gray, ..MidSaturation,
                ..MidSaturation, ..HighSaturation, ..LowSaturation, ..Gray,
                ..LowSaturation, ..Gray, ..MidSaturation, ..HighSaturation,
                ..Gray, ..MidSaturation, ..HighSaturation, ..LowSaturation
            ]).reshape(4, 4, 3);
        }
    }
}
