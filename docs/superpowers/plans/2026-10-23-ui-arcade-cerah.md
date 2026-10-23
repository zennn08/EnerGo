# UI "Arcade Cerah" Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restyle every EnerGo screen from the flat cream-card look into the chunky cartoon "Arcade Cerah" look of mockup v5, without changing any game rule.

**Architecture:**
- The style lives in `EnerGoUI.cs`: tokens, fonts, cached gradient textures, puffy buttons, paper cards, pills, bars, stroked text, icons and the world background. The existing helpers keep their signatures, so every caller restyles at once.
- Layout math that can be tested goes in a new `Core/UiLayout.cs` (EnerGo.Core, EditMode-tested).
- Each screen task then rearranges its own `OnGUI` code to the mockup.

**Tech Stack:** Unity 6 (URP), C#, IMGUI, new Input System (EnhancedTouch), NUnit EditMode tests, Python 3 (fontTools and Pillow are installed) for asset prep.

**Spec:** `docs/superpowers/specs/2026-10-23-ui-arcade-cerah-design.md`. The mockup is `docs/superpowers/specs/assets/2026-10-23-ui-arcade-cerah-mockup.html`; open it in a browser, because it is the visual contract.

## Global Constraints

- **No behaviour changes.** Flow, rewards, timers and quiz rules stay identical. Only drawing and layout change. Hit tests (`TapHit`/`IsHeld`/`ButtonHit`) must use the same rect that is drawn.
- **Mockup px × 1.6 = GUI units** (before `HudScale`). This covers sizes, outline widths, stroke and radius.
- **Headers are never rotated.** The only rotated things are the quiz card while it is dragged and its BENAR/SALAH stamp.
- **Fonts:**
  - *Lilita One* for titles, buttons and big numbers.
  - *Fredoka* (static SemiBold 600 and Bold 700 instances) for body text.
  - The OFL license file sits next to the fonts in `Assets/EnerGo/Resources/Fonts/`.
- **No special glyphs in text** (✓ ✕ ⚡ ★ ⏱ …). Draw them as icon PNGs, because the fonts may lack them and Unity falls back silently.
- **Every text over the camera** sits on a pill (Ink 75%) or is stroked.
- **Right/wrong is never colour-only.** It always has text or a ✓/✕ icon.
- **Copy:** player text is Bahasa Indonesia, HUD lines are UPPERCASE, and the existing strings are kept unless the spec names new ones.
- **Code comments** match the surrounding style: short, English, and `// ponytail:` for deliberate shortcuts.
- Leave the unrelated `Assets/Plugins/NuGet`, `Packages/` and `ProjectSettings/` changes and the untracked `.claude/` and `.mcp.json` out of every commit.

## Review Focus

1. **Tapping ✕/✓ on the quiz must not also start a card drag.** The touch Began on the button would otherwise set `isDraggingCard` and later commit a second answer. Task 6 hit-tests the buttons before the drag starts, and the manual check is in Task 9.
2. **Stroked labels on many HUD lines each frame could cost frame time on the A16.** The stroke is only for titles, buttons, numbers and camera labels, and styles are cached (Task 3). Task 9 checks the frame time on the device.
3. **Notch or gesture bar overlap.** The dock, BERBURU! and TANGKAP must stay inside `Screen.safeArea`. The `UiLayout.SafeTop/SafeBottom` tests are in Task 1, and the manual check is in Task 9.
4. **Long strings in pills or buttons** ("DEKATI DULU (128 m)", "TARGET DI LUAR JANGKAUAN // IKUTI ARAH RADAR") must shrink or wrap, not clip. `EnerGoUI.FitFontSize` is tested in Task 1 and used for every pill and button label.
5. **A zero-unit collection** must render as 0 on the lobby tiles and as total 0 in the top pill. A save from before oil and uranium has no entries for them, and `UiLayout.TotalUnits` tolerates missing entries. The test is in Task 1.

---

### Task 0: Commit the spec

- [ ] **Step 1: Commit**

