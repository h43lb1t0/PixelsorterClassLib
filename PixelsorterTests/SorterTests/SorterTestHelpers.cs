using NumSharp;

namespace Pixelsorter.Tests.SorterTests
{
    public class SorterTestHelpers
    {
        // HSL pixel definitions (H: 0-360, S: 0-1, L: 0-1)
        // Saturation ordering: Gray (0) < LowSaturation (0.3) < MidSaturation (0.5) < HighSaturation (1.0)
        public static readonly Half[] Gray =           [(Half)0, (Half)0, (Half)0.5];
        public static readonly Half[] LowSaturation =  [(Half)0, (Half)0.3, (Half)0.7];
        public static readonly Half[] MidSaturation =  [(Half)0, (Half)0.5, (Half)0.6];
        public static readonly Half[] HighSaturation = [(Half)0, (Half)1.0, (Half)0.4];

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
