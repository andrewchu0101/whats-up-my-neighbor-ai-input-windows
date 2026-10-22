using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;
using Neighbor.Core;

namespace Neighbor.Windows.UI
{
    // Drawn into a per-pixel alpha layered window (LayeredWindow), so the color-key fringe cannot appear.
    // Child controls are not drawn into that bitmap, so the eyes are hit-tested and hover-painted here.
    public sealed class RecordingHud : NonActivatingForm
    {
        private const int WS_EX_LAYERED = 0x00080000;
        private readonly Image cat = Theme.LoadCatImage();
        private readonly ScaledCatCache catCache;
        private readonly ToolTip tips = new ToolTip();
        private DictationState state;
        private string message = "按快捷鍵開始說話";
        private double level;
        private bool levelKnown, atTop;
        private RecordingHudTarget hovered;
        public event Action StartRequested;
        public event Action FinishRequested;
        public event Action CancelRequested;

        public RecordingHud()
        {
            Font = new Font("Microsoft JhengHei UI", 9F);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false; TopMost = true;
            ClientSize = new Size(340, 220);
            StartPosition = FormStartPosition.Manual;
            AccessibleName = "小黑貓語音輸入";
            catCache = new ScaledCatCache(cat);
            UpdateState(DictationState.Idle, message);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ExStyle |= WS_EX_LAYERED;
                return parameters;
            }
        }

        public void ApplySettings(AppSettings settings)
        {
            if (settings == null) return;
            atTop = settings.HudAtTop;
            PlaceOnScreen();
        }

