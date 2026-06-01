# Bubble UI — minimal correct design (for maintainers & AI tools)

This document captures the **working contract** for WhatsApp-style bubbles in this WinForms app (RTL Hebrew, AI vs user tails, thinking vs text). Follow it to avoid regressions (clipping, flat edges, dot overflow, wrong row height).

---

## 1. Layering

| Layer | Responsibility |
|--------|----------------|
| **`Form1.LayoutMessages`** | Row width, `bubble.GetPreferredSize`, merge `bubble.MinimumSize` into `pref` **before** row height (WinForms can enlarge `Size` to `MinimumSize` without updating the pref you used for row height → clip). |
| **`SpeechBubble`** | Chrome (`Padding`, `TailWidth`), **owner-draw** fill + border, **single closed outline** path (no two separate fill figures + winding cancellation). |
| **`BubbleTextView`** | Text only: measure, preferred size, paint (RTL via `TextFormatFlags`, **not** `Control.RightToLeft.Yes`). |
| **`ThinkingDots`** | Three dots only: **no** “center in bubble” requirement; cluster biased **toward the tail side** with **hard clamps** so dots never cross the inner edge toward the tail. |

---

## 2. AI “thinking” bubble (before fill) — **algorithm that worked**

### 2.1 `Form1.CreateBubble(..., isThinking: true)`

- Smaller padding than text bubbles.
- **`ThinkingDots.TailOnRight = (speaker == Speaker.Ai)`** — AI tail on the **right**, so dots hug the **right** (tail side), not the left.
- **`bubble.StretchContent = true`** so `ThinkingDots` receives the full inner rectangle (after tail + padding insets in `SpeechBubble.OnLayout`).
- **`MinimumSize.Width`** must leave enough **inner** width for: `marginInner + marginTailAi + tailClusterReserve + (3×dot + 2×spacing)` (see `ThinkingDots`). Use **`Math.Max(124, dotsOuter)`** style floor so layout never squeezes the cluster into negative `layoutW`.
- **`SpeechBubble.GetPreferredSize`**: when `Content` is **`ThinkingDots`**, add **~12px** to the returned **height** (beyond dots + padding). Short rows otherwise clip the bottom rounded edge / tail Bézier (anti-alias + control points extend slightly past strict chrome math).

### 2.2 `ThinkingDots.OnPaint` (critical)

1. **`Graphics.Save` / `SetClip(ClientRectangle)` / … / `Restore`** — anti-aliased ellipses can spill 1–2 px past bounds; clip prevents “stray dot” on the bubble stroke.
2. **Asymmetric horizontal margins**
   - **AI (`TailOnRight == true`)**: larger padding on the **right** (`marginTailAi`, e.g. **26px**) — tail + stroke live there; small padding on the left (`marginInner`, e.g. **6px**).
   - **User (`TailOnRight == false`)**: mirror (tail on left → larger `marginL`).
3. **`tailClusterReserve`** — extra shrink of the **layout band** on the side toward the tail (**larger for AI**, e.g. **18** vs **8**) so the cluster is never flush with the seam.
4. **Scale** `(d, s)` if `(3d + 2s) > layoutW` (floor `d`/`s`, never below sensible mins).
5. **`hardRight = Width - marginR - 3`** — after computing `startX` from tail-alignment rules, **`startX = Min(startX, hardRight - totalW)`** and **`drawX = Min(drawX, hardRight - d)`** per dot so **nothing** draws past the safe right bound (fixes third dot sitting on tail / border).
6. **Do not** use `Pen.Round` caps on a **closed** bubble path for the border (separate issue in `SpeechBubble`) — round caps on `DrawPath` look like extra blobs at the tail tip.

### 2.3 What *not* to do for thinking

- Do not “center” the three dots in the full bubble — requirement is **tail-adjacent cluster**, not centered text.
- Do not use **symmetric** L/R margins for AI and user — the tail side always needs **more** margin + reserve.

---

## 3. `SpeechBubble` geometry (fill + stroke)

