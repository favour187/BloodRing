# Blood Ring

**Blood Ring** is an original, mobile-first 3D battle-royale game ecosystem built
for mid-range Android (with optional iOS export). It pairs a Unity URP client with
a Node.js backend and a fully **authored** art library — every visual in the game
is a real, pre-made asset. **No art is generated procedurally by code at runtime.**

> Visual identity: a dark cyberpunk arena aesthetic — black & gunmetal surfaces,
> crimson-red energy accents, and cyan tracer highlights. "Enter the storm. Own the ring."

---

## Repository layout

```
/Client          Client-side build profiles, platform configs, entry points
/Server          Dedicated game-server & authoritative simulation configs
/Assets          Unity project assets (Scripts, Resources, Scenes, ScriptableObjects)
/UI              Master UI component library (500+ icons, 300+ buttons, 250+ panels, motion systems)
/Audio           Master audio channel routing, spatialization & sound libraries
/Effects         URP mobile VFX shaders, particle systems & visual feedback configs
/Networking      Transport, matchmaking, replication & anti-cheat configuration
/LiveOps         Live configuration, events, battle-pass & store schedules
/Analytics       Telemetry schema & dashboards
/Build           Build output + build pipeline definitions
/Docs            Architecture, roadmap, QA checklist, technical docs
/Generated       Machine-built indexes (asset_manifest.json, asset_library_20k.json)
/Tools           Asset validator, manifest builder, QA test runner & deploy scripts
/backend         Node.js backend (auth, profile, store, leaderboard, social, match)
```

See [`Docs/ARCHITECTURE.md`](Docs/ARCHITECTURE.md) for the full breakdown.

---

## Art policy (important)

All art is **authored content** that ships as real files under
`Assets/Resources/Art`. The previous code-based / procedural placeholder-art
pipeline (Python generators, runtime `ProceduralArt`, vector "recipe" assets) has
been **removed**. New art is produced as finished PNGs and indexed by:

```bash
python3 Tools/build_manifest.py     # rebuild Generated/asset_manifest.json
python3 Tools/validate_assets.py    # CI gate: rejects empty/placeholder art
```

Runtime code accesses art only through `BloodRing.Art.BloodRingArtLibrary`, which
loads finished assets from `Resources/Art` — it never paints textures in code.

---

## Engine & targets

| | |
|---|---|
| Engine | Unity 2022 LTS, Universal Render Pipeline (URP) |
| Primary platform | Android (mid-range) |
| Optional | iOS export |
| Performance target | 60 FPS (30 FPS fallback) |
| Optimization | LOD, streaming, pooling, occlusion, texture compression |

---

## Quick start

**Backend**
```bash
cd backend
npm install
npm start
```

**Client** — open the repository root in Unity 2022 LTS and load the boot scene.
Mobile/Android build settings are documented in
[`Docs/MOBILE_ANDROID_BUILD.md`](Docs/MOBILE_ANDROID_BUILD.md).

---

## Status

Production is incremental. Authored art is being expanded batch-by-batch
(terrain → weapons → vehicles → characters → UI → effects). Progress is tracked in
[`Docs/ROADMAP.md`](Docs/ROADMAP.md) and the current asset inventory lives in
[`Generated/asset_manifest.json`](Generated/asset_manifest.json).


