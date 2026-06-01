using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace EX_1_ChatAI_Winform;

/// <summary>Hosts chat UI in WebView2; posts JSON commands to <c>WebChat/chat.js</c>.</summary>
public sealed class ChatWebBridge
{
    private readonly WebView2 _view;
    private CoreWebView2? _core;

    public ChatWebBridge(WebView2 view) => _view = view;

    public async Task InitializeAsync()
    {
        await _view.EnsureCoreWebView2Async(null).ConfigureAwait(true);
        _core = _view.CoreWebView2;
        _core.Settings.AreBrowserAcceleratorKeysEnabled = false;

        var dir = Path.Combine(AppContext.BaseDirectory, "WebChat");
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Missing WebChat folder: {dir}");

        _core.SetVirtualHostNameToFolderMapping(
            "app.chat",
            dir,
            CoreWebView2HostResourceAccessKind.Allow);

        var tcs = new TaskCompletionSource<bool>();
        void OnNavCompleted(object? _, CoreWebView2NavigationCompletedEventArgs e)
        {
            _core!.NavigationCompleted -= OnNavCompleted;
            if (!e.IsSuccess)
                tcs.TrySetException(new InvalidOperationException($"WebView navigation failed: {e.WebErrorStatus}"));
            else
                tcs.TrySetResult(true);
        }

        _core.NavigationCompleted += OnNavCompleted;
        _core.Navigate("https://app.chat/index.html");
        await tcs.Task.ConfigureAwait(true);
    }

    private void Post(object payload)
    {
        if (_core is null) return;
        var json = JsonSerializer.Serialize(payload);
        _core.PostWebMessageAsJson(json);
    }

    public void Clear() => Post(new { t = "clear" });

    public void AppendUser(string text) => Post(new { t = "user", text });

    public void StartAiTyping(string messageId) => Post(new { t = "aiTyping", id = messageId });

    public void SetAiText(string messageId, string text, bool error) =>
        Post(new { t = "aiSet", id = messageId, text, error });
}
