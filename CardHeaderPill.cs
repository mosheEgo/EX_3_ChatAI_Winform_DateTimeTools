using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace EX_1_ChatAI_Winform;

public enum HeaderIconKind
{
    SparkleLayers = 1,
    Sliders = 2
}

/// <summary>Small line-style icon (14–16px) for card headers.</summary>
public sealed class LineIconGlyph : Control
{
    private HeaderIconKind _kind = HeaderIconKind.SparkleLayers;

    public LineIconGlyph()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint,
            true);
        TabStop = false;
        // Opaque (matches pill); avoids GDI artifacts that look like unrelated screen content.
        BackColor = Color.FromArgb(227, 234, 242);
        Size = new Size(16, 16);
        Margin = new Padding(0);
    }

    [DefaultValue(HeaderIconKind.SparkleLayers)]
    public HeaderIconKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color GlyphColor { get; set; } = Color.FromArgb(74, 90, 106);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(GlyphColor, 1.35f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        var r = ClientRectangle;
        var cx = r.Left + r.Width / 2f;
        var cy = r.Top + r.Height / 2f;

        switch (_kind)
        {
            case HeaderIconKind.SparkleLayers:
                // Sparkle + subtle stack (layers hint)
                g.DrawLine(pen, cx - 5, cy - 1, cx - 2, cy + 1);
                g.DrawLine(pen, cx + 2, cy - 1, cx + 5, cy + 1);
                g.DrawLine(pen, cx - 1, cy - 4, cx + 1, cy - 1);
                g.DrawLine(pen, cx - 1, cy + 2, cx + 1, cy + 5);
                using (var penLight = new Pen(Color.FromArgb(Math.Min(255, GlyphColor.R + 35), Math.Min(255, GlyphColor.G + 35), Math.Min(255, GlyphColor.B + 35)), 1f))
                {
                    g.DrawLine(penLight, cx - 4, cy + 4, cx + 3, cy + 5);
                }
                break;
            case HeaderIconKind.Sliders:
                // Three horizontal sliders
                for (var i = 0; i < 3; i++)
                {
                    var y = cy - 4 + i * 4f;
                    g.DrawLine(pen, cx - 6, y, cx + 6, y);
                    var knobX = cx - 3 + (i % 2) * 4f;
                    g.DrawEllipse(pen, knobX - 1.5f, y - 1.5f, 3f, 3f);
                }
                break;
        }
    }
}

/// <summary>Pill header: [icon][text] for RTL cards (icon visually left of Hebrew text).</summary>
public sealed class CardHeaderPill : RoundedPanel
{
    private readonly TableLayoutPanel _tlp;
    private readonly LineIconGlyph _glyph;
    private readonly Label _caption;

    public CardHeaderPill(string hebrewCaption, HeaderIconKind iconKind)
    {
        CornerRadius = 10;
        BorderThickness = 1;
        Padding = new Padding(10, 6, 10, 6);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Margin = new Padding(0, 0, 0, 8);

        _glyph = new LineIconGlyph { Kind = iconKind, Margin = new Padding(0, 0, 6, 0) };
        _caption = new Label
        {
            AutoSize = true,
            Text = hebrewCaption,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };

        _tlp = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            RightToLeft = RightToLeft.Yes,
            Margin = new Padding(0)
        };
        _tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _tlp.Controls.Add(_caption, 0, 0);
        _tlp.Controls.Add(_glyph, 1, 0);

        Controls.Add(_tlp);
    }

    public void ApplyHeaderColors(Color pillBack, Color border, Color text, Color iconColor)
    {
        BackColor = pillBack;
        BorderColor = border;
        _caption.ForeColor = text;
        _caption.BackColor = pillBack;
        _tlp.BackColor = pillBack;
        _glyph.GlyphColor = iconColor;
        _glyph.BackColor = pillBack;
        _glyph.Invalidate();
        Invalidate();
    }
}
