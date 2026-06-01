using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Text;
using Microsoft.Web.WebView2.WinForms;

namespace EX_1_ChatAI_Winform
{
    public partial class Form1 : Form
    {
        private static class Palette
        {
            public static readonly Color AppBg = Color.FromArgb(238, 243, 248);
            public static readonly Color Card = Color.FromArgb(255, 255, 255);
            public static readonly Color InputBg = Color.FromArgb(255, 255, 255);
            public static readonly Color ChatBg = Color.FromArgb(246, 248, 251);
            public static readonly Color BorderPrimary = Color.FromArgb(136, 151, 170);
            public static readonly Color BorderInner = Color.FromArgb(191, 201, 214);
            public static readonly Color CardRim = Color.FromArgb(114, 128, 145);

            public static readonly Color TextStrong = Color.FromArgb(31, 42, 55);
            public static readonly Color TextSoft = Color.FromArgb(82, 96, 109);

            public static readonly Color SoftSecondary = Color.FromArgb(231, 237, 244);
            public static readonly Color HeaderText = Color.FromArgb(74, 90, 106);

            public static readonly Color SendBg = Color.FromArgb(95, 143, 184);
            public static readonly Color SendHover = Color.FromArgb(79, 126, 167);

            public static readonly Color ClearBtnBack = Color.FromArgb(248, 250, 252);
            public static readonly Color ClearBtnBorder = Color.FromArgb(138, 151, 166);
            public static readonly Color ClearBtnText = Color.FromArgb(74, 85, 99);

            public static readonly Color SecondaryFill = Color.FromArgb(231, 237, 244);

            // Chat bubbles: colors only (geometry unchanged)
            public static readonly Color UserBubble = Color.FromArgb(199, 230, 225);
            public static readonly Color AiBubble = Color.FromArgb(221, 227, 242);
            public static readonly Color ErrorBubble = Color.FromArgb(253, 236, 236);
        }

        private enum ProviderKind
        {
            OpenAI = 1,
            Gemini = 2
        }

        private sealed record ModelChoice(string DisplayName, ProviderKind Provider, string ModelId)
        {
            public override string ToString() => DisplayName;
        }

        private Gemini_SDK? _gemini;
        private OpenAI_SDK_Response? _openAi;
        private readonly DateTimeTools _dateTimeTools = new();

        private readonly WebView2 _chatWebView = new();
        private ChatWebBridge? _chatBridge;

        private static readonly ModelChoice[] _modelOptions =
        [
            // OpenAI models
            new ModelChoice("OpenAI — gpt-5-mini", ProviderKind.OpenAI, "gpt-5-mini"),          // Reasoning model
            new ModelChoice("OpenAI — gpt-4.1-mini", ProviderKind.OpenAI, "gpt-4.1-mini"),      // Classic fast model
            new ModelChoice("OpenAI — gpt-4o", ProviderKind.OpenAI, "gpt-4o"),                  // Multimodal / fast

            // Gemini models
            new ModelChoice("Gemini — gemini-3 (preview)", ProviderKind.Gemini, "gemini-3-flash-preview"),
            new ModelChoice("Gemini — gemini-2.5", ProviderKind.Gemini, "gemini-2.5-flash")
        ];

        private ModelChoice? _selectedModel;

        private RoundedPanel? _hdrModel;
        private RoundedPanel? _hdrSystem;
        private TableLayoutPanel? _tlpModelInner;
        private TableLayoutPanel? _tlpSpInner;
        private Panel? _selectedModelRowPanel;
        private bool _topCardsRightEdgeHooked;
        private readonly string _uiDiagLogPath = Path.Combine(AppContext.BaseDirectory, "ui-layout-debug.log");
        private readonly Stopwatch _uiDiagClock = Stopwatch.StartNew();
        private readonly object _uiDiagLock = new();

