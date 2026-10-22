using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Neighbor.Core;
using Neighbor.Windows.UI;

namespace Neighbor.Tests
{
    internal static class DesktopPetTests
    {
        public static void Run(Action<bool, string> check)
        {
            var pet = new DesktopPetMotion();
            pet.Advance(.1, -1920, -128, 1, false);
            double start = pet.X;
            for (int i = 0; i < 30; i++) pet.Advance(.1, -1920, -128, 1, true);
            check(pet.X == start && !pet.Walking, "Pet suspension freezes movement and rest clock");
            for (int i = 0; i < 30; i++) pet.Advance(.1, -1920, -128, 1, false);
            check(pet.Walking && pet.X < start && pet.FacingLeft, "Pet starts leftward walk on negative-coordinate monitor");
            bool bounded = true, rested = false;
            for (int i = 0; i < 2000; i++)
            {
                pet.Advance(.1, -1920, -128, 1, false);
                bounded &= pet.X >= -1920 && pet.X <= -128;
                rested |= !pet.Walking;
            }
            check(bounded && rested, "Pet remains in work area and rests between journeys");
            pet.Advance(60, 0, 100, 1, false);
            check(pet.X >= 0 && pet.X <= 100, "Pet clamps position after monitor removal without a sleep jump");
            pet.Advance(.1, 5, 0, 1, false);
            check(pet.X == 5, "Pet handles a work area narrower than its window");
            DesktopPetMotion.PawOffset(0, 0, out float x, out float y);
            check(Math.Abs(x) < .001 && y == 0, "Walking starts with a neutral planted paw");
            DesktopPetMotion.PawOffset(20, 1, out x, out y);
            check(Math.Abs(x) <= 43 && y <= 0 && y >= -28, "Paw swing remains bounded");
            check(new AppSettings { DesktopPetEnabled = false }.Clone().DesktopPetEnabled == false, "Pet preference survives settings clone");
            check(!new AppSettings().DesktopPetEnabled, "Pet is off by default, matching the macOS first-run state");
            string directory = Path.Combine(Path.GetTempPath(), "Neighbor-pet-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var store = new SettingsStore(Path.Combine(directory, "settings.json"));
                store.Save(new AppSettings { DesktopPetEnabled = false });
                check(!store.Load().DesktopPetEnabled, "Pet preference survives settings save and reload");
            }
            finally { Directory.Delete(directory, true); }
            using (var artwork = new DesktopPetArtwork())
            using (var image = new Bitmap(128, 128))
            using (var graphics = Graphics.FromImage(image))
            {
                int rendered = 0;
                foreach (bool left in new[] { false, true })
                    for (int i = 0; i < 16; i++)
                    {
                        graphics.Clear(Color.Transparent); artwork.Draw(graphics, 128, true, left, i * 2.5);
                        if (image.GetPixel(64, 88).A > 0) rendered++;
                    }
                graphics.Clear(Color.Transparent); artwork.Draw(graphics, 128, false, false, 0);
                check(rendered == 32 && image.GetPixel(64, 88).A > 0, "Original artwork renders sitting and 32 directional walking frames");
                check(image.GetPixel(0, 0).A == 0, "Pet artwork retains transparent background");
                string output = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-results", "pet-preview");
                Directory.CreateDirectory(output);
                image.Save(Path.Combine(output, "sitting.png"), ImageFormat.Png);
                foreach (bool left in new[] { false, true })
                {
                    using (var strip = new Bitmap(128 * 8, 128))
                    using (var canvas = Graphics.FromImage(strip))
                    {
                        canvas.Clear(Color.FromArgb(247, 246, 243));
                        for (int i = 0; i < 8; i++)
                        {
                            var state = canvas.Save(); canvas.TranslateTransform(i * 128, 0);
                            artwork.Draw(canvas, 128, true, left, i * 5); canvas.Restore(state);
                        }
                        strip.Save(Path.Combine(output, left ? "walking-left.png" : "walking-right.png"), ImageFormat.Png);
                    }
                }
            }
        }
    }
}