- **One** closed `GraphicsPath` for both **FillPath** and **DrawPath** (`CreateBubbleOutlinePath`).
- **Right tail**: `CreateRoundedOutlineWithSmoothTail` (tail on `body.Right`).
- **Left tail**: **`CreateRoundedOutlineWithSmoothTailLeft`** built in **screen space** (same wall order as the right version: top arcs, straight far wall, bottom arcs, tail wall segment + Bézier tail). Avoid relying on a mirrored path for fill if it ever interacts badly with winding / corners.
- **Do not** shrink `ClientRectangle` by 1px before building the path if it clips anti-aliased corners (“flat” edge look).
- Border `Pen`: **`LineCap.Flat`** on closed outline; **`PenAlignment.Inset`**.

---

## 4. Text bubble (after fill) — `BubbleTextView` + `Form1`

### 4.1 Measurement (`GetPreferredSize`)

- **`Control.RightToLeft` stays `No`**. Hebrew uses **`TextFormatFlags.RightToLeft`** (and related) on `TextRenderer` only.
- **`MinTextWidth`** (e.g. 56) is a **policy floor** for wrapping probes — not the same as “minimum outer bubble 132px” (that over-wide short messages).
- **Single line at cap**: if `WordBreak` measurement at `widthCap` still yields **~one line height**, treat as **`IsSingleLine = true`** and width **`Min(widthCap, singleW)`** even when `singleW` and slack disagree — avoids “fake multiline” height.
- **`paintAsSingleLine` in `OnPaint`**: if there are no `\r/\n` and **unbounded single-line width ≤ `rect.Width`**, paint as **single line** even if `IsSingleLine` was false (stale or probe mismatch).

### 4.2 `Form1.SetBubbleContentToText` — outer width

- **Wrong**: fixed **`MinimumSize.Width = max(132, …)`** before or regardless of measured text → short Hebrew sits in a **huge** inner area; centering math then looks “top-left” relative to the oversized box.
- **Right**: after **`txt.RecalculateSize()`**, set  
  `bubble.MinimumSize.Width = Max(textChrome + smallInnerFloor, txt.Width + textChrome)`  
  so the bubble **tracks** content for short strings while keeping a small readability floor.

### 4.3 Painting — centered Hebrew (must match pixels on screen)

- **Do not** rely on **`HorizontalCenter` + `RightToLeft` on a wide rect** — the run often hugs one side visually.
- **Do**: **`MeasureText`** without **`NoPadding`** (that flag shrinks height and **clips descenders**). Then **`DrawText`** in a **tight `tw×th` box** placed at `(rect.Width-tw)/2`, `(rect.Height-th)/2`, with **`Right | RightToLeft | VerticalCenter`** (+ `SingleLine` or `WordBreak`). **`Right`** anchors Hebrew correctly inside the tight box; centering is geometric via box position.
- **`SpeechBubble.GetPreferredSize`**: **~16px** height slack for **`BubbleTextView`**. **`Form1.LayoutMessages`**: **+8px** on `pref.Height` for text rows. Multiline width scan starts at **`MinTextWidth`** (not 72) so short bubbles stay narrow.

### 4.4 Multiline **width** (avoid “full viewport” short bubbles)

- Do **not** score only by target aspect ratio: at a **narrow** wrap width, `WordBreak` can report **more** lines than at a **wide** width, so the old scorer picked a **wide** column for short Hebrew.
- **Do**: compute **minimum pixel height** over candidate widths, then choose the **narrowest** width whose height equals that minimum (step ~4px). That shrink-wraps “איך / אפשר” style messages.

---

## 5. File map (quick)

| File | Bubble-related |
|------|------------------|
| `Form1.cs` | `CreateBubble`, `AddThinkingBubble`, `SetBubbleContentToText`, `LayoutMessages` (pref + `MinimumSize` merge), `GetBubbleTextMaxWidth` |
| `SpeechBubble.cs` | Outline path, tail geometry, `OnLayout` content insets |
| `BubbleTextView.cs` | Text size + paint |
| `ThinkingDots.cs` | Dots layout + paint |

---

## 6. Status (snapshot)

- **Thinking (AI)**: algorithm in §2 is the reference “good” behaviour: tail-side bias + clamps + clip + minimum outer width.
- **Text after fill**: `SetBubbleContentToText` sets **`MinimumSize.Width` from `txt.RecalculateSize()`** (no fixed 132px floor); single-line paint uses **full `rect`** with **`HorizontalCenter | VerticalCenter`** (§4.2–4.3).

When changing any of the above, re-run a visual check: AI thinking (right tail), user thinking (left tail), short Hebrew single line, long wrapped AI reply.