        public Form1()
        {
            InitializeComponent();
            InitUiDiagnostics();


            BuildEmbeddedTopHeaders();

            Font = new Font("Segoe UI", 10.5F);
            BackColor = Palette.AppBg;
            EnableDoubleBuffer(this);

            pnlChatScroll.AutoScroll = false;
            _chatWebView.Dock = DockStyle.Fill;
            _chatWebView.Margin = Padding.Empty;
            _chatWebView.TabStop = true;
            pnlChatScroll.Controls.Add(_chatWebView);
            _chatWebView.SendToBack();
            pnlAiOverlay.BringToFront();
            _chatBridge = new ChatWebBridge(_chatWebView);
            Load += (_, _) => _ = InitChatWebAsync();

            ApplyModernTheme();
            WireRoundedChrome();
            EnableDoubleBuffer(pnlChatScroll);

            // Keep WhatsApp-style chat area without split headers.
            lblUserHeader.Visible = false;
            lblAiHeader.Visible = false;
            if (tlpChat.RowStyles.Count > 0)
            {
                tlpChat.RowStyles[0].Height = 0;
                tlpChat.RowStyles[0].SizeType = SizeType.Absolute;
            }

            btnSend.Click += async (_, _) => await SendAsync();
            btnClear.Click += (_, _) => ClearAll();

            txtUserInput.KeyDown += async (_, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    await SendAsync();
                }
            };

            LoadModels();
        }

        private async Task InitChatWebAsync()
        {
            if (_chatBridge is null) return;
            try
            {
                await _chatBridge.InitializeAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "לא ניתן לטעון את תצוגת הצ'אט (WebView2)." + Environment.NewLine + ex.Message,
                    "WebView2",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static RoundedPanel CreateTextOnlyHeaderPill(string hebrewText)
        {
            var font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            var textSize = TextRenderer.MeasureText(hebrewText, font);
            var pill = new RoundedPanel
            {
                CornerRadius = 10,
                BorderThickness = 1,
                Padding = new Padding(12, 5, 12, 5),
                Margin = new Padding(0),
                Size = new Size(textSize.Width + 26, textSize.Height + 12),
                RightToLeft = RightToLeft.No   // prevent RTL inheritance from form mirroring label Dock
            };
            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                Text = hebrewText,
                Font = font,
                TextAlign = ContentAlignment.MiddleRight,
                RightToLeft = RightToLeft.No,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            pill.Controls.Add(lbl);
            return pill;
        }

        private static void StyleHeaderPill(RoundedPanel? pill)
        {
            if (pill == null || pill.Controls.Count == 0)
            {
                return;
            }

            pill.BackColor = Palette.SoftSecondary;
            pill.BorderColor = Palette.BorderInner;
            if (pill.Controls[0] is Label l)
            {
                l.ForeColor = Palette.HeaderText;
                l.BackColor = Palette.SoftSecondary;
            }
        }


        private void BuildEmbeddedTopHeaders()
        {
            if (!_topCardsRightEdgeHooked)
            {
                tlpTop.Paint += (_, e) =>
                {
                    if (pnlTopCardRight.Height <= 0) return;
                    var xLeft = pnlTopCardRight.Left;
                    var xRight = pnlTopCardRight.Right - 1;
                    using var pen = new Pen(Palette.CardRim, 1f);
                    var y1 = pnlTopCardRight.Top;
                    var y2 = pnlTopCardRight.Top + pnlTopCardRight.Height - 1;
                    if (xLeft >= 0)
                        e.Graphics.DrawLine(pen, xLeft, y1, xLeft, y2);
                    if (xRight >= 0 && xRight != xLeft)
                        e.Graphics.DrawLine(pen, xRight, y1, xRight, y2);
                };
                tlpTop.Resize += (_, _) => tlpTop.Invalidate();
                pnlTopCardRight.Resize += (_, _) => tlpTop.Invalidate();
                pnlTopCardRight.LocationChanged += (_, _) => tlpTop.Invalidate();
                _topCardsRightEdgeHooked = true;
            }

            // Keep physical left/right stable inside this card even when the form is RTL-mirrored.
            pnlTopCardRight.RightToLeft = RightToLeft.No;
            pnlTopCardRight.Controls.Clear();
            pnlTopCardLeft.Controls.Clear();

            _tlpModelInner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(8, 0, 8, 8),
                BackColor = Color.White,
                Margin = new Padding(0)
            };
            var tlpModel = _tlpModelInner;
            tlpModel.RightToLeft = RightToLeft.No;
            tlpModel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            tlpModel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var hostModel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 54,
                BackColor = Color.White,
                Padding = new Padding(8, 6, 8, 6)
            };
            _hdrModel = CreateTextOnlyHeaderPill("מודלים");
            hostModel.Controls.Add(_hdrModel);
            hostModel.Layout += (_, _) =>
            {
                if (_hdrModel == null) return;
                _hdrModel.Left = Math.Max(0, hostModel.ClientSize.Width - hostModel.Padding.Right - _hdrModel.Width);
                _hdrModel.Top = Math.Max(0, (hostModel.ClientSize.Height - _hdrModel.Height) / 2);
            };

            var pnlModelsListShell = new Panel
            {
                Dock = DockStyle.Top,
                Height = _modelOptions.Length * 34 + 12,
                BackColor = Palette.CardRim,
                Padding = new Padding(0, 0, 2, 0),
                Margin = new Padding(8, 8, 8, 8),
                BorderStyle = BorderStyle.None,
                RightToLeft = RightToLeft.No,
            };
            var pnlModelsListContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Palette.AppBg,
                Padding = new Padding(0, 5, 0, 5),
                Margin = Padding.Empty,
                RightToLeft = RightToLeft.No
            };
            pnlModelsListContent.Paint += (_, e) =>
            {
                var rc = pnlModelsListContent.ClientRectangle;
                using var pen = new Pen(Palette.BorderInner, 1f);
                e.Graphics.DrawRectangle(pen, new Rectangle(rc.X, rc.Y, rc.Width - 1, rc.Height - 1));
            };

