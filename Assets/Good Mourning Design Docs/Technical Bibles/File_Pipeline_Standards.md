# File & Pipeline Standards

- Keep assets under `/_Project/` with domain folders (`Art`, `Audio`, `Code`, `Docs`, `Prefabs`, `Scenes`, etc.).
- Prefabs are atomic; stations and interactables serialize IDs using the Save Master keys.
- ScriptableObjects drive gameplay data (Rituals, Charms, Requests, Plants).
- Scenes: `scn_Hub_Main`, shards like `scn_Shard_WitheredParish`.
- Version control: meta files on, feature branches (`feature/<name>`), commit messages use `feat:`/`fix:`/`refactor:` etc.
- LFS for large audio/texture packs if needed.
