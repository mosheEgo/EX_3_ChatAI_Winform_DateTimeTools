using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace EX_1_ChatAI_Winform;

public enum BubbleTailSide
{
    Left = 1,
    Right = 2
}

public sealed class SpeechBubble : UserControl
{
    private readonly record struct BubbleCoreGeometry(
        Rectangle Body,
        int Radius,
        int AttachTop,
        int AttachBottom,
        int TipY,
        int TailW);

    private Control? _content;
    private bool _stretchContent = true;

    public SpeechBubble()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
        FillColor = Color.FromArgb(240, 242, 250);
        BorderColor = Color.FromArgb(225, 229, 239);

        Padding = new Padding(14, 12, 14, 12);
        TailSide = BubbleTailSide.Left;
        CornerRadius = 18;
        BorderThickness = 1;
        TailWidth = 12;
        TailHeight = 10;
        TailOffsetY = 16;

        MinimumSize = new Size(80, 40);
    }

    private BubbleTailSide _tailSide = BubbleTailSide.Left;
    private int _cornerRadius = 18;

    [Category("Appearance")]
    [DefaultValue(typeof(BubbleTailSide), "Left")]
    public BubbleTailSide TailSide
    {
        get => _tailSide;
        set { _tailSide = value; Invalidate(); PerformLayout(); }
    }

    [Category("Appearance")]
    [DefaultValue(18)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = Math.Max(0, value); Invalidate(); }
    }

    [Category("Appearance")]
    [DefaultValue(1)]
    public int BorderThickness { get; set; } = 1;

    [Category("Appearance")]
    public Color BorderColor { get; set; }
    public void ResetBorderColor() => BorderColor = Color.FromArgb(225, 229, 239);
    public bool ShouldSerializeBorderColor() => BorderColor != Color.FromArgb(225, 229, 239);

    [Category("Appearance")]
    public Color FillColor { get; set; }
    public void ResetFillColor() => FillColor = Color.FromArgb(240, 242, 250);
    public bool ShouldSerializeFillColor() => FillColor != Color.FromArgb(240, 242, 250);

    [Category("Appearance")]
    [DefaultValue(12)]
    public int TailWidth { get; set; } = 12;

    [Category("Appearance")]
    [DefaultValue(10)]
    public int TailHeight { get; set; } = 10;

    [Category("Appearance")]
    [DefaultValue(16)]
    public int TailOffsetY { get; set; } = 16;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control? Content
    {
        get => _content;
        set
        {
            if (_content != null)
            {
                Controls.Remove(_content);
            }
            _content = value;
            if (_content != null)
            {
                _content.Margin = Padding.Empty;
                _content.Location = new Point(0, 0);
                Controls.Add(_content);
                _content.BringToFront();
            }
            PerformLayout();
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool StretchContent
    {
        get => _stretchContent;
        set
        {
            _stretchContent = value;
            PerformLayout();
            Invalidate();
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var extraW = Padding.Horizontal + TailWidth;
        var extraH = Padding.Vertical;

        var available = proposedSize;
        if (available.Width > 0)
        {
            available = new Size(Math.Max(0, available.Width - extraW), available.Height);
        }
        if (available.Height > 0)
        {
            available = new Size(available.Width, Math.Max(0, available.Height - extraH));
        }

        var contentPref = _content?.GetPreferredSize(available) ?? Size.Empty;
        var h = contentPref.Height + extraH;
        // Thinking bubbles are very short; bottom arcs + tail Bézier control/flare can extend slightly past
        // the strict text chrome height and get clipped by the parent row — add a small vertical slack.
        if (_content is ThinkingDots)
            h += 12;
        else if (_content is BubbleTextView)
            h += 6; // pen inset + bottom arc; GDI+ can still extend a few px past strict TextRenderer pref height
        return new Size(contentPref.Width + extraW, h);
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_content != null)
        {
            var leftInset = TailSide == BubbleTailSide.Left ? TailWidth : 0;
            var rightInset = TailSide == BubbleTailSide.Right ? TailWidth : 0;

            var rect = ClientRectangle;
            rect = Rectangle.FromLTRB(
                rect.Left + leftInset + Padding.Left,
                rect.Top + Padding.Top,
                rect.Right - rightInset - Padding.Right,
                rect.Bottom - Padding.Bottom
            );

            if (StretchContent)
            {
                _content.Bounds = rect;
            }
            else
            {
                var pref = _content.GetPreferredSize(rect.Size);
                var cw = Math.Min(rect.Width, pref.Width);
                var ch = Math.Min(rect.Height, pref.Height);
                _content.Bounds = new Rectangle(
                    rect.Left + (rect.Width - cw) / 2,
                    rect.Top + (rect.Height - ch) / 2,
                    cw,
                    ch
                );
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        // Inset by 1px shrank the outline vs anti-aliased corners and could read as a "flat" clipped edge.
        var rect = ClientRectangle;

        using var path = CreateBubbleOutlinePath(rect);
        path.FillMode = FillMode.Winding;
        using var fill = new SolidBrush(FillColor);
        e.Graphics.FillPath(fill, path);

        if (BorderThickness > 0)
        {
            using var pen = new Pen(BorderColor, BorderThickness);
            pen.Alignment = PenAlignment.Inset;
            pen.LineJoin = LineJoin.Round;
            // Round caps on a closed DrawPath add visible blobs at sharp joins (e.g. tail tip).
            pen.StartCap = LineCap.Flat;
            pen.EndCap = LineCap.Flat;
            e.Graphics.DrawPath(pen, path);
        }
    }

    private BubbleCoreGeometry GetBubbleCoreGeometry(Rectangle r)
    {
        var tailW = Math.Max(4, TailWidth);
        var tailH = Math.Max(4, TailHeight);

        var bodyLeft = r.Left + (TailSide == BubbleTailSide.Left ? tailW : 0);
        var bodyRight = r.Right - (TailSide == BubbleTailSide.Right ? tailW : 0);
        var body = Rectangle.FromLTRB(bodyLeft, r.Top, bodyRight, r.Bottom);

        var radius = Math.Max(0, CornerRadius);
        radius = Math.Min(radius, Math.Max(0, Math.Min(body.Width, body.Height) / 2));

        var sideWallTop = body.Top + Math.Max(6, radius);
        var sideWallBottom = body.Bottom - Math.Max(6, radius);

        int tipY;
        int attachTop, attachBottom;
        if (sideWallBottom > sideWallTop)
        {
            attachTop = Math.Clamp(body.Top + TailOffsetY,
                sideWallTop, Math.Max(sideWallTop, sideWallBottom - tailH));
            attachBottom = Math.Min(sideWallBottom, attachTop + tailH);
            tipY = (attachTop + attachBottom) / 2;
        }
        else
        {
            tipY = (body.Top + body.Bottom) / 2;
            var halfH = Math.Max(2, tailH / 2);
            attachTop = tipY - halfH;
            attachBottom = tipY + halfH;
        }

        return new BubbleCoreGeometry(body, radius, attachTop, attachBottom, tipY, tailW);
    }

    /// <summary>
    /// One closed outer contour for fill and stroke. Two separate figures (rounded body + tail) under
    /// FillMode.Winding can cancel fill near corners and produce flat vertical edges.
    /// </summary>
    private GraphicsPath CreateBubbleOutlinePath(Rectangle r)
    {
        var g = GetBubbleCoreGeometry(r);

        if (TailSide == BubbleTailSide.Right)
            return CreateRoundedOutlineWithSmoothTail(g.Body, g.Radius, g.AttachTop, g.AttachBottom, g.TailW, g.TipY);

        // Build left-tailed outline in screen space (avoids Matrix mirror + winding quirks on some GPUs).
        return CreateRoundedOutlineWithSmoothTailLeft(
            g.Body, g.Radius, g.AttachTop, g.AttachBottom, g.TailW, g.TipY);
    }

    /// <summary>Tail on body.Left; same vertex pattern as right-tailed outline with tail moved to the left wall.</summary>
    private static GraphicsPath CreateRoundedOutlineWithSmoothTailLeft(
        Rectangle b, int r, int attachTop, int attachBottom, int tailW, int tipY)
    {
        var path = new GraphicsPath();
        if (r <= 0)
        {
            path.StartFigure();
            path.AddRectangle(b);
            path.CloseFigure();
            return path;
        }

        var d = 2 * r;
        var yR = b.Top + r;
        var yBr = b.Bottom - r;

        path.StartFigure();
        path.AddArc(b.X, b.Y, d, d, 180, 90);
        path.AddArc(b.Right - d, b.Y, d, d, 270, 90);
        path.AddLine(b.Right, yR, b.Right, yBr);
        path.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
        path.AddArc(b.X, b.Bottom - d, d, d, 90, 90);

        if (attachTop > yR)
            path.AddLine(b.Left, yR, b.Left, attachTop);
        AppendSmoothTailCurves(path, b.Left, attachTop, attachBottom, tailW, tipY, -1f);
        if (attachBottom < yBr)
            path.AddLine(b.Left, attachBottom, b.Left, yBr);

        path.CloseFigure();
        return path;
    }

    /// <summary>Same tail curves without StartFigure/Close — continues an open outline from (wall, attachTop).</summary>
    private static void AppendSmoothTailCurves(
        GraphicsPath path, float wallX, int attachTop, int attachBottom, int tailW, int tipY, float dir)
    {
        var tipX = wallX + dir * (tailW - 1);
        var depth = Math.Max(3f, tailW * 0.92f);
        var gap = Math.Max(2, attachBottom - attachTop);
        var flare = Math.Clamp(gap * 0.34f, 2f, 8f);
        var pull = depth * 0.26f;

        path.AddBezier(
            wallX, attachTop,
            wallX + dir * pull, attachTop,
            tipX - dir * (depth * 0.22f), tipY - flare,
            tipX, tipY);
        path.AddBezier(
            tipX, tipY,
            tipX - dir * (depth * 0.22f), tipY + flare,
            wallX + dir * pull, attachBottom,
            wallX, attachBottom);
    }

    /// <summary>Clockwise body outline: straight right wall → smooth tail → rest of rounded rect (mirror for left-tailed bubbles).</summary>
    private static GraphicsPath CreateRoundedOutlineWithSmoothTail(
        Rectangle b, int r, int attachTop, int attachBottom, int tailW, int tipY)
    {
        var path = new GraphicsPath();
        if (r <= 0)
        {
            path.StartFigure();
            path.AddRectangle(b);
            path.CloseFigure();
            return path;
        }

        var d = 2 * r;
        var yR = b.Top + r;
        var yBr = b.Bottom - r;

        path.StartFigure();
        path.AddArc(b.X, b.Y, d, d, 180, 90);
        path.AddArc(b.Right - d, b.Y, d, d, 270, 90);

        if (attachTop > yR)
            path.AddLine(b.Right, yR, b.Right, attachTop);
        AppendSmoothTailCurves(path, b.Right, attachTop, attachBottom, tailW, tipY, 1f);
        if (attachBottom < yBr)
            path.AddLine(b.Right, attachBottom, b.Right, yBr);

        path.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
        path.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

