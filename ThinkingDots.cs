using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace EX_1_ChatAI_Winform;

public sealed class ThinkingDots : Control
{
    private readonly System.Windows.Forms.Timer _timer;
    private int _phase;

    private int _dotDiameter = 6;
    private int _dotSpacing = 7;
    private Color _dotColor = Color.FromArgb(70, 80, 110);
    private bool _tailOnRight;

    [Category("Appearance")]
    [DefaultValue(6)]
    public int DotDiameter
    {
        get => _dotDiameter;
        set { _dotDiameter = Math.Max(2, value); Invalidate(); }
    }

    [Category("Appearance")]
    [DefaultValue(7)]
    public int DotSpacing
    {
        get => _dotSpacing;
        set { _dotSpacing = Math.Max(0, value); Invalidate(); }
    }

    [Category("Appearance")]
    public Color DotColor
    {
        get => _dotColor;
        set { _dotColor = value; Invalidate(); }
    }

    public void ResetDotColor() => DotColor = Color.FromArgb(70, 80, 110);
    public bool ShouldSerializeDotColor() => DotColor != Color.FromArgb(70, 80, 110);

    /// <summary>
    /// When true (AI bubble), cluster sits toward the tail on the right; when false (user), toward the left tail.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool TailOnRight
    {
        get => _tailOnRight;
        set
        {
            if (_tailOnRight == value) return;
            _tailOnRight = value;
            Invalidate();
        }
    }

    public ThinkingDots()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.SupportsTransparentBackColor,
            true
        );
        BackColor = Color.Transparent;
        var s = GetPreferredSize(Size.Empty);
        Size = s;

        _timer = new System.Windows.Forms.Timer { Interval = 95 };
        _timer.Tick += (_, _) =>
        {
            _phase = (_phase + 1) % 12;
            Invalidate();
        };
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        UpdateAnimationState();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateAnimationState();
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        if (DesignMode)
        {
            _timer.Stop();
            return;
        }

        if (Visible && IsHandleCreated)
        {
            if (!_timer.Enabled) _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var g = e.Graphics;
        var state = g.Save();
        try
        {
            // Anti-aliased ellipses can paint a few pixels outside bounds; clip so nothing leaks past the bubble fill.
            if (ClientRectangle.Width > 0 && ClientRectangle.Height > 0)
                g.SetClip(ClientRectangle);

        // Extra padding on the tail side (AI = right): otherwise the third dot + AA sits on the stroke / past the body.
        const int marginInner = 6;
        const int marginTailUser = 14;
        const int marginTailAi = 26;
        var marginL = _tailOnRight ? marginInner : marginTailUser;
        var marginR = _tailOnRight ? marginTailAi : marginInner;
        const int marginT = 4;
        const int marginB = 4;
        var innerW = Math.Max(1, Width - marginL - marginR);
        var innerH = Math.Max(1, Height - marginT - marginB);
        // Reserve more toward the tail so the cluster never crowds the seam (even when width is tight).
        var tailClusterReserve = _tailOnRight ? 18 : 8;
        var layoutW = Math.Max(10, innerW - tailClusterReserve);

        var d0 = Math.Max(2, DotDiameter);
        var s0 = Math.Max(2, DotSpacing);
        var totalW0 = (3 * d0) + (2 * s0);
        var d = d0;
        var s = s0;
        if (totalW0 > layoutW)
        {
            var scale = layoutW / (double)totalW0;
            d = Math.Max(2, (int)Math.Floor(d0 * scale));
            s = Math.Max(2, (int)Math.Floor(s0 * scale));
            var tw = (3 * d) + (2 * s);
            if (tw > layoutW && d > 2)
            {
                d--;
                tw = (3 * d) + (2 * s);
            }
            if (tw > layoutW && s > 2)
            {
                s--;
            }
        }

        var totalW = (3 * d) + (2 * s);
        var hardRight = Width - marginR - 3;
        int startX;
        if (_tailOnRight)
            startX = marginL + Math.Max(0, layoutW - totalW);
        else
            startX = marginL;

        if (startX < marginL)
            startX = marginL;
        if (startX + totalW > marginL + layoutW)
            startX = Math.Max(marginL, marginL + layoutW - totalW);
        // Hard clamp: layoutW math can still let totalW exceed when dots cannot scale further.
        if (startX + totalW > hardRight)
            startX = Math.Max(marginL, hardRight - totalW);

        var y = marginT + Math.Max(0, (innerH - d) / 2);
        var active = _phase / 4;
        var sub = _phase % 4;
        var pulseY = sub switch
        {
            0 => -1,
            1 => -3,
            2 => -2,
            _ => 0
        };

        for (var i = 0; i < 3; i++)
        {
            var isActive = i == active;
            var alpha = isActive ? 230 : 110;

            using var brush = new SolidBrush(Color.FromArgb(alpha, DotColor));
            var drawX = startX + i * (d + s);
            drawX = Math.Min(drawX, hardRight - d);
            var drawY = isActive ? y + pulseY : y;
            var maxY = marginT + innerH - d;
            drawY = Math.Max(marginT, Math.Min(maxY, drawY));
            g.FillEllipse(brush, drawX, drawY, d, d);
        }
        }
        finally
        {
            g.Restore(state);
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var d = DotDiameter;
        var s = DotSpacing;
        var w = (3 * d) + (2 * s) + 8;
        // Extra vertical slack for pulse animation + parent row rounding vs bubble outline bottom.
        var h = d + 14;
        return new Size(w, h);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }
}
