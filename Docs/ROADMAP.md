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
      crimson-cyan house style.
- [x] Weapons batch 3: full SMG class completed — MP40, P90, Mac10, Thompson,
      Bizon, MP5, CG15, MP9 — plus FAMAS and XM8. 22/50 weapons.
- [x] Weapons batch 4: full Assault Rifle class completed — AN94, AUG, Parafal,
      Kingfisher, G36, FAL — plus M1014, SPAS12, MAG7, ChargeBuster. 32/50 weapons.
- [x] Weapons batch 5: full Shotgun class completed — Trogon, Striker12 — plus
      M24, SVD, Woodpecker, AC80, M14, M500, USP, MiniUzi. 42/50 weapons.
- [x] **Weapons batch 6 (this session — 50/50 COMPLETE ✅):**
      TreatmentGun, Katana, Pan, Machete, Bat, Crossbow, M79, Gatling —
      all 8 remaining catalog weapons now have real AI-generated hero-shot art.
      **50/50 original weapons complete.**
- [x] **Weapons batch 7 — Catalog expansion to 62 weapons (this session):**
      Added 12 new original weapons: Tempest (energy AR), Spectre_AR (suppressed AR),
      Havoc (rapid-fire SMG), Razorback (PDW), BreachersSG (breaching shotgun),
      Thunderbolt (double-barrel), Valkyrie (railgun sniper), Phantom (suppressed DMR),
      Python (revolver), Stinger (auto-pistol), ArcBlade (energy melee),
      PulseRifle (energy rifle). Added `EnergyAmmo` ammo type. All weapons have
      `WeaponRarity` system (Common/Uncommon/Rare/Epic/Legendary) for loot generation.
      **52/62** catalog weapons have real hero-shot art. 10 remaining
      (Havoc, Razorback, BreachersSG, Thunderbolt, Valkyrie, Phantom, Python,
      Stinger, ArcBlade, PulseRifle) — art generation hit per-session limit;
      will complete in next session.

- [ ] Vehicles: complete ground/air/water set + skins
- [ ] Characters: hero roster portraits + outfits
- [ ] UI: icon set, buttons, HUD, frames, backgrounds
- [ ] Effects: smoke, explosion, weather, lighting sheets
- [ ] Scene backdrops: splash, lobby, character hub, battle map, results
- [ ] Throwables art: FragGrenade, SmokeGrenade, Flashbang, Molotov, StickyBomb — directories created, art pending
- [ ] Attachment icons: 24 attachments defined in code, icon art pending

### NEW: Weapon Systems (completed this session)
- [x] **AttachmentData.cs** — 24 weapon attachments across 7 slots (Scope, Muzzle,
      Magazine, Grip, Stock, Laser, Barrel). Stat modifiers: damage, spread,
      fire rate, reload, ammo, range, ADS speed. Compatibility checking per
      weapon category. Auto-apply on pickup.
- [x] **WeaponSkinData.cs** — Cosmetic skin system with 5 tiers (Standard/Deluxe/
      Premium/Legendary/Mythic). Universal skins + weapon-specific legendary skins
      (Dragon Scale, Arctic Wolf, Sakura Blade). Color palette, glow, kill effect,
      and trail effect flags. Unlock via BattlePass/Store/Event/Craft/Airdrop.
- [x] **EvoWeaponSystem.cs updated** — Now integrates weapon skins (apply/unlock),
      weapon attachments (equip/remove/compatible checking), attachment inventory,
      and BuildModifiedWeapon() for fully-modded weapon stats.
- [x] **LootSpawner.cs updated** — Rarity-weighted smart loot generation across
      all 62 weapons. Spawns attachments (25 per match), throwables (20 per match),
      and all 6 ammo types including EnergyAmmo. Rarity-colored loot sparkle effects.
- [x] **PlayerController.cs updated** — Pickup handler now supports Attachment
      (auto-equip or store) and Throwable pickup types.
- [x] **TouchControls.cs updated** — Throwable inventory management (add, count,
      consume, cycle).
- [x] **BloodRingArtLibrary.cs updated** — Added Throwable(), Attachment(),
      and WeaponSkin() convenience lookups.
- [x] **WeaponData.cs expanded** — 62 weapons, GetWeaponsByCategory(),
      GetWeaponsByRarity(), IsMelee(), IsSpecial() utility methods.

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


