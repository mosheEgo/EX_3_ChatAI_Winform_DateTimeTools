# EX_1_ChatAI_Winform

WinForms chat application (.NET 10.0-windows) with a WhatsApp-style UI supporting multiple AI providers (OpenAI and Google Gemini) with streaming responses.

## Architecture

### Entry Point
- [Program.cs](Program.cs) — standard WinForms bootstrap (`Application.Run(new Form1())`)

### Main Form
- [Form1.cs](Form1.cs) — all UI logic: layout engine, scroll handling, message lifecycle, SDK dispatch
- [Form1.Designer.cs](Form1.Designer.cs) — generated designer code (do not edit by hand)

### AI Provider SDKs
| File | Provider | SDK |
|---|---|---|
| [OpenAI_SDK_Response.cs](OpenAI_SDK_Response.cs) | OpenAI | `OpenAI` 2.10.0 (`ResponsesClient`) |
| [Gemini_SDK.cs](Gemini_SDK.cs) | Gemini | `Google.GenAI` 1.6.1 |

Both expose `Call(string)` (non-streaming) and `CallStream(string)` (`IAsyncEnumerable<string>`), and `ClearHistory()`.

### Custom Controls
| File | Purpose |
|---|---|
| [SpeechBubble.cs](SpeechBubble.cs) | Owner-drawn rounded bubble with directional tail |
| [BubbleTextView.cs](BubbleTextView.cs) | RTL-aware text renderer inside a bubble; wraps/measures without triggering layout loops |
| [ThinkingDots.cs](ThinkingDots.cs) | Animated 3-dot indicator (WhatsApp-style) shown while AI or user is "thinking" |
| [RoundedPanel.cs](RoundedPanel.cs) | Panel with rounded corners and a drawn border |
| [CardHeaderPill.cs](CardHeaderPill.cs) | Pill-shaped label used as section header |
| [TurnRow.cs](TurnRow.cs) | (Reserved) row-level container for future use |

## Supported Models

### OpenAI (via `ResponsesClient`)
- `gpt-5-mini` — reasoning model (ReasoningOptions enabled, no Temperature/TopP)
- `gpt-4.1-mini` — fast classic model
- `gpt-4o` — multimodal / fast

### Gemini (via `Google.GenAI`)
- `gemini-3-flash-preview` — Gemini 3 preview
- `gemini-2.5-flash` — Gemini 2.5

Model selection is done via the sidebar list. Switching model clears conversation history and disposes the current SDK instance.

## Key Design Decisions

### Manual Scroll (no AutoScroll)
`pnlChatScroll` uses a manually managed `_chatHost` panel instead of WinForms AutoScroll. Reason: AutoScroll + `SuspendLayout` caused scrollbar fights and repaint flicker during streaming. Mouse-wheel is intercepted via `IMessageFilter` (`ChatWheelFilter`).

### Streaming Throttle
UI updates during streaming are throttled to **48 ms** (`streamUiThrottleMs`) to avoid freezing on long responses. `Task.Yield()` is used after each paint to let the WinForms message loop breathe.

### Bubble Layout
- **Authoritative UI contract** (thinking vs text, RTL, tail clamps, outline fill): see [BUBBLE_UI.md](BUBBLE_UI.md).
- Bubbles never use `AutoSize = true` during layout (causes "trails" during streaming relayout).
- `LayoutMessages()` does a single top-to-bottom pass computing each row's `Top`/`Height` manually.
- First overflow transition triggers a deferred `BeginInvoke` settle pass to fix clipped edges.

### RTL Support
`BubbleTextView` renders with `TextFormatFlags.Right | TextFormatFlags.RightToLeft`. The form uses `RightToLeft.Yes` on the text control so Hebrew text flows correctly.

### Draft Bubble (Live Typing Preview)
While the user types, a "thinking" bubble appears immediately in the chat. On send, it is converted to the final text bubble in place (no flicker from remove+add).

### Models Card Outer Border (RTL Notes)
- Scope: the missing line is the **outer right edge** of the top-right models card (`pnlTopCardRight`), not the inner models list box.
- In this screen, form-level `RightToLeftLayout` mirroring can make "physical right edge" fixes unreliable when applied inside nested child controls.
- Approaches that caused regressions or unreliable visuals: floating overlay panels, forced extra border widths, and mixed parent/child paint hooks.
- Keep fixes minimal and local: prefer a single stable render path for the outer card frame (same approach for both top cards), avoid stacking multiple border mechanisms.
- If testing visual tweaks, always run from a fresh process to avoid locked-output confusion (`EX_1_ChatAI_Winform.dll` lock during rebuild can hide latest UI changes).

## API Keys

Keys are loaded from environment variables via `DotNetEnv` from a `.env` file in the output directory:

```
OPENAI_API_KEY=sk-...
GEMINI_API_KEY=...        # or GOOGLE_API_KEY
```

The `.env` file is **not committed** (add to `.gitignore` if missing). Missing keys throw `InvalidOperationException` at the moment the first message is sent to that provider.

## Dependencies

```xml
<PackageReference Include="DotNetEnv"   Version="3.1.1" />
<PackageReference Include="Google.GenAI" Version="1.6.1" />
<PackageReference Include="OpenAI"       Version="2.10.0" />
```

## Build & Run

```bash
dotnet build
dotnet run
```

Target framework: `net10.0-windows`. Requires Windows (WinForms).

## Current Status (2025-04-25)

- Core chat loop (user → AI, with streaming) is fully functional for both providers.
- UI theme: blue-grey palette, WhatsApp-style bubbles, RTL Hebrew support.
- No persistence — conversation history lives in memory only, cleared on model switch or "Clear" button.
- No image/file attachment support yet.
- `TurnRow.cs` exists but is not wired up to the main layout.
- `CardHeaderPill.cs` is defined but the embedded pill creation in `Form1` is done inline via `CreateTextOnlyHeaderPill()`; the class may be a leftover candidate for consolidation.
