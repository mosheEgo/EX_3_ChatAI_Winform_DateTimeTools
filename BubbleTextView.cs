using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;

namespace EX_1_ChatAI_Winform;

public sealed class BubbleTextView : Control
{
    private int _maxTextWidth = 320;
    private string _bubbleText = string.Empty;
    private const int MinTextWidth = 36;
    private const int MeasureSlackX = 6;
    private const int MeasureSlackY = 4;

    /// <summary>GDI+ single-line RTL metrics (TextRenderer + tight proposed height inflated Width for Hebrew).</summary>
    private SizeF MeasureRtlOneLineGdi(string text, int maxLayoutWidthPx)
    {
        if (string.IsNullOrEmpty(text)) return SizeF.Empty;
        using var bmp = new Bitmap(1, 1);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        using var sf = RtlOneLineMeasureFormat();
        var w = Math.Max(1, maxLayoutWidthPx);
        return g.MeasureString(text, Font, w, sf);
    }

    private static StringFormat RtlOneLineMeasureFormat()
    {
        var sf = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
            FormatFlags =
                StringFormatFlags.DirectionRightToLeft
                | StringFormatFlags.NoWrap
        };
        return sf;
    }

    private static StringFormat RtlOneLineDrawCenteredFormat()
    {
        var sf = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags =
                StringFormatFlags.DirectionRightToLeft
                | StringFormatFlags.NoWrap
        };
        return sf;
    }

    [Browsable(false)]
    public bool IsSingleLine { get; private set; }

    public BubbleTextView()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw,
            true
        );

        ForeColor = Color.FromArgb(55, 63, 89);
        BackColor = Color.White;
        // LTR control + RTL text flags (TextRenderer). Do not use RightToLeft.Yes here — it mirrors the DC
        // and breaks alignment. Use Right|RightToLeft for Hebrew (same as multiline path).
        RightToLeft = RightToLeft.No;
        Margin = Padding.Empty;
        Padding = new Padding(6, 6, 6, 6);
        Size = new Size(64, Font.Height + 6);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string BubbleText
    {
        get => _bubbleText;
        set
        {
            var next = value ?? string.Empty;
            if (_bubbleText == next) return;
            _bubbleText = next;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int MaxTextWidth
    {
        get => _maxTextWidth;
        set => _maxTextWidth = Math.Max(MinTextWidth, value);
    }

    public bool RecalculateSize()
    {
        var pref = GetPreferredSize(new Size(MaxTextWidth, int.MaxValue));
        pref.Width = Math.Max(1, pref.Width);
        pref.Height = Math.Max(1, pref.Height);

        var changed = Size != pref;
        Size = pref;
        Invalidate();
        return changed;
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var widthCap = proposedSize.Width > 0 ? Math.Min(MaxTextWidth, proposedSize.Width) : MaxTextWidth;
        widthCap = Math.Max(MinTextWidth, widthCap);

        if (string.IsNullOrWhiteSpace(BubbleText))
        {
            IsSingleLine = true;
            return new Size(28, Math.Max(Font.Height + Padding.Vertical + 4, 22));
        }

        var single = MeasureTextBlock(BubbleText, int.MaxValue, singleLine: true);
        const TextFormatFlags drawLikeLine =
            TextFormatFlags.NoPrefix
            | TextFormatFlags.RightToLeft
            | TextFormatFlags.SingleLine
            | TextFormatFlags.TextBoxControl;
        var innerMax = Math.Max(MinTextWidth, widthCap - Padding.Horizontal - MeasureSlackX);
        // GDI+ single-line RTL width avoids TextRenderer/MeasureTextBlock width blow-up (strip-wide bubbles).
        var gdi1 = MeasureRtlOneLineGdi(BubbleText, 16_384);
        var bodyW = Math.Max(MinTextWidth, Math.Min(innerMax, (int)Math.Ceiling(gdi1.Width)));
        var singleW = bodyW + Padding.Horizontal + MeasureSlackX;
        var hasExplicitBreak = BubbleText.IndexOfAny(['\r', '\n']) >= 0;

        // WordBreak at a capped width can still be one visual line while singleW exceeded widthCap (slack / measure mismatch).
        if (!hasExplicitBreak)
        {
            var atCap = MeasureTextBlock(BubbleText, Math.Max(1, widthCap - Padding.Horizontal), singleLine: false);
            if ((int)Math.Ceiling(atCap.Height) <= Font.Height + MeasureSlackY + 10)
            {
                var wLine = Math.Max(MinTextWidth, Math.Min(widthCap, singleW));
                var looseH = TextRenderer.MeasureText(BubbleText, Font, new Size(int.MaxValue, int.MaxValue), drawLikeLine).Height;
                var lineH = Math.Max(Math.Max(single.Height, looseH), gdi1.Height);
                var singleH = (int)Math.Ceiling(lineH) + MeasureSlackY + Padding.Vertical;
                var h = Math.Max(Font.Height + Padding.Vertical + 2, singleH + 4);
                IsSingleLine = true;
                return new Size(wLine, h);
            }
        }

        int measuredWidth;
        if (!hasExplicitBreak && singleW <= widthCap)
        {
            measuredWidth = Math.Max(MinTextWidth, Math.Min(widthCap, singleW));
            // Do not run WordBreak measurement here: at tight widths it can report 2+ lines even though
            // the same string fits one line visually, which flipped IsSingleLine and blew up row height.
            // Height: NoPadding measure is shorter than DrawText without NoPadding — use loose line height too.
            var looseH = TextRenderer.MeasureText(BubbleText, Font, new Size(int.MaxValue, int.MaxValue), drawLikeLine).Height;
            var lineH = Math.Max(Math.Max(single.Height, looseH), gdi1.Height);
            var singleH = (int)Math.Ceiling(lineH) + MeasureSlackY + Padding.Vertical;
            var h = Math.Max(Font.Height + Padding.Vertical + 2, singleH + 4);
            IsSingleLine = true;
            return new Size(measuredWidth, h);
        }

        // Narrowest width that achieves minimum line height — aspect-ratio scoring often picked a *wide*
        // column because WordBreak at narrow widths inflated line count (short Hebrew looked like a full-width strip).
        var minWidth = MinTextWidth;
        const int step = 4;
        var innerPad = Padding.Horizontal;
        var minPixelH = int.MaxValue;
        for (var w = minWidth; w <= widthCap; w += step)
        {
            var h = (int)Math.Ceiling(MeasureTextBlock(BubbleText, w - innerPad, singleLine: false).Height);
            minPixelH = Math.Min(minPixelH, h);
        }

        measuredWidth = widthCap;
        for (var w = minWidth; w <= widthCap; w += step)
        {
            var h = (int)Math.Ceiling(MeasureTextBlock(BubbleText, w - innerPad, singleLine: false).Height);
            if (h == minPixelH)
                measuredWidth = Math.Min(measuredWidth, w);
        }

        measuredWidth = Math.Max(MinTextWidth, Math.Min(widthCap, measuredWidth));
        var wrapped = MeasureTextBlock(BubbleText, measuredWidth - Padding.Horizontal, singleLine: false);
        var wrapPixelH = (int)Math.Ceiling(wrapped.Height);
        // WordBreak at a narrow probe width can report 2+ lines even when the string fits one line visually.
        if (!hasExplicitBreak && wrapPixelH <= Font.Height + MeasureSlackY + 6)
        {
            measuredWidth = Math.Max(MinTextWidth, Math.Min(widthCap, singleW));
            var looseH = TextRenderer.MeasureText(BubbleText, Font, new Size(int.MaxValue, int.MaxValue), drawLikeLine).Height;
            var lineH = Math.Max(Math.Max(single.Height, looseH), gdi1.Height);
            var singleH = (int)Math.Ceiling(lineH) + MeasureSlackY + Padding.Vertical;
            var h = Math.Max(Font.Height + Padding.Vertical + 2, singleH + 4);
            IsSingleLine = true;
            return new Size(measuredWidth, h);
        }

        var wrapH = Math.Max(
            Font.Height + Padding.Vertical + 2,
            wrapPixelH + MeasureSlackY + Padding.Vertical + 4
        );
        IsSingleLine = false;
        return new Size(measuredWidth, wrapH);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        const int edgeGutter = 2;
        var rect = Rectangle.FromLTRB(
            Padding.Left + edgeGutter,
            Padding.Top + edgeGutter,
            Width - Padding.Right - edgeGutter,
            Height - Padding.Bottom - edgeGutter
        );
        if (rect.Width <= 0 || rect.Height <= 0 || string.IsNullOrEmpty(BubbleText))
            return;

        e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        const TextFormatFlags rtlLine =
            TextFormatFlags.NoPrefix
            | TextFormatFlags.RightToLeft
            | TextFormatFlags.SingleLine
            | TextFormatFlags.TextBoxControl;

        var hasBreak = BubbleText.IndexOfAny(['\r', '\n']) >= 0;
        var oneLineProbe = TextRenderer.MeasureText(e.Graphics, BubbleText, Font, new Size(int.MaxValue, int.MaxValue), rtlLine);
        var paintAsSingleLine = IsSingleLine || (!hasBreak && oneLineProbe.Width <= rect.Width + 6);

        DrawRtlCentered(e.Graphics, rect, paintAsSingleLine, hasBreak);
    }

    /// <summary>One visual line: <see cref="Graphics.DrawString"/> RTL + typographic centering. Multiline: TextRenderer.</summary>
    private void DrawRtlCentered(Graphics g, Rectangle rect, bool preferSinglePaint, bool hasExplicitNewline)
    {
        const TextFormatFlags measureBase =
            TextFormatFlags.NoPrefix | TextFormatFlags.RightToLeft | TextFormatFlags.TextBoxControl;

        var gdiProbe = MeasureRtlOneLineGdi(BubbleText, Math.Max(rect.Width, MinTextWidth));
        var fitsOneLineInRect = (int)Math.Ceiling(gdiProbe.Width) <= rect.Width + 8;
        var oneVisualLine = !hasExplicitNewline && (preferSinglePaint || fitsOneLineInRect);

        if (oneVisualLine)
        {
            // GDI+ centering matches Hebrew baseline better than TextRenderer+VerticalCenter for single-line RTL here.
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using var brush = new SolidBrush(ForeColor);
            using var sf = RtlOneLineDrawCenteredFormat();
            g.DrawString(BubbleText, Font, brush, rect, sf);
            return;
        }

        var wrapCol = Math.Max(MinTextWidth, Math.Min(rect.Width, MaxTextWidth));
        var flagsWrap = measureBase | TextFormatFlags.WordBreak;
        var sz = TextRenderer.MeasureText(g, BubbleText, Font, new Size(wrapCol, int.MaxValue), flagsWrap);
        var thM = Math.Min(rect.Height, Math.Max(1, sz.Height + 8));
        var yM = rect.Top + Math.Max(0, (rect.Height - thM) / 2);
        var boxM = new Rectangle(rect.Left, yM, rect.Width, thM);
        var drawMulti = measureBase
            | TextFormatFlags.HorizontalCenter
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.WordBreak
            | TextFormatFlags.NoClipping;
        TextRenderer.DrawText(g, BubbleText, Font, boxM, ForeColor, drawMulti);
    }

    private SizeF MeasureTextBlock(string text, int width, bool singleLine)
    {
        // No NoPadding: it shrinks measured height and contributes to bottom clipping of Hebrew descenders.
        var flags =
            TextFormatFlags.NoPrefix
            | TextFormatFlags.RightToLeft
            | TextFormatFlags.TextBoxControl;

        // Never cap single-line proposed height: RTL TextRenderer can return huge Width when height is tight.
        var proposed = singleLine
            ? new Size(int.MaxValue, int.MaxValue)
            : new Size(Math.Max(1, width), int.MaxValue);

        flags |= singleLine ? TextFormatFlags.SingleLine : TextFormatFlags.WordBreak;
        // Do not add DT_RIGHT here: for Hebrew it can shrink measured width vs DrawText, producing a too-narrow bubble.

        var s = TextRenderer.MeasureText(text, Font, proposed, flags);
        return new SizeF(s.Width, s.Height);
    }
}
