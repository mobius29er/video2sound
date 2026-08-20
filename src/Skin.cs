using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VideoToSound
{
    /// <summary>
    /// Palette and custom-drawn controls. Everything here is presentation only --
    /// no control in this file knows anything about ffmpeg or conversion.
    /// </summary>
    public static class Skin
    {
        public static readonly Color Ink      = Color.FromArgb(16, 16, 18);
        public static readonly Color Panel    = Color.FromArgb(26, 26, 29);
        public static readonly Color Raised   = Color.FromArgb(36, 36, 40);
        public static readonly Color Line     = Color.FromArgb(58, 58, 64);
        public static readonly Color Text     = Color.FromArgb(238, 238, 238);
        public static readonly Color Dim      = Color.FromArgb(138, 138, 146);
        public static readonly Color Acid     = Color.FromArgb(198, 255, 0);
        public static readonly Color Pink     = Color.FromArgb(255, 46, 136);
        public static readonly Color Danger   = Color.FromArgb(255, 78, 78);

        public static Font Display(float size)
        {
            return new Font("Arial Black", size, FontStyle.Italic);
        }

        public static Font Heavy(float size)
        {
            return new Font("Arial Black", size, FontStyle.Regular);
        }

        public static Font Body(float size)
        {
            return new Font("Tahoma", size, FontStyle.Regular);
        }

        public static Font BodyBold(float size)
        {
            return new Font("Tahoma", size, FontStyle.Bold);
        }

        /// <summary>Ska checkerboard strip -- the whole look hangs off this.</summary>
        public static void DrawCheckerStrip(Graphics g, Rectangle area, int cell, Color a, Color b)
        {
            using (SolidBrush ba = new SolidBrush(a))
            using (SolidBrush bb = new SolidBrush(b))
            {
                int rows = (int)Math.Ceiling(area.Height / (double)cell);
                int cols = (int)Math.Ceiling(area.Width / (double)cell);
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        Rectangle cellRect = new Rectangle(
                            area.X + c * cell, area.Y + r * cell, cell, cell);
                        cellRect.Intersect(area);
                        if (cellRect.Width <= 0 || cellRect.Height <= 0) continue;
                        g.FillRectangle(((r + c) % 2 == 0) ? ba : bb, cellRect);
                    }
                }
            }
        }
    }

    /// <summary>Chunky flat button that inverts to a solid slab on hover.</summary>
    public class SkateButton : Button
    {
        private Color accent = Skin.Acid;
        private bool hot;

        public Color Accent
        {
            get { return accent; }
            set { accent = value; Restyle(); }
        }

        public SkateButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Skin.Heavy(8F);
            Cursor = Cursors.Hand;
        }

        private void Restyle() { Invalidate(); }

        protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hot = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);

            Color face   = Enabled ? (hot ? accent : Skin.Ink) : Skin.Panel;
            Color border = Enabled ? accent : Skin.Line;
            Color label  = Enabled ? (hot ? Skin.Ink : accent) : Skin.Dim;

            using (SolidBrush b = new SolidBrush(face)) g.FillRectangle(b, r);
            using (Pen p = new Pen(border, 2f)) g.DrawRectangle(p, 1, 1, Width - 3, Height - 3);

            TextRenderer.DrawText(g, (Text == null ? "" : Text.ToUpperInvariant()), Font, r, label,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>Sticker-style toggle chip used for the format checklist.</summary>
    public class SkateChip : CheckBox
    {
        private bool hot;
        public Color Accent = Skin.Acid;

        public SkateChip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Skin.Heavy(8.5F);
            Cursor = Cursors.Hand;
            Appearance = Appearance.Button;
        }

        protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hot = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);

            Color face, label, border;
            if (!Enabled)          { face = Skin.Panel; label = Skin.Dim;   border = Skin.Line; }
            else if (Checked)      { face = Accent;     label = Skin.Ink;   border = Accent; }
            else if (hot)          { face = Skin.Raised; label = Accent;    border = Accent; }
            else                   { face = Skin.Ink;   label = Skin.Dim;   border = Skin.Line; }

            using (SolidBrush b = new SolidBrush(face)) g.FillRectangle(b, r);
            using (Pen p = new Pen(border, 2f)) g.DrawRectangle(p, 1, 1, Width - 3, Height - 3);

            TextRenderer.DrawText(g, Text, Font, r, label,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }

    /// <summary>Same chip behaviour, radio semantics.</summary>
    public class SkateRadio : RadioButton
    {
        private bool hot;
        public Color Accent = Skin.Pink;

        public SkateRadio()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Skin.BodyBold(8.25F);
            Cursor = Cursors.Hand;
            Appearance = Appearance.Button;
            TextAlign = ContentAlignment.MiddleLeft;
        }

        protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hot = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);

            Color face   = Checked ? Skin.Raised : Skin.Ink;
            Color border = Checked ? Accent : (hot ? Accent : Skin.Line);
            Color label  = Checked ? Skin.Text : (Enabled ? Skin.Dim : Skin.Line);

            using (SolidBrush b = new SolidBrush(face)) g.FillRectangle(b, r);
            using (Pen p = new Pen(border, 2f)) g.DrawRectangle(p, 1, 1, Width - 3, Height - 3);

            // marker block on the left
            Rectangle dot = new Rectangle(9, Height / 2 - 5, 10, 10);
            using (SolidBrush b = new SolidBrush(Checked ? Accent : Skin.Line)) g.FillRectangle(b, dot);

            Rectangle textRect = new Rectangle(26, 0, Width - 30, Height);
            TextRenderer.DrawText(g, Text, Font, textRect, label,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>Panel with a stuck-on label tab across its top edge.</summary>
    public class SkateGroup : Panel
    {
        public string Title = "";
        public Color Accent = Skin.Acid;
        public const int HeaderHeight = 26;

        public SkateGroup()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Skin.Panel;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(Skin.Panel)) g.FillRectangle(b, ClientRectangle);

            Rectangle tab = new Rectangle(0, 0, Width, HeaderHeight);
            using (SolidBrush b = new SolidBrush(Accent)) g.FillRectangle(b, tab);

            TextRenderer.DrawText(g, Title.ToUpperInvariant(), Skin.Heavy(8F),
                new Rectangle(10, 0, Width - 14, HeaderHeight), Skin.Ink,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            using (Pen p = new Pen(Skin.Line, 2f))
                g.DrawRectangle(p, 1, 1, Width - 3, Height - 3);
        }
    }

    /// <summary>Progress bar with diagonal hazard stripes.</summary>
    public class SkateProgress : Control
    {
        private int val, max = 100;

        public int Maximum
        {
            get { return max; }
            set { max = Math.Max(1, value); Invalidate(); }
        }

        public int Value
        {
            get { return val; }
            set { val = Math.Max(0, Math.Min(value, max)); Invalidate(); }
        }

        public Color Accent = Skin.Acid;

        public SkateProgress()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle r = ClientRectangle;

            using (SolidBrush b = new SolidBrush(Skin.Ink)) g.FillRectangle(b, r);

            int w = (int)((r.Width - 4) * (val / (double)max));
            if (w > 0)
            {
                Rectangle fill = new Rectangle(2, 2, w, r.Height - 4);
                using (HatchBrush hb = new HatchBrush(HatchStyle.WideUpwardDiagonal,
                                                      Skin.Ink, Accent))
                    g.FillRectangle(hb, fill);
            }

            using (Pen p = new Pen(Skin.Line, 2f))
                g.DrawRectangle(p, 1, 1, r.Width - 3, r.Height - 3);
        }
    }

    /// <summary>Title bar: checkerboard strip plus the wordmark.</summary>
    public class SkateHeader : Panel
    {
        /// <summary>Square icon drawn to the left of the drawn wordmark.</summary>
        public Image Mark;
        /// <summary>Full logo image. When set it replaces both mark and drawn type.</summary>
        public Image Wordmark;
        public string Tagline = "RIP THE AUDIO OUT OF ANYTHING";

        public SkateHeader()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Skin.Ink;
            Height = 84;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (SolidBrush b = new SolidBrush(Skin.Ink)) g.FillRectangle(b, ClientRectangle);

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            int x = 14;

            if (Wordmark != null)
            {
                DrawScaled(g, Wordmark, ref x, 58);
            }
            else
            {
                if (Mark != null) DrawScaled(g, Mark, ref x, 36);

                Font f = Skin.Display(20F);
                x = DrawRun(g, "video", f, Skin.Text, x, 6);
                x = DrawRun(g, "2",     f, Skin.Pink, x, 6);
                x = DrawRun(g, "sound", f, Skin.Text, x, 6);
                f.Dispose();
            }

            using (Font tf = Skin.Heavy(7F))
            {
                Size sz = TextRenderer.MeasureText(g, Tagline, tf);
                TextRenderer.DrawText(g, Tagline, tf,
                    new Point(Width - sz.Width - 16, (Height - 8 - sz.Height) / 2), Skin.Acid);
            }

            Rectangle strip = new Rectangle(0, Height - 8, Width, 8);
            Skin.DrawCheckerStrip(g, strip, 8, Skin.Acid, Skin.Ink);
        }

        private void DrawScaled(Graphics g, Image img, ref int x, int targetHeight)
        {
            int w = (int)Math.Round(img.Width * (targetHeight / (double)img.Height));
            int y = (Height - 8 - targetHeight) / 2;
            g.DrawImage(img, new Rectangle(x, y, w, targetHeight));
            x += w + 10;
        }

        private int DrawRun(Graphics g, string s, Font f, Color c, int x, int y)
        {
            TextRenderer.DrawText(g, s, f, new Point(x, y), c, TextFormatFlags.NoPadding);
            return x + TextRenderer.MeasureText(g, s, f, Size.Empty, TextFormatFlags.NoPadding).Width;
        }
    }

    /// <summary>
    /// ComboBox is a native control, so the drop-down button keeps its system
    /// look no matter what colours we set. Paint over it after the fact.
    /// </summary>
    public class SkateCombo : ComboBox
    {
        private const int WM_PAINT = 0x000F;
        private const int ButtonWidth = 18;

        public SkateCombo()
        {
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 18;
            BackColor = Skin.Ink;
            ForeColor = Skin.Text;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg != WM_PAINT) return;

            using (Graphics g = Graphics.FromHwnd(Handle))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle btn = new Rectangle(Width - ButtonWidth - 1, 1, ButtonWidth, Height - 2);
                using (SolidBrush b = new SolidBrush(Enabled ? Skin.Raised : Skin.Panel))
                    g.FillRectangle(b, btn);

                Color arrow = Enabled ? Skin.Acid : Skin.Line;
                int cx = btn.Left + btn.Width / 2;
                int cy = btn.Top + btn.Height / 2;
                Point[] tri = new Point[]
                {
                    new Point(cx - 4, cy - 2),
                    new Point(cx + 4, cy - 2),
                    new Point(cx,     cy + 3)
                };
                using (SolidBrush b = new SolidBrush(arrow)) g.FillPolygon(b, tri);

                using (Pen p = new Pen(Skin.Line, 1f))
                    g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            }
        }
    }

    /// <summary>Sticker + caption shown over the file list while it is empty.</summary>
    public class EmptyState : Control
    {
        public Image Sticker;
        public string Caption = "DRAG VIDEO FILES HERE";

        public EmptyState()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Skin.Panel;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            using (SolidBrush b = new SolidBrush(Skin.Panel)) g.FillRectangle(b, ClientRectangle);

            int captionHeight = 26;
            int y = 0;

            if (Sticker != null)
            {
                int h = Math.Max(0, Math.Min(Sticker.Height, Height - captionHeight - 12));
                if (h > 0)
                {
                    int w = (int)Math.Round(Sticker.Width * (h / (double)Sticker.Height));
                    g.DrawImage(Sticker, new Rectangle((Width - w) / 2, 0, w, h));
                    y = h + 12;
                }
            }

            using (Font f = Skin.Heavy(9.5F))
                TextRenderer.DrawText(g, Caption, f,
                    new Rectangle(0, y, Width, captionHeight), Skin.Line,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.SingleLine);
        }
    }
}
