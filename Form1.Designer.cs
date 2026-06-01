namespace EX_1_ChatAI_Winform
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel tlpRoot;
        private TableLayoutPanel tlpTop;
        private Panel pnlTopCardRight;
        private Panel pnlTopCardLeft;
        private TextBox txtSystemPrompt;
        private Panel pnlChatContainer;
        private TableLayoutPanel tlpChat;
        private Label lblUserHeader;
        private Label lblAiHeader;
        private Panel pnlChatScroll;
        private Panel pnlAiOverlay;
        private ProgressBar prgThinking;
        private Panel pnlBottomCard;
        private TableLayoutPanel tlpBottom;
        private Panel pnlUserInputShell;
        private TextBox txtUserInput;
        private Button btnSend;
        private Button btnClear;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            tlpRoot = new TableLayoutPanel();
            tlpTop = new TableLayoutPanel();
            pnlTopCardRight = new Panel();
            pnlTopCardLeft = new Panel();
            txtSystemPrompt = new TextBox();
            pnlChatContainer = new Panel();
            tlpChat = new TableLayoutPanel();
            lblUserHeader = new Label();
            lblAiHeader = new Label();
            pnlChatScroll = new Panel();
            pnlAiOverlay = new Panel();
            prgThinking = new ProgressBar();
            pnlBottomCard = new Panel();
            tlpBottom = new TableLayoutPanel();
            pnlUserInputShell = new Panel();
            txtUserInput = new TextBox();
            btnSend = new Button();
            btnClear = new Button();
            pnlAiOverlay.SuspendLayout();
            pnlChatScroll.SuspendLayout();
            tlpChat.SuspendLayout();
            pnlChatContainer.SuspendLayout();
            pnlTopCardRight.SuspendLayout();
            pnlTopCardLeft.SuspendLayout();
            pnlUserInputShell.SuspendLayout();
            tlpTop.SuspendLayout();
            pnlBottomCard.SuspendLayout();
            tlpBottom.SuspendLayout();
            tlpRoot.SuspendLayout();
            SuspendLayout();
            // 
            // tlpRoot
            // 
            tlpRoot.ColumnCount = 1;
            tlpRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRoot.Dock = DockStyle.Fill;
            tlpRoot.RowCount = 3;
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 265F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
            tlpRoot.Controls.Add(tlpTop, 0, 0);
            tlpRoot.Controls.Add(pnlChatContainer, 0, 1);
            tlpRoot.Controls.Add(pnlBottomCard, 0, 2);
            tlpRoot.Padding = new Padding(20, 16, 20, 16);
            tlpRoot.BackColor = Color.FromArgb(238, 243, 248);
            // 
            // tlpTop — שני כרטיסים נפרדים על רקע הטופס (ללא עטיפה כפולה)
            // 
            tlpTop.ColumnCount = 2;
            tlpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 295F));
            tlpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpTop.Dock = DockStyle.Fill;
            tlpTop.RowCount = 1;
            tlpTop.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpTop.Controls.Add(pnlTopCardRight, 0, 0);
            tlpTop.Controls.Add(pnlTopCardLeft, 1, 0);
            tlpTop.Padding = new Padding(0);
            tlpTop.BackColor = Color.FromArgb(238, 243, 248);
            // 
            // pnlTopCardRight
            // 
            pnlTopCardRight.Name = "pnlTopCardRight";
            pnlTopCardRight.BackColor = Color.FromArgb(255, 255, 255);
            pnlTopCardRight.Dock = DockStyle.Fill;
            pnlTopCardRight.Margin = new Padding(0, 0, 8, 0);
            pnlTopCardRight.MinimumSize = new Size(295, 0);
            // 
            // pnlTopCardLeft
            // 
            pnlTopCardLeft.Name = "pnlTopCardLeft";
            pnlTopCardLeft.BackColor = Color.FromArgb(255, 255, 255);
            pnlTopCardLeft.Dock = DockStyle.Fill;
            pnlTopCardLeft.Margin = new Padding(8, 0, 0, 0);
            pnlTopCardLeft.Padding = new Padding(2);
            // 
            // txtSystemPrompt
            //
            txtSystemPrompt.BorderStyle = BorderStyle.None;
            txtSystemPrompt.Multiline = true;
            txtSystemPrompt.ScrollBars = ScrollBars.None;
            txtSystemPrompt.RightToLeft = RightToLeft.Yes;
            //
            // pnlChatContainer
            // 
            pnlChatContainer.Name = "pnlChatContainer";
            pnlChatContainer.Dock = DockStyle.Fill;
            pnlChatContainer.Padding = new Padding(0);
            pnlChatContainer.BackColor = Color.FromArgb(255, 255, 255);
            pnlChatContainer.Controls.Add(tlpChat);
            pnlChatContainer.Margin = new Padding(0, 16, 0, 16);
            // 
            // tlpChat
            // 
            tlpChat.ColumnCount = 2;
            tlpChat.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpChat.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpChat.Dock = DockStyle.Fill;
            // Keep left/right columns visually stable even in RTL form.
            tlpChat.RightToLeft = RightToLeft.No;
            tlpChat.RowCount = 2;
            tlpChat.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            tlpChat.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpChat.Controls.Add(lblUserHeader, 0, 0);
            tlpChat.Controls.Add(lblAiHeader, 1, 0);
            tlpChat.Controls.Add(pnlChatScroll, 0, 1);
            tlpChat.SetColumnSpan(pnlChatScroll, 2);
            // 
            // lblUserHeader
            // 
            lblUserHeader.Dock = DockStyle.Fill;
            lblUserHeader.TextAlign = ContentAlignment.MiddleCenter;
            lblUserHeader.Text = "הודעות שלי";
            lblUserHeader.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            lblUserHeader.ForeColor = Color.FromArgb(55, 63, 89);
            lblUserHeader.BackColor = Color.FromArgb(246, 247, 251);
            lblUserHeader.Margin = new Padding(0, 0, 8, 10);
            lblUserHeader.Padding = new Padding(12, 6, 12, 6);
            lblUserHeader.Visible = false;
            // 
            // lblAiHeader
            // 
            lblAiHeader.Dock = DockStyle.Fill;
            lblAiHeader.TextAlign = ContentAlignment.MiddleCenter;
            lblAiHeader.Text = "תשובות הבינה";
            lblAiHeader.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            lblAiHeader.ForeColor = Color.FromArgb(55, 63, 89);
            lblAiHeader.BackColor = Color.FromArgb(246, 247, 251);
            lblAiHeader.Margin = new Padding(8, 0, 0, 10);
            lblAiHeader.Padding = new Padding(12, 6, 12, 6);
            lblAiHeader.Visible = false;
            // 
            // pnlChatScroll
            // 
            pnlChatScroll.Dock = DockStyle.Fill;
            pnlChatScroll.AutoScroll = false;
            pnlChatScroll.BackColor = Color.FromArgb(246, 248, 251);
            pnlChatScroll.RightToLeft = RightToLeft.No;
            pnlChatScroll.Controls.Add(pnlAiOverlay);
            // 
            // pnlAiOverlay
            // 
            pnlAiOverlay.Dock = DockStyle.None;
            pnlAiOverlay.Location = new Point(0, 0);
            pnlAiOverlay.Size = new Size(100, 100);
            pnlAiOverlay.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlAiOverlay.Visible = false;
            pnlAiOverlay.BackColor = Color.FromArgb(120, 246, 248, 251);
            pnlAiOverlay.Controls.Add(prgThinking);
            // 
            // prgThinking
            // 
            prgThinking.Style = ProgressBarStyle.Marquee;
            prgThinking.MarqueeAnimationSpeed = 30;
            prgThinking.Size = new Size(220, 16);
            prgThinking.Anchor = AnchorStyles.None;
            // location is updated dynamically on resize
            prgThinking.Location = new Point(0, 0);
            // 
            // pnlBottomCard
            // 
            pnlBottomCard.Name = "pnlBottomCard";
            pnlBottomCard.BackColor = Color.FromArgb(255, 255, 255);
            pnlBottomCard.Controls.Add(tlpBottom);
            pnlBottomCard.Dock = DockStyle.Fill;
            pnlBottomCard.Margin = new Padding(0);
            pnlBottomCard.Padding = new Padding(2);
            // 
            // tlpBottom
            // 
            tlpBottom.ColumnCount = 3;
            tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            tlpBottom.Dock = DockStyle.Fill;
            tlpBottom.RowCount = 1;
            tlpBottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpBottom.Controls.Add(pnlUserInputShell, 0, 0);
            tlpBottom.Controls.Add(btnSend, 1, 0);
            tlpBottom.Controls.Add(btnClear, 2, 0);
            tlpBottom.Padding = new Padding(10, 8, 10, 8);
            // 
            // pnlUserInputShell
            // 
            pnlUserInputShell.BackColor = Color.FromArgb(255, 255, 255);
            pnlUserInputShell.Controls.Add(txtUserInput);
            pnlUserInputShell.Dock = DockStyle.Fill;
            pnlUserInputShell.Margin = new Padding(0, 0, 12, 0);
            pnlUserInputShell.Padding = new Padding(11, 9, 11, 9);
            // 
            // txtUserInput
            // 
            txtUserInput.BorderStyle = BorderStyle.None;
            txtUserInput.Dock = DockStyle.Fill;
            txtUserInput.Multiline = true;
            txtUserInput.PlaceholderText = "כתבו כאן שאלה, בקשה או הודעה...";
            txtUserInput.RightToLeft = RightToLeft.Yes;
            txtUserInput.ScrollBars = ScrollBars.None;
            // 
            // btnSend
            // 
            btnSend.Dock = DockStyle.Fill;
            btnSend.Margin = new Padding(0, 0, 8, 0);
            btnSend.Text = "שלח";
            // 
            // btnClear
            // 
            btnClear.Dock = DockStyle.Fill;
            btnClear.Margin = new Padding(0);
            btnClear.Text = "נקה";
            //
            // Form1
            //
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 720);
            Controls.Add(tlpRoot);
            MinimumSize = new Size(900, 600);
            Name = "Form1";
            Text = "שיחה עם מודלים של בינה מלאכותית";
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            StartPosition = FormStartPosition.CenterScreen;
            pnlAiOverlay.ResumeLayout(false);
            pnlChatScroll.ResumeLayout(false);
            pnlChatScroll.PerformLayout();
            tlpChat.ResumeLayout(false);
            pnlChatContainer.ResumeLayout(false);
            pnlTopCardRight.ResumeLayout(false);
            pnlTopCardLeft.ResumeLayout(false);
            pnlTopCardLeft.PerformLayout();
            pnlUserInputShell.ResumeLayout(false);
            pnlUserInputShell.PerformLayout();
            pnlBottomCard.ResumeLayout(false);
            tlpTop.ResumeLayout(false);
            tlpTop.PerformLayout();
            tlpBottom.ResumeLayout(false);
            tlpBottom.PerformLayout();
            tlpRoot.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
    }
}
