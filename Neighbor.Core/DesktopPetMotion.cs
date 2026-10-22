using System;

namespace Neighbor.Core
{
    /// <summary>Small, deterministic first-stage walking model. Coordinates are screen pixels.</summary>
    public sealed class DesktopPetMotion
    {
        public double X { get; private set; }
        public double Distance { get; private set; }
        public bool FacingLeft { get; private set; }
        public bool Walking { get; private set; }
        private double rest = 2, journey;
        private bool placed;

        public void Advance(double seconds, double left, double right, double scale, bool paused)
        {
            right = Math.Max(left, right);
            if (!placed) { X = left + (right - left) * .65; placed = true; }
            X = Math.Max(left, Math.Min(right, X));
            if (paused || seconds <= 0 || scale <= 0) return;
            seconds = Math.Min(seconds, .25); // No jump after sleep or a blocked UI thread.
            if (!Walking)
            {
                rest -= seconds;
                if (rest > 0 || right - left < scale) return;
                Walking = true; journey = 0; Distance = 0;
                FacingLeft = X > (left + right) / 2;
            }
            double step = 55 * scale * seconds;
            double next = Math.Max(left, Math.Min(right, X + (FacingLeft ? -step : step)));
            Distance += Math.Abs(next - X) / scale;
            X = next; journey += seconds;
            if (X <= left || X >= right || journey >= 7)
            { Walking = false; rest = 3; }
        }

        // Ported from macOS DesktopPetView.pawOffset; alternate diagonal pairs.
        public static void PawOffset(double distance, int pair, out float x, out float y)
        {
            const double stride = 86 * .28 / .6;
            distance = Math.Max(0, distance);
            double cycle = (distance / stride + .3 + pair * .5) % 1;
            if (cycle < .6) { x = (float)(-43 + 86 * cycle / .6); y = 0; return; }
            double swing = (cycle - .6) / .4, eased = swing * swing * (3 - 2 * swing);
            double startup = Math.Min(1, distance / (stride * .08));
            x = (float)(43 - 86 * eased);
            y = (float)(-Math.Sin(swing * Math.PI) * 28 * startup * startup * (3 - 2 * startup));
        }
    }
}
