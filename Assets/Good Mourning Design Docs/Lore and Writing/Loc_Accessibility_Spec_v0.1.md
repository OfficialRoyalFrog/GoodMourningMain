# Localization & Accessibility Spec v0.1 — *Good Mourning*

## Overview
Defines pipeline for multilingual text and accessibility support in Unity (URP + TMP).

## Localization
- **Format:** CSV / Google Sheet → Unity Localization Tables.
- **Source language:** English (en-US).
- **Targets:** FR, DE, JP post-launch.
- **File naming:** `loc_<lang>.csv` (e.g., `loc_fr.csv`)
- **Font Atlas:** NotoSans fallback for CJK, 4096×4096, max 2 atlases.
- **Workflow:**
  1. Export `strings.csv` from design docs.
  2. Import via Unity Localization package.
  3. Validate in editor for overflow/clipping.
- **Runtime:** TMP font fallback chain with atlas hot-swap.

## Accessibility
- **Text Scaling:** 0.8–1.5× global multiplier.
- **High Contrast Mode:** toggle swaps UI sprite sheet (white-on-black).
- **Colorblind Filters:** LUT swap (Protan/Deutan/Tritan).
- **Audio Captions:** `[SFX: distant whisper]` markup localized separately.
- **Input Remap:** Unity Input System rebind UI.
- **Auto-Read Narration:** platform TTS option.

## Testing Checklist
| Feature | Status | Notes |
|----------|---------|-------|
| Font swap pipeline | ☐ | Verify fallback rendering |
| Text overflow test | ☐ | Multilanguage string audit |
| Contrast mode | ☐ | Confirm readability |
| Colorblind filters | ☐ | LUT validation |
| Subtitle captions | ☐ | Consistent timing |
| Input remap | ☐ | Rebind persistence |
| TTS voice | ☐ | OS integration |

_Tagline: “Every ghost deserves to be heard.”_