        public void UpdateState(DictationState value, string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdateState(value, text))); return; }
            state = value;
            message = String.IsNullOrWhiteSpace(text) ? StateLabel(value) : text;
            SetHovered(hovered);
            if (value != DictationState.Recording) { level = 0; levelKnown = false; }
            AccessibleDescription = StateLabel(value) + "，" + message;
            Redraw();
        }

        public void UpdateLevel(double decibels, bool isKnown)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdateLevel(decibels, isKnown))); return; }
            levelKnown = state == DictationState.Recording && isKnown && !Double.IsNaN(decibels) && !Double.IsInfinity(decibels);
            level = levelKnown ? RecordingHudLayout.LevelFraction(decibels, true) : 0;
            Redraw();
        }

        // Renders the current frame as premultiplied ARGB at the window's client size.
        public Bitmap RenderFrame()
        {
            var frame = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(frame))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                // ClearType writes opaque color into the text pixels and breaks per-pixel alpha, so use grayscale AA.
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                int catHeight = (int)(ClientSize.Width * .4978);
                graphics.DrawImage(catCache.Get(new Size(ClientSize.Width, catHeight)), 0, 0);
                var plate = new Rectangle(18, catHeight + 2, ClientSize.Width - 36, ClientSize.Height - catHeight - 4);
                using (var shape = LayeredWindow.RoundedPath(plate, 12))
                using (var background = new SolidBrush(Color.FromArgb(30, 31, 35))) graphics.FillPath(background, shape);
                var bounds = new RectangleF(plate.Left + 12, plate.Top + 7, plate.Width - 24, plate.Height - 11);
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                using (var brush = new SolidBrush(Color.FromArgb(241, 242, 245))) graphics.DrawString(message, Font, brush, bounds, format);
                if (state == DictationState.Recording)
                {
                    int meterWidth = (int)(ClientSize.Width * .30);
                    var meter = new Rectangle((ClientSize.Width - meterWidth) / 2, (int)(catHeight * .82), meterWidth, Math.Max(4, (int)(ClientSize.Width / 68.0)));
                    using (var brush = new SolidBrush(Color.FromArgb(61, 62, 68))) graphics.FillRectangle(brush, meter);
                    if (levelKnown && level > 0)
                    {
                        var filled = new Rectangle(meter.Left, meter.Top, Math.Max(1, (int)(meterWidth * level)), meter.Height);
                        using (var brush = new SolidBrush(Color.FromArgb(165, 193, 255))) graphics.FillRectangle(brush, filled);
                    }
                }
                PaintHover(graphics, RecordingHudTarget.Cancel, RecordingHudLayout.CancelEye(ClientSize));
                PaintHover(graphics, RecordingHudTarget.Finish, RecordingHudLayout.FinishEye(ClientSize));
            }
            return frame;
        }

        protected override void OnShown(EventArgs args) { base.OnShown(args); PlaceOnScreen(); }
        protected override void OnHandleCreated(EventArgs args) { base.OnHandleCreated(args); Redraw(); }
        protected override void OnMouseMove(MouseEventArgs args)
        {
            base.OnMouseMove(args);
            var target = Clickable(RecordingHudLayout.HitTest(args.Location, ClientSize));
            if (target != hovered) SetHovered(target);
        }
        protected override void OnMouseLeave(EventArgs args) { base.OnMouseLeave(args); SetHovered(RecordingHudTarget.None); }
        protected override void OnMouseUp(MouseEventArgs args)
        {
            base.OnMouseUp(args);
            if (args.Button != MouseButtons.Left) return;
            var target = Clickable(RecordingHudLayout.HitTest(args.Location, ClientSize));
            if (target == RecordingHudTarget.Cancel) CancelRequested?.Invoke();
            else if (target == RecordingHudTarget.Finish)
            {
                if (state == DictationState.Recording) FinishRequested?.Invoke();
                else StartRequested?.Invoke();
            }
        }

        private void PlaceOnScreen()
        {
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(area.Left + (area.Width - Width) / 2, atTop ? area.Top + 12 : area.Bottom - Height - 12);
            Redraw();
        }

        private void Redraw()
        {
            if (IsDisposed || !IsHandleCreated) return;
            using (var frame = RenderFrame()) LayeredWindow.Present(Handle, Location, frame);
        }

        private RecordingHudTarget Clickable(RecordingHudTarget target)
        {
            switch (target)
            {
                case RecordingHudTarget.Cancel: return state != DictationState.Idle && state != DictationState.Delivering ? target : RecordingHudTarget.None;
                case RecordingHudTarget.Finish: return state == DictationState.Idle || state == DictationState.Recording || state == DictationState.RetainedAudio ? target : RecordingHudTarget.None;
                default: return RecordingHudTarget.None;
            }
        }

        private void SetHovered(RecordingHudTarget target)
        {
            hovered = Clickable(target);
            Cursor = hovered == RecordingHudTarget.None ? Cursors.Default : Cursors.Hand;
            tips.SetToolTip(this, TipFor(hovered));
            Redraw();
        }

        private string TipFor(RecordingHudTarget target)
        {
            switch (target)
            {
                case RecordingHudTarget.Cancel: return "取消";
                case RecordingHudTarget.Finish: return state == DictationState.Recording ? "完成錄音" : state == DictationState.RetainedAudio ? "重試辨識" : "開始錄音";
                default: return "";
            }
        }

        private void PaintHover(Graphics graphics, RecordingHudTarget target, Rectangle eye)
        {
            if (hovered != target) return;
            using (var pen = new Pen(Color.FromArgb(160, 190, 255), 2)) graphics.DrawEllipse(pen, eye.X + 2, eye.Y + 2, eye.Width - 5, eye.Height - 5);
        }

        private static string StateLabel(DictationState value)
        {
            switch (value)
            {
                case DictationState.Starting: return "正在開啟麥克風…";
                case DictationState.Recording: return "正在聽你說話…";
                case DictationState.Finalizing: return "正在整理錄音…";
                case DictationState.Transcribing: return "正在辨識…";
                case DictationState.Delivering: return "正在放入文字…";
                case DictationState.RetainedAudio: return "辨識未完成，可重試或取消";
                default: return "按快捷鍵開始說話";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { catCache.Dispose(); cat.Dispose(); tips.Dispose(); }
            base.Dispose(disposing);
        }
    }

    public enum RecordingHudTarget { None, Cancel, Finish }

    // Geometry shared by painting and hit testing. Eyes are ellipses inscribed in these rectangles.
    public static class RecordingHudLayout
    {
        public static Rectangle CancelEye(Size clientSize)
        {
            int catHeight = CatHeight(clientSize);
            return new Rectangle((int)(clientSize.Width * .14), (int)(catHeight * .53), (int)(clientSize.Width * .18), (int)(catHeight * .29));
        }

        public static Rectangle FinishEye(Size clientSize)
        {
            int catHeight = CatHeight(clientSize);
            return new Rectangle((int)(clientSize.Width * .67), (int)(catHeight * .53), (int)(clientSize.Width * .19), (int)(catHeight * .29));
        }

        public static RecordingHudTarget HitTest(Point point, Size clientSize)
        {
            if (InsideEllipse(CancelEye(clientSize), point)) return RecordingHudTarget.Cancel;
            if (InsideEllipse(FinishEye(clientSize), point)) return RecordingHudTarget.Finish;
            return RecordingHudTarget.None;
        }

        // Maps decibels to a 0..1 meter fill; unknown or non-finite input yields 0.
        public static double LevelFraction(double decibels, bool isKnown)
        {
            if (!isKnown || Double.IsNaN(decibels) || Double.IsInfinity(decibels)) return 0;
            return Math.Max(0, Math.Min(1, (decibels + 60) / 60));
        }

        private static int CatHeight(Size clientSize)
        {
            return (int)(clientSize.Width * .4978);
        }

        private static bool InsideEllipse(Rectangle box, Point point)
        {
            double dx = (point.X - (box.Left + box.Width / 2.0)) / (box.Width / 2.0);
            double dy = (point.Y - (box.Top + box.Height / 2.0)) / (box.Height / 2.0);
            return dx * dx + dy * dy <= 1;
        }
    }
}