            for (var i = _modelOptions.Length - 1; i >= 0; i--)
            {
                var capturedChoice = _modelOptions[i];
                var isDefault = (i == 0);
                var row = new Panel
                {
                    Height = 34,
                    Dock = DockStyle.Top,
                    BackColor = isDefault ? Palette.SoftSecondary : Color.White,
                    Cursor = Cursors.Hand,
                    Padding = new Padding(0)
                };
                if (isDefault)
                    _selectedModelRowPanel = row;

                var capturedDisplayName = capturedChoice.DisplayName;
                row.Paint += (_, e) =>
                {
                    var r = (Panel)row;
                    var textRect = new Rectangle(10, 0, r.Width - 20, r.Height);
                    TextRenderer.DrawText(e.Graphics, capturedDisplayName, r.Font, textRect,
                        Palette.TextStrong,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                        TextFormatFlags.PreserveGraphicsClipping);
                };

                var capturedRow = row;
                capturedRow.Click += (_, _) =>
                {
                    _selectedModel = capturedChoice;
                    if (_selectedModelRowPanel != null)
                        _selectedModelRowPanel.BackColor = Color.White;
                    _selectedModelRowPanel = capturedRow;
                    capturedRow.BackColor = Palette.SoftSecondary;
                    RecreateClientAndClearHistory();
                };
                capturedRow.MouseEnter += (_, _) => { if (capturedRow != _selectedModelRowPanel) capturedRow.BackColor = Color.FromArgb(245, 247, 251); };
                capturedRow.MouseLeave += (_, _) => { if (capturedRow != _selectedModelRowPanel) capturedRow.BackColor = Color.White; };

                pnlModelsListContent.Controls.Add(capturedRow);
            }

            pnlModelsListShell.Controls.Add(pnlModelsListContent);

            tlpModel.Controls.Add(hostModel, 0, 0);
            tlpModel.Controls.Add(pnlModelsListShell, 0, 1);
            pnlTopCardRight.Controls.Add(tlpModel);

