using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace EX_1_ChatAI_Winform;

public class RoundedPanel : Panel
{
    private int _cornerRadius = 14;
    private int _borderThickness = 1;
    private Color _borderColor = Color.FromArgb(225, 229, 239);

    [Category("Appearance")]
    [DefaultValue(14)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = Math.Max(0, value);
            _regionSize = Size.Empty; // force Region refresh on next size event
            ApplyRoundedRegion();
            Invalidate();
        }
    }

    [Category("Appearance")]
    [DefaultValue(1)]
    public int BorderThickness
    {
        get => _borderThickness;
        set
        {
            _borderThickness = Math.Max(0, value);
            Invalidate();
        }
    }

    [Category("Appearance")]
    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            Invalidate();
        }
    }

    public void ResetBorderColor() => BorderColor = Color.FromArgb(225, 229, 239);
    public bool ShouldSerializeBorderColor() => BorderColor != Color.FromArgb(225, 229, 239);

    // Track the size for which Region was last computed, to avoid
    // calling Region = new Region(...) inside OnPaint (which triggers
    // another Invalidate → infinite paint loop → visual artifacts).
    private Size _regionSize;

    public RoundedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyRoundedRegion();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyRoundedRegion();
    }

    private void ApplyRoundedRegion()
    {
        if (Width <= 1 || Height <= 1 || _regionSize == Size) return;
        _regionSize = Size;
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = CreateRoundRectPath(r, CornerRadius);
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (BorderThickness <= 0) return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        // Draw border fully INSIDE the Region so the full pen stroke is visible
        // and anti-aliasing works on both sides without being clipped at the edge.
        int inset = (BorderThickness + 1) / 2;
        var borderRect = new Rectangle(inset, inset,
            Width - inset * 2 - 1, Height - inset * 2 - 1);
        if (borderRect.Width <= 0 || borderRect.Height <= 0) return;

        int borderRadius = Math.Max(0, CornerRadius - inset);
        using var borderPath = CreateRoundRectPath(borderRect, borderRadius);
        using var pen = new Pen(BorderColor, BorderThickness);
        e.Graphics.DrawPath(pen, borderPath);
    }

    private static GraphicsPath CreateRoundRectPath(Rectangle rect, int radius)
    {
        var r = Math.Max(0, radius);
        var d = r * 2;
        var path = new GraphicsPath();

        if (r == 0)
        {
            path.AddRectangle(rect);
            path.CloseFigure();
            return path;
        }

        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