```bash
cd /d/code/unity/EnerGo
git add .gitignore docs/superpowers/specs/2026-10-23-ui-arcade-cerah-design.md docs/superpowers/specs/assets/2026-10-23-ui-arcade-cerah-mockup.html docs/superpowers/plans/2026-10-23-ui-arcade-cerah.md
git commit -m "docs: Arcade Cerah UI spec, mockup and plan

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 1: UiLayout (pure layout math)

**Files:**
- Create: `Assets/EnerGo/Scripts/Core/UiLayout.cs` (namespace `EnerGo`, static class)
- Test: `Assets/EnerGo/Tests/Editor/UiLayoutTests.cs`

**Interfaces:**
- Produces:
  - `const float Mock = 1.6f`
  - `float SafeTop(float screenH, float safeYMax, float scale)` and `float SafeBottom(float safeYMin, float scale)`. These are the current Lobby formulas: `Max(16, (screenH - safeYMax)/scale + 12)` and `Max(20, safeYMin/scale + 16)`.
  - `Rect[] Grid(Rect area, int count, int cols, float gap, float cellH)`. Cells fill row by row, and a short last row is centred.
  - `int TotalUnits(int[] perResource)`. Null is treated as 0, and negative entries are ignored.
  - `int FitFontSize(float textWidthAtSize, int size, float maxWidth, int minSize)`. This is the largest size ≤ `size` whose scaled width fits, never below `minSize`.
  - `string ResultTitle(int right, int count)`, which returns `"SEMPURNA!"` when right == count, `"COBA LAGI"` when right == 0, and `"BAGUS!"` otherwise.
  - `int Stars(int right)`, which is `Clamp(right, 0, 3)`.

- [ ] **Step 1: Write the failing tests** in `UiLayoutTests.cs`:

```csharp
[Test] public void SafeAreaKeepsTheMinimumMargins()
{
    Assert.AreEqual(16f, UiLayout.SafeTop(2340f, 2340f, 2.25f));
    Assert.AreEqual(20f, UiLayout.SafeBottom(0f, 2.25f));
    Assert.AreEqual(100f / 2.25f + 12f, UiLayout.SafeTop(2340f, 2240f, 2.25f), 1e-3f);
    Assert.AreEqual(90f / 2.25f + 16f, UiLayout.SafeBottom(90f, 2.25f), 1e-3f);
}
[Test] public void GridCentresTheShortLastRow()
{
    var r = UiLayout.Grid(new Rect(0, 0, 300, 200), 5, 3, 12f, 80f);
    Assert.AreEqual(5, r.Length);
    Assert.AreEqual(r[1].center.x, (r[3].center.x + r[4].center.x) / 2f, 1e-3f); // row 2 centred under row 1
    Assert.AreEqual(r[0].width, r[3].width, 1e-3f);
    Assert.AreEqual(80f + 12f, r[3].y, 1e-3f);
}
[Test] public void TotalUnitsToleratesMissingAndNegative()
{
    Assert.AreEqual(0, UiLayout.TotalUnits(null));
    Assert.AreEqual(17, UiLayout.TotalUnits(new[] { 12, 5, -3, 0 }));
}
[Test] public void FitFontSizeShrinksButNotBelowMin()
{
    Assert.AreEqual(20, UiLayout.FitFontSize(100f, 20, 200f, 12));
    Assert.AreEqual(10, UiLayout.FitFontSize(400f, 20, 200f, 10));
    Assert.AreEqual(12, UiLayout.FitFontSize(4000f, 20, 200f, 12));
}
[Test] public void ResultTitleAndStars()
{
    Assert.AreEqual("SEMPURNA!", UiLayout.ResultTitle(3, 3));
    Assert.AreEqual("BAGUS!", UiLayout.ResultTitle(2, 3));
    Assert.AreEqual("COBA LAGI", UiLayout.ResultTitle(0, 3));
    Assert.AreEqual(3, UiLayout.Stars(5));
    Assert.AreEqual(0, UiLayout.Stars(-1));
}
```

- [ ] **Step 2: Run the tests and verify they fail.** Run `npx -y unity-mcp-cli run-tool tests-run --input '{"testMode":"EditMode"}'`. Expected: compile error `The name 'UiLayout' does not exist`.
- [ ] **Step 3: Implement `UiLayout`** with the signatures above.
- [ ] **Step 4: Run the tests and verify they pass.** Run the same command. Expected: `"Status": "Passed"`, `FailedTests: 0`.
- [ ] **Step 5: Commit** with `feat: UiLayout helpers for the Arcade Cerah UI`.

---

### Task 2: Fonts and icon assets

**Files:**
- Create: `Assets/EnerGo/Resources/Fonts/LilitaOne-Regular.ttf`, `Fredoka-SemiBold.ttf`, `Fredoka-Bold.ttf` and `OFL.txt`
- Create: `Branding/icons/*.svg` (sources) and `Assets/EnerGo/Resources/UI/Icons/*.png` (256×256, alpha)
- Create: `Branding/icons/export_icons.py`

**Interfaces:**
- Produces these icon names, used by `EnerGoUI.Icon(name)` (Task 3):
  - resources: `sun`, `coal`, `gold`, `oil`, `uranium`, `dice`
  - top pill: `bolt`
  - dock: `book` (Panduan), `target` (Latihan), `gear` (Pengaturan)
  - HUD and quiz: `compass`, `star`, `star_empty`, `check`, `cross`, `clock`
  - world background: `tree`
- Produces Resources font paths: `Fonts/LilitaOne-Regular`, `Fonts/Fredoka-SemiBold` and `Fonts/Fredoka-Bold`.

- [ ] **Step 1: Fetch the fonts.**
  - Download from `https://github.com/google/fonts`: `ofl/lilitaone/LilitaOne-Regular.ttf`, `ofl/fredoka/Fredoka[wdth,wght].ttf` and `ofl/fredoka/OFL.txt`.
  - Make the static Fredoka instances with fontTools:
    - `python -m fontTools.varLib.instancer "Fredoka[wdth,wght].ttf" wght=600 wdth=100 -o Fredoka-SemiBold.ttf`
    - the same with `wght=700` for `Fredoka-Bold.ttf`.
  - Put the downloads in their own empty temporary folder and pass the paths as arguments, as for any download.
- [ ] **Step 2: Check the glyph coverage.**
  - Use fontTools to check that the cmap of all three fonts covers `A–Z a–z 0–9 . , : ; ! ? / % + - ( ) ' " · … ‹ › →`.
  - Expected: none missing. If one is missing, replace it in the copy (for example `‹` with `<`) and note it in the commit.
- [ ] **Step 3: Extract the icons and export them.**
  - Copy every `<symbol>` the list above needs from the mockup into `Branding/icons/<name>.svg`. The mockup ids map like this:

    | Mockup id | Icon name |
    |---|---|
    | `i-sun` | `sun` |
    | `i-coal` | `coal` |
    | `i-gold` | `gold` |
    | `i-oil` | `oil` |
    | `i-uran` | `uranium` |
    | `i-dice` | `dice` |
    | `i-bolt` | `bolt` |
    | `i-book` | `book` |
    | `i-target` | `target` |
    | `i-gear` | `gear` |
    | `i-comp` | `compass` |
    | `q-star` | `star` |
    | `q-star0` | `star_empty` |
    | `q-check` | `check` |
    | `q-x` | `cross` |
    | `q-clock` | `clock` |

  - `tree` is drawn fresh in the same style: a trunk `#A83A5C` plus three teal circles `#2E8A6C`/`#3FA77F` with a 1.6 px `#1D5A48` outline.
  - Glyph-only symbols (`book`, `gear`, `bolt`, `target`, `compass`) get the Ink fill in the SVG. `gear` is white in the mockup, so make it Ink for consistency.
  - `export_icons.py` rasterises each SVG to 256×256 PNG. Use any working rasteriser: `cairosvg`, `resvg`, or Playwright's `page.screenshot({omitBackground:true})` of a page holding the SVG. Then check the result with Pillow: mode `RGBA`, size 256×256, a corner pixel with alpha 0, and alpha present on the centre pixel.
- [ ] **Step 4: Set the Unity import settings.**
  - Icons: Texture Type `Default`, Alpha Is Transparency on, mipmaps off, filter Bilinear, max size 256, compression Normal.
  - Fonts: Dynamic.
  - Run `assets-refresh`.
- [ ] **Step 5: Commit** the fonts, `OFL.txt`, the SVG sources, the PNGs with their metas and the script, with `feat: Lilita One/Fredoka fonts and the icon set`.

---

### Task 3: EnerGoUI foundation

**Files:**
- Modify: `Assets/EnerGo/Scripts/EnerGoUI.cs`. It is 161 lines now. If it grows past about 400, split it into `EnerGoUI.cs` (tokens, fonts, input) and `EnerGoUI.Draw.cs` (shapes, text, icons, world) as a `static partial class`.

**Interfaces:**
- Consumes: Task 1 `UiLayout`, Task 2 fonts and icons.
- Produces:
  - **Colour tokens.** Replace the old palette, keeping the names `Ink`, `Sun`, `Orange`, `Success`, `Danger`, `Muted` and `Paper` so that callers still compile. `Paper` becomes `#FFFAF0`, `Ink` becomes `#14213A`, `Success` becomes `#45B83A` and `Danger` becomes `#E8304E`. Add these, with values from spec §2:
    - `SkyTop`, `Sky`, `SkyLow`, `Grass`, `GrassLow`, `Berry`, `PillBg`
    - the gradient pairs `OrangeTop/OrangeBot`, `OkTop/OkBot`, `BadTop/BadBot`, `InfoTop/InfoBot`, `PaperTop/PaperBot`
    - `ResourceTop(id)` and `ResourceBot(id)`
    - `Resource(id)` returns the middle colour of its pair.
  - **Gradients.** `Texture2D Gradient(Color top, Color bottom)` returns a cached 1×64 vertical texture with clamp wrap and bilinear filtering, keyed by the colour pair.
  - **Buttons.** `enum ButtonKind { Orange, Ok, Bad, Info, Sun, Paper, Off, Dark }` and `Rect DrawButton(Rect rect, ButtonKind kind, bool pressed = false)` return the face rect. Each button is drawn in this order:
    1. the Ink lip, offset 7×1.6;
    2. the gradient face with an Ink outline of 5×1.6 and radius 20×1.6, clamped to half the height;
    3. a dark band of 8×1.6 at the inner bottom, black at 18%;
    4. a highlight of 6×1.6 at the inner top, white at 45%.

    While the button is pressed, the face sits on the lip with no lip drawn. The existing wrappers map like this, keeping their signatures: `DrawPrimaryButton` → `Orange`, `DrawSecondaryButton` → `Paper`, `DrawDarkButton` → `Dark`.
  - **Tiles.** `void DrawTile(Rect r, string resourceId, int count)` draws the gradient from `ResourceTop/Bot`, radius 18×1.6, lip 6×1.6, the icon, and a stroked name. When `count > 0` it adds a Berry count badge at the top right. When `count == 0` it shows the badge "0", and `count < 0` shows no badge.
  - **Cards.** `void DrawCard(Rect rect, float radius = …, Color? outline = null, float outlineWidth = …)` keeps its signature and becomes a paper card: the PaperTop/Bot gradient, an Ink outline of 5×1.6, radius 24×1.6 and an Ink lip of 8×1.6. Add `void DrawRibbon(Rect cardRect, string text, ButtonKind kind)`. It draws a small button-shaped title centred on the card's top edge and half outside it, using the stroked Lilita label.
  - **Pills and bars.**
    - `Rect DrawPill(Rect r)` draws PillBg with a 3×1.6 Ink outline and full radius. It returns the inner rect.
    - `DrawBar(Rect track, float progress, Color fill)` keeps its signature and becomes an Ink track with an Ink 4×1.6 outline.
    - Add the overload `DrawBar(Rect track, float progress, Color top, Color bottom)` for gradient fills.
  - **Text.**
    - `GUIStyle Title(int size, Color color, TextAnchor align = MiddleCenter)` uses Lilita.
    - `Text(...)` keeps its signature and now uses Fredoka Bold for bold text and Fredoka SemiBold for regular text.
    - `void DrawStrokedLabel(Rect r, string text, GUIStyle style, float strokePx, float dropPx = 0f)` draws 8 Ink copies offset on a circle of `strokePx`, then an optional Ink copy at `+dropPx` in y, then the text itself. Styles are cached by `(size, color, align)`.
    - Add `int FitSize(GUIStyle style, string text, float maxWidth, int minSize)`, which wraps `UiLayout.FitFontSize` using `style.CalcSize` at the style's size.
  - **Icons.** `Texture2D Icon(string name)` loads `UI/Icons/<name>` from Resources and caches it.
  - **World background.** `void DrawWorld(float sw, float sh, float dim = 0f)` draws, in order:
    1. the sky gradient SkyTop→SkyLow;
    2. a grass hill: a large rounded rect in the Grass→GrassLow gradient, starting at `sh*0.40` and wider than the screen so its corners hide;
    3. a cached 64×36 isometric diamond-line texture at white 12%, tiled with `DrawTextureWithTexCoords`;
    4. 4 to 6 `tree` icons at fixed positions relative to `sw`/`sh`;
    5. if `dim > 0`, an Ink overlay at `dim` alpha.
  - **Safe area.** `float SafeTop(float scale)` and `float SafeBottom(float scale)` call `UiLayout` with the `Screen` values.

- [ ] **Step 1: Implement the interfaces above.** `ApplyFont` switches to the Fredoka instances. Poppins stays in Resources until Task 9.
- [ ] **Step 2: Compile and run the tests.**
  - Run `npx -y unity-mcp-cli run-tool console-get-logs --input '{"logTypes":["Error"]}'`. Expected: no compile errors.
  - Run `tests-run` with EditMode. Expected: all pass.
- [ ] **Step 3: Take a screenshot of the lobby** with the recipe in memory `unity-mcp-screenshot-recipe`: 1080×1920 Game View and `ScreenCapture.CaptureScreenshot`. Expected: the old lobby layout now has puffy orange buttons, paper cards and Fredoka text, with no missing-glyph boxes.
- [ ] **Step 4: Commit** with `feat: Arcade Cerah style foundation in EnerGoUI`.

---

### Task 4: Lobby, splash, settings, practice and guides

**Files:**
- Modify: `Assets/EnerGo/Scripts/LobbyController.cs` (`DrawSplash`, `DrawLobby`, `DrawCollectionCard`, `DrawTransition`, `DrawSettings`, `DrawPractice`, `DrawGuides`, and the toggle and row helpers)

**Interfaces:**
- Consumes: everything Task 3 produces.
- Produces: nothing used elsewhere.

- [ ] **Step 1: Every screen uses `DrawWorld`.**
  - Splash, Lobby and Transition use `dim = 0`.
  - Settings, Practice and Guides draw `DrawWorld(sw, sh, 0.45f)`, then a paper card with a ribbon titled `PENGATURAN`, `LATIHAN` or `PANDUAN`, in place of the bottom sheet.
- [ ] **Step 2: Lay out the lobby to the mockup, from top to bottom.**
  - The top pill shows the `bolt` icon and `UiLayout.TotalUnits` over all five `InventoryEntry.Total`. It sits at `SafeTop`.
  - The title is `ENER` in white and `GO!` in `#FF9A1F`, in Lilita size 56×1.6×0.75 (≈ 67), stroked 6×1.6 with a drop of 5×1.6. It is not rotated.
  - Below it is the pill `KOLEKSI ENERGIMU` in Sun text.
  - The tiles come from `UiLayout.Grid(area, 5, 3, 10×1.6, 82×1.6)` and are drawn with `DrawTile(r, id, entry.Total)`. A resource that is not playable keeps the existing lock: `DrawLock` on top and alpha 0.45.
  - Above the button is the hint `Mode kamera: …`.
  - Then comes the `BERBURU!` button: `DrawButton(…, Orange)` with height 74×1.6×0.75 and a Lilita label. It keeps the existing ExploreScene action.
  - At the bottom is the dock: one pill holding 3 buttons. They are `DrawButton` of kind `Sun`, `Info` and `Paper`, with the icons `book`, `target` and `gear`. They lead to `ScreenState.Guides`, `ScreenState.Practice` and `ScreenState.Settings`. The dock bottom sits at `SafeBottom`.
  - Delete the old top-right `?` and settings buttons and the faint crystal watermark.
- [ ] **Step 3: Restyle the inner content of the sheets.**
  - Toggle rows: a pill row with a switch drawn as a small `DrawButton(Ok|Off)`.
  - Volume: `DrawBar` with the Orange gradient.
  - Practice: `UiLayout.Grid(area, 6, 3, …)` with `DrawTile`. The sixth tile is `dice` with the label `Acak` and count −1.
  - Guides: rows inside the card.
  - Keep every existing action and string, including `TEKAN LAGI UNTUK MENGHAPUS`.
- [ ] **Step 4: Compile, run the tests and take screenshots** of the lobby, the settings, the practice and the guides. Expected: no errors, all tests pass, and the screenshots match the mockup lobby. There are only three dock buttons and no tilted text.
- [ ] **Step 5: Commit** with `feat: Arcade Cerah lobby, settings, practice and guides`.

---

### Task 5: Shared AR HUD helpers

**Files:**
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs` (`InitCardStyles` and the style fields, `DrawPill`, `DrawHudBar`, `DrawPrimaryBtn`, `DrawSunReticle`, `DrawCoalPips`, `DrawCompassStrip`, and the header and footer block in `OnGUI` at about lines 1567–1672)

**Interfaces:**
- Consumes: Task 3.
- Produces, for Tasks 6 and 7:
  - `void DrawHudPill(Rect r, string text, Color textColor)`, which is a pill with fitted text;
  - `void DrawHudPill(Rect r, string icon, string tag, string val)`, which keeps the tag and value layout and replaces `DrawPill`;
  - the existing `DrawHudBar(...)` and `DrawPrimaryBtn(...)` with their signatures unchanged, restyled.

- [ ] **Step 1: Restyle the shared HUD.**
  - Header row: a target pill with the resource icon and the timer pill. Feedback lines become a pill in Sun text, sized with `FitSize` and a minimum of 11.
  - Bars use the gradient `DrawBar`.
  - The reticle is a ring dashed in white with an Ink inner outline, plus a soft Sun glow (`DrawPanel` circle at alpha 0.35).
  - TANGKAP and capture buttons are round `DrawButton(Sun)` with stroked Lilita text.
  - The close or back control is a round `DrawButton(Dark)` with the `cross` icon.
  - The mode-switch and respawn buttons become small `Paper` buttons.
  - All header and footer positions use `EnerGoUI.SafeTop` and `SafeBottom`.
- [ ] **Step 2: Compile, run the tests and take a screenshot** of the sun mini-game with the target in range. Drive the state by reflection as the recipe says. Expected: every text is on a pill or stroked.
- [ ] **Step 3: Commit** with `feat: Arcade Cerah AR HUD`.

---

### Task 6: Quiz, feedback and result

**Files:**
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs` (`DrawAnalysis`, the swipe-card block after it, `HandleCardSwipeInput`, `CommitOrReturnCard`)

**Interfaces:**
- Consumes: Tasks 1, 3 and 5.
- Produces: `void CommitAnswer(bool answer)`. This is the body that `CommitOrReturnCard` uses today: it calls `EnerGoProgress.Answer`, then the haptics, the SFX and `resultShownAt`. `CommitOrReturnCard` now calls it.

- [ ] **Step 1: Answer buttons.**
  - Add `Rect QuizButtonRect(bool answer, float sw, float sh)` in GUI units. There are two `DrawButton` rects at the bottom, each of height 56×1.6×0.75, above `SafeBottom`. `false` is on the left and uses `Bad`; `true` is on the right and uses `Ok`. Each is labelled with an icon (`cross`/`check`) plus the stroked text `SALAH`/`BENAR`.
  - In `HandleCardSwipeInput`, on touch Began and in the editor mouse-down branch, if the point is inside either button rect:
    1. set `cardOffset = new Vector2(answer ? sw : -sw, 0)`;
    2. set `cardFlicking = true`;
    3. call `CommitAnswer(answer)`;
    4. skip the drag start for that touch.
  - The touch-to-GUI conversion is the same as in `TapHit`.
- [ ] **Step 2: Restyle the card.**
  - Background: `DrawWorld(sw, sh, 0.45f)`, in place of the Cream fill.
  - Top: three progress dots, each a circle with an Ink outline. The current dot is Sun, a correct one is Ok with `check`, and a wrong one is Bad with `cross`. Next to them is the pill `SOAL n/3`.
  - Title: `BENAR ATAU SALAH?`, with "ATAU" in `#FF9A1F`, stroked and not rotated.
  - The card is a paper card with one tinted paper card peeking out behind it (only the card stack may rotate, by 3°).
  - Inside the card: the timer row with the `clock` icon, the gradient bar and the seconds; then the resource icon at 74×1.6×0.75; then the statement in Fredoka Bold Ink.
  - The drag keeps its existing tilt. The BENAR or SALAH stamp becomes a Lilita label in an Ok or Bad outline box.
  - Remove the `‹ geser kiri …` hint line, because the buttons replace it.
- [ ] **Step 3: The feedback state.**
  - The card has a ribbon: `BENAR!` uses `Ok`, and `SALAH` or `WAKTU HABIS` uses `Bad`.
  - Below it are a chip `Pernyataan ini BENAR|SALAH`, the resource icon, and the explanation in Muted.
  - When correct, add a chip with the resource icon and `+1 unit`.
  - The button is `SOAL BERIKUTNYA` or `LIHAT HASIL` using `Orange`. While it is locked, it is an `Off` button filling left to right over the 2 s, labelled `BACA DULU...`. This keeps the existing `resultShownAt` timing.
- [ ] **Step 4: The result state.**
  - A stroked title from `UiLayout.ResultTitle(right, count)` sits above the card.
  - The card has the `HASIL ANALISIS` ribbon in `Orange` and three stars, where `UiLayout.Stars(right)` are filled and the rest are `star_empty`. The middle star is larger.
  - Then `n/3 BENAR` in Lilita Ink, then the resource icon with a big stroked `+{rewardQuantity}` and a chip `Murni|Normal`.
  - Then the chips `Dasar +1`, `Kuis +n` and `{MiniGameName} {Grade} +b`, followed by the existing message and the existing `LANJUT BERBURU` / `LANJUT KE LOBBY` button.
- [ ] **Step 5: Compile, run the tests and take screenshots** of the card, the correct feedback, the wrong feedback (locked button), and the 2/3 result. Back up and restore the save as the recipe says. Expected: they match mockup screens 1–4.
- [ ] **Step 6: Commit** with `feat: Arcade Cerah quiz with answer buttons`.

---

### Task 7: Mini-game overlays and guides

**Files:**
- Modify:
  - `Assets/EnerGo/Scripts/ARCapturePrototype.cs`: `DrawGoldPan` and the gold countdown, `EMAS TERBUANG!` card, and coal pips;
  - `ARCapturePrototype.Oil.cs`: `DrawOilHud`, `DrawOilOverlay`, `DrawOilSection` and `DrawOilPump`;
  - `ARCapturePrototype.Uranium.cs`: `DrawUraniumHud`, `DrawHudTag` and `DrawUraniumOverlay`;
  - `Assets/EnerGo/Scripts/GuideViewer.cs`.

**Interfaces:**
- Consumes: Tasks 3 and 5.

- [ ] **Step 1: Restyle the mini-game overlays.**
  - Replace each panel, tag and bar with a paper card, `DrawHudPill` or the gradient `DrawBar`.
  - Counters such as `EMAS n/5` become pills.
  - The countdown and `MULAI!` use the stroked Lilita title.
  - Fail cards such as `EMAS TERBUANG!` become a paper card with a `Bad` ribbon and an `Orange` button.
  - Keep every rect that a touch handler also tests, for example `UraniumGripRect`, `GoldPanRect`, and the oil guiY bounds at `ARCapturePrototype.Oil.cs:90-91`. Restyle only what is drawn inside them.
- [ ] **Step 2: Restyle `GuideViewer`.**
  - Each step is a paper card with a ribbon carrying the resource name, over the camera dimmed with an Ink overlay at 0.45.
  - The next or done button uses `Orange`.
  - The "Jangan ingatkan lagi" checkbox becomes a small `Ok`/`Off` square button with `check`.
- [ ] **Step 3: Compile, run the tests and take screenshots** of each mini-game in its main phase (sun, coal, gold, oil, uranium) and of one guide. Expected: no old white cards remain. `grep -n "DrawCard\|DrawPanel" ARCapturePrototype*.cs` should list only calls that go through the restyled helpers or draw circles and glows.
- [ ] **Step 4: Commit** with `feat: Arcade Cerah mini-game overlays and guides`.

---

### Task 8: Radar

**Files:**
- Modify: `Assets/EnerGo/Scripts/RadarController.cs` (`OnGUI`, `DrawRadar`, `DrawSelected`, `DrawStatus`, `DrawSafety`)

**Interfaces:**
- Consumes: Task 3.

- [ ] **Step 1: Restyle the radar.**
  - Background: `DrawWorld(sw, sh)`, in place of the white fill.
  - `‹ LOBBY` becomes a `Paper` button, and the spawn timer becomes a pill.
  - The radar disc is a round paper card (PaperTop/Bot with an Ink outline of 5×1.6). The range rings are Ink at 15%, and the north letter is in Orange Lilita.
  - Spawns use `Icon(resourceIconName)`, in place of the crystal textures, with the same glow and selection rings. The selection ring is Ink, and the in-range ring is Orange.
  - The selected card is a paper card with a `TANGKAP` (`Orange`) button, a `SEGERA HADIR` button, or `DEKATI DULU (x m)` (`Off`).
  - The status and safety popups are paper cards with ribbons: `Hati-hati di jalan` uses `Orange`.
  - The GPS lines become pills. A weak signal is shown as a pill in Bad text.
- [ ] **Step 2: Compile, run the tests and take a screenshot** of the radar with a selected spawn, in Editor mode with fake GPS as the existing editor path does. Expected: no white page and no crystal icons.
- [ ] **Step 3: Commit** with `feat: Arcade Cerah radar`.

---

### Task 9: Cleanup, build and device check

**Files:**
- Delete only after a grep shows no references:
  - `Assets/EnerGo/Resources/UI/bg_lobby.jpg`, `btn_play.png`, `btn_secondary.png`, `card_panel.png` and `pill_badge.png`;
  - `crystal_*.png`;
  - `Fonts/Poppins-*.ttf`, together with their `.meta` files.
- Keep `logo_game.png` and `logo_wordmark.png`, which are used by the splash and brand.

- [ ] **Step 1: Find unreferenced assets.** For each candidate, `grep -rn "<name without extension>" Assets/EnerGo/Scripts Assets/EnerGo/Editor`. Delete only the ones with no hits. Then `assets-refresh`, check the console has no errors, and run `tests-run` with all passing.
- [ ] **Step 2: Build the APK** with the queued-build pattern from `docs/superpowers/plans/2026-10-11-uranium-ambil-aman-haptics.md` Task 6. Use `locationPathName = "Builds/EnerGo-ArcadeCerah.apk"` and the result file `Builds/arcade-result.txt`. Expected: `Succeeded errors=0`.
- [ ] **Step 3: Device checklist (the user, on the Samsung A16).**
  - Every screen has the new look, and no cream or white page is left: splash, lobby, settings, practice, guides, radar, the 5 mini-games, the quiz and the result.
  - No title is tilted.
  - AR text can be read outdoors in daylight and indoors.
  - In the quiz, tapping ✕ or ✓ answers exactly once and the card flies out. A swipe still works. The 2 s lock after a wrong answer still holds.
  - The dock, BERBURU! and TANGKAP are clear of the gesture bar and the camera cut-out.
  - Frame rate in the sun and gold mini-games feels as smooth as before.
- [ ] **Step 4: Commit** with `chore: remove assets replaced by the Arcade Cerah UI`.
