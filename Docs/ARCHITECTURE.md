# Blood Ring — Architecture

## 1. High-level

Blood Ring is a client/server mobile battle royale. The Unity URP client renders
and predicts; a dedicated Node.js-backed services layer handles identity, economy,
matchmaking and persistence; authoritative match simulation runs on dedicated game
servers. A modular, data-driven design keeps gameplay systems decoupled from one
another and from the underlying asset layout.

```
 ┌────────────┐      HTTPS / WSS      ┌──────────────┐
 │  Client    │◄────────────────────►│  Backend     │  auth, profile, store,
 │  (Unity)   │                      │  (Node.js)   │  leaderboard, social, match
 └─────┬──────┘                      └──────┬───────┘
       │ UDP (replication)                  │
       ▼                                     ▼
 ┌────────────┐                       ┌──────────────┐
 │ Dedicated  │  authoritative sim    │  Data stores │  profiles, inventory,
 │ Game Server│◄─────────────────────│  (DB / cache)│  telemetry, live config
 └────────────┘                       └──────────────┘
```

## 2. Module map

| Layer | Folder | Responsibility |
|-------|--------|----------------|
| Client shell | `/Client` | Build profiles, platform entry, quality tiers |
| Game code | `/Assets/Scripts` | Player, weapons, AI, UI, systems |
| Art | `/Assets/Resources/Art` | Authored PNG art, loaded via `BloodRingArtLibrary` |
| Audio | `/Assets/Resources/Audio` | Music, SFX, VO |
| Server | `/Server` | Dedicated server config, simulation tick, zone authority |
| Networking | `/Networking` | Transport, matchmaking, replication, anti-cheat |
| LiveOps | `/LiveOps` | Events, battle pass, store schedules, remote config |
| Analytics | `/Analytics` | Telemetry event schema, dashboards |
| Backend | `/backend` | Node.js REST services |
| Tools | `/Tools` | Asset validate/manifest, deploy, monitoring |
| Generated | `/Generated` | Machine-built manifests (never hand-edited) |

## 3. Gameplay systems (data-driven)

Each system is a self-contained C# module under `Assets/Scripts/Systems`, driven by
`ScriptableObject`/JSON data rather than hard-coded values:

- Battle royale loop, zone/storm controller, matchmaking, loading
- Aircraft phase, parachute landing, gameplay, spectator, replay
- Inventory, loot spawning, weapons, vehicles
- Ranking, clans/factions, events, missions, cosmetics
- Bounty, talents, traps, barricades, throwables, emotes, pets, ping, revive

## 4. Art subsystem

- **Single entry point:** `BloodRing.Art.BloodRingArtLibrary` (cached `Resources` loads).
- **Backdrops:** `SceneArtBackdrop` blits a scene's authored texture behind the UI.
- **No runtime art generation.** The old `ProceduralArt` path is removed; CI rejects
  any reintroduction of code-based art generators (see `.github/workflows/ci.yml`).
- **Indexing:** `Tools/build_manifest.py` → `Generated/asset_manifest.json`.
- **Validation:** `Tools/validate_assets.py` fails CI on empty/placeholder art.

## 5. Performance strategy

LOD meshes, texture streaming + ASTC compression, object pooling for projectiles/VFX,
occlusion culling, GPU instancing for vegetation/props, and a 3-tier quality scaler
(Low/Med/High) selected from device profile at boot. Target 60 FPS, 30 FPS fallback.

## 6. Networking & anti-cheat

Client-side prediction with server reconciliation; snapshot interpolation for remote
entities; server-authoritative hit validation; regional matchmaking pools; encrypted
transport. Replays are deterministic command logs replayed by the spectator/replay
system.


