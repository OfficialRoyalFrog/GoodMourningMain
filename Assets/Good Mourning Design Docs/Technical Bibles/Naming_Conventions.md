# Naming Conventions

## Scripts & Namespaces
- Use `KD.<Domain>` (e.g., `KD.Mood.ContentmentAgent`).
- Classes: `PascalCase`, methods `camelCase`, fields `_camelCase` private.
- Avoid abbreviations; prefer clarity.

## Assets & Files
| Type | Prefix | Example |
|------|--------|---------|
| ScriptableObject | `SO_` | `SO_Ritual_Cleansing.asset` |
| Prefab | `pfb_` | `pfb_altar_t2.prefab` |
| Material | `mat_` | `mat_ui_panel.mat` |
| Shader | `shd_` | `shd_ghost_fade.shader` |
| Texture/Sprite | `tex_` | `tex_icon_memento.png` |
| Audio | `sfx_`/`mus_` | `sfx_manifest_splat.wav` |
| VFX | `vfx_` | `vfx_cleansing_burst.prefab` |
| UI | `ui_` | `ui_panel_inventory.prefab` |
| Scene | `scn_` | `scn_Hub_Main.unity` |

## IDs
- Ghost: `ghost_{guid}`
- Structure: `str_{guid}`
- Charm: `ch_{guid}`
- Ritual: `rit_{shortname}`
