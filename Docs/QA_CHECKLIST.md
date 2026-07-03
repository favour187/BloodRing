# Blood Ring — QA Checklist

## Build & boot
- [ ] Android build compiles (URP, IL2CPP, ARM64).
- [ ] Cold boot to main menu < 8s on mid-range device.
- [ ] No missing-asset (pink/magenta) materials in any scene.

## Art integrity
- [ ] `python3 Tools/validate_assets.py` passes (no empty/placeholder art).
- [ ] `Generated/asset_manifest.json` regenerated and committed.
- [ ] All `BloodRingArtLibrary` lookups resolve (no null sprite warnings in log).
- [ ] No runtime code-generated textures (search: `new Texture2D(` in gameplay paths).

## Game flow (20 screens)
- [ ] Splash → Auth → Account → Intro → Lobby reachable.
- [ ] Character Hub, Inventory, Store, Battle Pass, Events open and close cleanly.
- [ ] Matchmaking → Loading → Aircraft → Landing → Gameplay transitions work.
- [ ] Spectator, Results, Statistics, Rankings, Settings functional.

## Gameplay
- [ ] Movement joystick, sprint, fire/aim/reload, jump/crouch/prone respond.
- [ ] Weapon switch, interact, heal, utility usable from HUD.
- [ ] Zone/storm shrinks and damages outside the ring.
- [ ] Loot pickup, inventory equip, vehicles drivable.

## Performance
- [ ] Sustained 60 FPS on target device; 30 FPS fallback engages on low-end.
- [ ] Memory under budget; no leak across 5 consecutive matches.
- [ ] Battery/thermal acceptable over a 15-minute session.

## Networking
- [ ] Reconnect after drop restores match state.
- [ ] Server-authoritative hits; no client-side damage exploits.
- [ ] Regional matchmaking routes to nearest pool.

## Backend
- [ ] Auth, profile, store, leaderboard, social, match endpoints return expected payloads.
- [ ] Rate limiter active; invalid tokens rejected.


