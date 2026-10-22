using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using Neighbor.Core;

namespace Neighbor.Windows.UI
{
    /// <summary>Non-activating, click-through companion. First milestone: walking and resting.</summary>
    public sealed class DesktopPetForm : NonActivatingForm
    {
        private readonly DesktopPetArtwork artwork = new DesktopPetArtwork();
        private readonly DesktopPetMotion motion = new DesktopPetMotion();
        private readonly Timer timer = new Timer { Interval = 83 };
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private double previous;
        private bool enabled, busy, locked, suspended;
        /// <summary>Raised once when the companion stops itself after an unexpected error.</summary>
        public event EventHandler Failed;
        public DesktopPetForm()
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            BackColor = TransparencyKey = Color.Magenta; DoubleBuffered = true;
            StartPosition = FormStartPosition.Manual; AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(128, 128); AccessibleName = "桌面散步小黑貓";
            timer.Tick += (_, __) => RefreshPet();
            SystemEvents.SessionSwitch += SessionChanged;
            SystemEvents.PowerModeChanged += PowerChanged;
        }
        protected override CreateParams CreateParams
        {
            get { var p = base.CreateParams; p.ExStyle |= 0x00000020 | 0x00080000; return p; } // WS_EX_TRANSPARENT | WS_EX_LAYERED
        }
        public void Configure(bool visible, bool isBusy)
        {
            if (IsDisposed) return;
            enabled = visible; busy = isBusy;
            // Off means no timer at all, so a disabled companion costs nothing while the app runs.
            if (!enabled) { timer.Stop(); Hide(); return; }
            if (!timer.Enabled) { previous = clock.Elapsed.TotalSeconds; timer.Start(); }
            RefreshPet();
        }
        private void RefreshPet()
        {
            if (IsDisposed) return;
            try { Step(); }
            catch { Stop(); }
        }
        // Stop quietly instead of letting one failing tick raise an error dialog every 83 ms.
        private void Stop()
        {
            enabled = false; timer.Stop();
            if (!IsDisposed && Visible) Hide();
            Failed?.Invoke(this, EventArgs.Empty);
        }
        private void Step()
        {
            double now = clock.Elapsed.TotalSeconds, elapsed = now - previous; previous = now;
            var area = Screen.FromPoint(Location).WorkingArea;
            int size = Math.Max(96, (int)(128 * DeviceDpi / 96.0));
            bool hidden = !enabled || busy || locked || suspended || FullScreenForeground();
            bool animate;
            if (!SystemParametersInfo(0x1042, 0, out animate, 0)) animate = true; // SPI_GETCLIENTAREAANIMATION
            motion.Advance(elapsed, area.Left, area.Right - size, size / 128.0, hidden || !animate);
            if (hidden) { Hide(); return; }
            ClientSize = new Size(size, size);
            Location = new Point((int)Math.Round(motion.X), area.Bottom - Height);
            if (!Visible) Show();
            Invalidate();
        }
        private void SessionChanged(object sender, SessionSwitchEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke(new Action(() => {
                if (e.Reason == SessionSwitchReason.SessionLock || e.Reason == SessionSwitchReason.SessionLogoff) locked = true;
                if (e.Reason == SessionSwitchReason.SessionUnlock || e.Reason == SessionSwitchReason.SessionLogon) locked = false;
                RefreshPet();
            }));
        }
        private void PowerChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke(new Action(() => { suspended = e.Mode == PowerModes.Suspend ? true : e.Mode == PowerModes.Resume ? false : suspended; RefreshPet(); }));
        }
        private static bool FullScreenForeground()
        {
            IntPtr window = GetForegroundWindow();
            if (window == IntPtr.Zero || window == GetShellWindow() || !GetWindowRect(window, out Rect rect)) return false;
            var name = new StringBuilder(64);
            GetClassName(window, name, name.Capacity);
            if (name.ToString() == "Progman" || name.ToString() == "WorkerW") return false;
            var screen = Screen.FromHandle(window);
            // Only suppress borderless full-screen windows, not maximized working-area windows.
            return rect.Left <= screen.Bounds.Left && rect.Top <= screen.Bounds.Top && rect.Right >= screen.Bounds.Right && rect.Bottom >= screen.Bounds.Bottom;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            try { artwork.Draw(e.Graphics, ClientSize.Width, motion.Walking, motion.FacingLeft, motion.Distance); }
            catch { BeginInvoke(new Action(Stop)); }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084) { m.Result = (IntPtr)(-1); return; } // HTTRANSPARENT
            base.WndProc(ref m);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { timer.Stop(); SystemEvents.SessionSwitch -= SessionChanged; SystemEvents.PowerModeChanged -= PowerChanged; timer.Dispose(); artwork.Dispose(); }
            base.Dispose(disposing);
        }
        [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder name, int maximum);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool SystemParametersInfo(uint action, uint param, [MarshalAs(UnmanagedType.Bool)] out bool value, uint flags);
    }
}
