# Blood Ring — Production Roadmap

Authored-art production is incremental and batched. Each art batch is finished PNG
content (no code-generated placeholders), indexed into `Generated/asset_manifest.json`
and gated by `Tools/validate_assets.py`.

## Milestones

### M0 — Cleanup & foundation ✅
- Removed all code-based art generators (Python + runtime `ProceduralArt`).
- Removed procedural/"recipe" placeholder assets and noise textures.
- Reorganized authored art into `Assets/Resources/Art/{Characters,Weapons,Vehicles,Terrain,UI,...}`.
- Added `BloodRingArtLibrary`, `SceneArtBackdrop`, asset manifest + validator, GitHub CI.

### M1 — Core authored art (in progress)
- [x] Terrain tiles: grass, sand, rock, snow, asphalt, mud, concrete, metal grate
- [x] Weapons batch 1 (legacy): AK47, AK47 Evo skin, AWM — genuine hero-shot renders
- [x] Weapons batch 2: M4A1, Groza, Vector, DesertEagle, Kar98k, M1887, UMP,
      SCAR, M82B, G18 — 10 hero-shot renders in the dark cyberpunk /
      crimson-cyan house style. Removed all fake noise-placeholder weapon PNGs
      (`HD_*`, `Wpn_2D_Equalized_*`) that were padded past the validator's size
      threshold but contained no real artwork — these violated the project's
      own "no procedural art" policy. Fixed `BloodRingArtLibrary.Weapon()` and
      `GameHUD` weapon icon lookup so each gun now resolves to its own art
      instead of silently falling back to the AK47 icon.
- [x] Weapons batch 3: full SMG class completed — MP40, P90, Mac10, Thompson,
      Bizon, MP5, CG15 (original in-house SMG design), MP9 — plus two more
      assault rifles, FAMAS and XM8. 22 / 50 catalog weapons had real authored
      hero-shot art at the end of this batch.
- [x] Weapons batch 4: full Assault Rifle class completed — AN94, AUG, Parafal
      (original in-house design), Kingfisher (original in-house design), G36,
      FAL — plus 4 shotguns: M1014, SPAS12, MAG7, and ChargeBuster (original
      in-house charge-up energy shotgun). 32 / 50 catalog weapons had real
      authored hero-shot art at the end of this batch.
- [x] Weapons batch 5 (this session): full Shotgun class completed — Trogon
      and Striker12 (both original in-house designs) — plus the full
      Sniper/DMR class: M24, SVD, Woodpecker (original in-house design), AC80
      (original in-house design), M14 — plus 3 pistols: M500, USP, MiniUzi.
      **42 / 50 catalog weapons now have real authored hero-shot art**
      (`Assets/Resources/Art/Weapons/*_Hero.png`). Every render is a genuine
      AI-generated hero shot — zero code-generated/procedural placeholder art
      used anywhere in this batch.
- [ ] Weapons batch 6 (next session, final weapons batch): 1 remaining pistol
      (TreatmentGun), all 4 melee weapons (Katana, Pan, Machete, Bat), and all
      3 specials (Crossbow, M79, Gatling) — this will complete 50/50 weapon
      art coverage. Then: throwables (frag/smoke/flash/molotov/sticky) and
      weapon attachment/skin icon sets.

- [ ] Vehicles: complete ground/air/water set + skins
- [ ] Characters: hero roster portraits + outfits
- [ ] UI: icon set, buttons, HUD, frames, backgrounds
- [ ] Effects: smoke, explosion, weather, lighting sheets
- [ ] Scene backdrops: splash, lobby, character hub, battle map, results

### M2 — Systems vertical slice
- Full match loop: aircraft → landing → gameplay → zone → results.
- Inventory, loot, weapons handling, vehicles drivable.
- Backend: auth, profile, store, leaderboard, social wired to client.

### M3 — LiveOps & polish
- Battle pass, events, store rotation via `/LiveOps` remote config.
- Ranking, clans/factions, missions, cosmetics.
- Performance pass: LOD/streaming/pooling/occlusion verified on mid-range Android.

### M4 — Release candidate
- Anti-cheat, regional matchmaking, cloud saves hardened.
- Full QA pass (see `QA_CHECKLIST.md`), store submission assets finalized.

## Art generation note
Art is produced in capped batches. When a batch hits the per-session generation
limit, production pauses and resumes in the next batch — terrain and weapons are
prioritized first, then vehicles, characters, UI, and effects.