            _tlpSpInner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(8, 0, 8, 8),
                BackColor = Color.White,
                Margin = new Padding(0)
            };
            var tlpSp = _tlpSpInner;
            tlpSp.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            tlpSp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var hostSp = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 54,
                BackColor = Color.White,
                Padding = new Padding(8, 6, 8, 6)
            };
            _hdrSystem = CreateTextOnlyHeaderPill("הנחיות מערכת");
            hostSp.Controls.Add(_hdrSystem);
            hostSp.Layout += (_, _) =>
            {
                if (_hdrSystem == null) return;
                _hdrSystem.Left = Math.Max(0, hostSp.ClientSize.Width - hostSp.Padding.Right - _hdrSystem.Width);
                _hdrSystem.Top = Math.Max(0, (hostSp.ClientSize.Height - _hdrSystem.Height) / 2);
            };

            var pnlSystemPromptShell = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(8),
                Margin = new Padding(8, 12, 8, 8),
                BorderStyle = BorderStyle.FixedSingle,
            };
            txtSystemPrompt.Dock = DockStyle.Fill;
            pnlSystemPromptShell.Controls.Add(txtSystemPrompt);

            tlpSp.Controls.Add(hostSp, 0, 0);
            tlpSp.Controls.Add(pnlSystemPromptShell, 0, 1);
            pnlTopCardLeft.Controls.Add(tlpSp);

            hostModel.PerformLayout();
            hostSp.PerformLayout();
        }


        private void ApplyModernTheme()
        {
            tlpRoot.BackColor = Palette.AppBg;
            tlpTop.BackColor = Palette.AppBg;

            pnlBottomCard.BackColor = Palette.CardRim;
            tlpBottom.BackColor = Palette.Card;

            _tlpModelInner!.BackColor = Color.White;
            _tlpSpInner!.BackColor = Color.White;
            if (_tlpModelInner.GetControlFromPosition(0, 0) is Panel hModelHost)
            {
                hModelHost.BackColor = Color.White;
            }

            if (_tlpSpInner.GetControlFromPosition(0, 0) is Panel hSysHost)
            {
                hSysHost.BackColor = Color.White;
            }

            StyleHeaderPill(_hdrModel);
            StyleHeaderPill(_hdrSystem);

            txtSystemPrompt.BackColor = Color.White;
            txtSystemPrompt.ForeColor = Palette.TextStrong;

            // BackColor-as-border: the card's gray background shows as a 2px rim
            // around the white inner TLP. No drawing code needed → no paint-loop.
            pnlTopCardRight.BackColor = Palette.CardRim;
            pnlTopCardRight.Padding = new Padding(2);
            pnlTopCardRight.BorderStyle = BorderStyle.None;
            pnlTopCardLeft.BackColor = Palette.CardRim;
            pnlTopCardLeft.Padding = new Padding(2);
            pnlUserInputShell.BackColor = Color.White;

            // Chat: same BackColor-as-border approach
            pnlChatContainer.BackColor = Palette.CardRim;
            pnlChatContainer.Padding = new Padding(2);
            tlpChat.BackColor = Palette.ChatBg;
            pnlChatScroll.BackColor = Palette.ChatBg;

            lblUserHeader.BackColor = Palette.ChatBg;
            lblAiHeader.BackColor = Palette.ChatBg;
            lblUserHeader.ForeColor = Palette.TextStrong;
            lblAiHeader.ForeColor = Palette.TextStrong;

            txtUserInput.BackColor = Color.White;
            txtUserInput.ForeColor = Palette.TextStrong;

            StylePrimaryButton(btnSend);
            StyleSecondaryButton(btnClear);
        }

        private void WireRoundedChrome()
        {
            btnSend.MouseEnter += (_, _) =>
            {
                if (btnSend.Enabled)
                    btnSend.BackColor = Color.FromArgb(57, 103, 148);
            };
            btnSend.MouseLeave += (_, _) =>
            {
                btnSend.BackColor = Color.FromArgb(72, 118, 162);
            };

            btnClear.MouseEnter += (_, _) =>
            {
                if (btnClear.Enabled)
                    btnClear.BackColor = Color.FromArgb(210, 220, 232);
            };
            btnClear.MouseLeave += (_, _) =>
            {
                btnClear.BackColor = Color.FromArgb(231, 237, 244);
            };
        }

        private static void ApplyRoundedRegion(Control control, int radius)
        {
            void update()
            {
                if (control.Width <= 1 || control.Height <= 1)
                {
                    return;
                }

                var rect = control.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                using var path = CreateRoundRectPath(rect, radius);
                control.Region = new Region(path);
            }

            control.SizeChanged += (_, _) => update();
            update();
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

        private void StylePrimaryButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.FromArgb(72, 118, 162);
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void StyleSecondaryButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Palette.CardRim;
            btn.BackColor = Color.FromArgb(231, 237, 244);
            btn.ForeColor = Color.FromArgb(44, 58, 74);
            btn.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void LoadModels()
        {
            _selectedModel = _modelOptions[0];
        }

        private ModelChoice? SelectedModel => _selectedModel;

        private void RecreateClientAndClearHistory()
        {
            _gemini = null;
            _openAi = null;
            ClearAll();
        }

        private void ClearAll()
        {
            _chatBridge?.Clear();
            _gemini?.ClearHistory();
            _openAi?.ClearHistory();
        }

        private async Task SendAsync()
        {
            var model = SelectedModel;
            if (model is null) return;

            var userText = (txtUserInput.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(userText)) return;

            var systemPrompt = (txtSystemPrompt.Text ?? string.Empty).Trim();

            _chatBridge?.AppendUser(userText);
            var aiId = Guid.NewGuid().ToString("n");
            _chatBridge?.StartAiTyping(aiId);
            txtUserInput.Clear();

            SetUiBusy(true);
            try
            {
                await StreamModelToBubbleAsync(model, systemPrompt, userText, aiId).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _chatBridge?.SetAiText(aiId, ex.Message, error: true);
            }
            finally
            {
                SetUiBusy(false);
            }
        }

        private async Task StreamModelToBubbleAsync(ModelChoice model, string systemPrompt, string userText, string aiMessageId)
        {
            var sb = new StringBuilder();
            const int streamUiThrottleMs = 48;
            var lastUiUtc = DateTime.MinValue;
            var firstVisual = true;

            IAsyncEnumerable<string> stream = model.Provider switch
            {
                ProviderKind.OpenAI => GetOpenAiStream(model, systemPrompt, userText),
                ProviderKind.Gemini => GetGeminiStream(model, systemPrompt, userText),
                _ => throw new NotSupportedException("Unsupported model provider.")
            };

            await foreach (var chunk in stream.ConfigureAwait(true))
            {
                sb.Append(chunk);

                var now = DateTime.UtcNow;
                if (firstVisual || (now - lastUiUtc).TotalMilliseconds >= streamUiThrottleMs)
                {
                    firstVisual = false;
                    lastUiUtc = now;
                    _chatBridge?.SetAiText(aiMessageId, sb.ToString(), false);
                    await Task.Yield();
                }
            }

            _chatBridge?.SetAiText(aiMessageId, sb.ToString(), false);
        }

        private IAsyncEnumerable<string> GetOpenAiStream(ModelChoice model, string systemPrompt, string userText)
        {
            _openAi ??= new OpenAI_SDK_Response(model.ModelId, string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt, _dateTimeTools);
            return _openAi.CallStream(userText);
        }

        private IAsyncEnumerable<string> GetGeminiStream(ModelChoice model, string systemPrompt, string userText)
        {
            _gemini ??= new Gemini_SDK(model.ModelId, string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt, _dateTimeTools);
            return _gemini.CallStream(userText);
        }


        private void SetUiBusy(bool busy)
        {
            btnSend.Enabled = !busy;
            btnClear.Enabled = !busy;
            txtSystemPrompt.ReadOnly = busy;
            txtUserInput.ReadOnly = busy;
        }

        private void InitUiDiagnostics()
        {
            try
            {
                lock (_uiDiagLock)
                {
                    File.WriteAllText(_uiDiagLogPath, $"UI diagnostics started {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}{Environment.NewLine}");
                }
            }
            catch
            {
                // Diagnostics must never break UI flow.
            }
        }

        private void UiDiagLog(string evt, string details)
        {
            try
            {
                var line = $"{_uiDiagClock.ElapsedMilliseconds,7}ms [{evt}] {details}";
                lock (_uiDiagLock)
                {
                    File.AppendAllText(_uiDiagLogPath, line + Environment.NewLine);
                }
                Debug.WriteLine(line);
            }
            catch
            {
                // Diagnostics must never break UI flow.
            }
        }

        private static void EnableDoubleBuffer(Control control)
        {
            var prop = typeof(Control).GetProperty(
                "DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            prop?.SetValue(control, true);
        }
    }
}
