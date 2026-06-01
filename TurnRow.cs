namespace EX_1_ChatAI_Winform;

public sealed class TurnRow : UserControl
{
    public Panel UserHost { get; }
    public Panel AiHost { get; }

    public TurnRow()
    {
        AutoSize = false;
        Margin = new Padding(0);
        Padding = new Padding(6, 4, 6, 4);
        BackColor = Color.Transparent;

        UserHost = new Panel
        {
            Dock = DockStyle.Left,
            BackColor = Color.Transparent,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        AiHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        Controls.Add(AiHost);
        Controls.Add(UserHost);

        Height = 64;
    }

    public void Reflow(int width)
    {
        Width = width;
        var half = Math.Max(200, width / 2);
        UserHost.Width = half;

        PerformLayout();

        var contentHeight = Math.Max(UserHost.PreferredSize.Height, AiHost.PreferredSize.Height);
        Height = Math.Max(56, contentHeight + Padding.Vertical);
    }
}

