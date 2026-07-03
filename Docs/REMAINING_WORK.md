# Blood Ring — Honest Remaining-Work Punch List

> This file replaces the marketing-grade "100% complete" claims. It reflects what
> was **independently verified** vs. what is still outstanding. Update it every
> milestone; do not inflate it.

## ✅ Done & verified this session (Project Configuration & Honesty Pass)
- **Backend fully functional** — Node/Express + SQLite. Tested end-to-end:
  register/login/guest, profile, store, leaderboard, matchmake (HOST/JOIN),
  match/result reward math, missions. Client `BackendAPI.cs` endpoints match the
  server routes 1:1.
- **Unity ProjectSettings authored** — the project previously had only
  `ProjectVersion.txt`. Added `ProjectSettings.asset` (bundle id
  `com.bloodring.royale`, landscape, minSdk 24, ARM64, IL2CPP, Linear color),
  `EditorBuildSettings.asset` (31 scenes, boot scene first), `TagManager`,
  `InputManager`, `TimeManager`, `QualitySettings`, `GraphicsSettings`,
  `DynamicsManager`, `Physics2DSettings`, `AudioManager`, `NavMeshAreas`,
  `EditorSettings`, and more.
- **Deterministic `.meta` files** — the repo shipped with **zero** meta files
  (random GUIDs on every clone). Added `Tools/generate_meta_files.py` and
  generated 760+ stable metas so GUIDs are version-controlled.
- **Animator Controllers** — added `PlayerAnimator`, `AIAnimator`,
  `WeaponAnimator` with the exact parameters the code sets, and wired them into
  `PlayerController`/`AIBot` at runtime so calls resolve.
- **Honest tooling** — `check_csharp_compilation.py` is now a truthful
  *structural lint* (and clearly says it is not a compiler);
  `build_mobile3d_android.sh` now **fails loudly** instead of touching a fake
  `.apk.placeholder` and printing "BUILD COMPLETE".

## ❌ Still outstanding (blocking a real shippable build)
1. **Real Unity compilation** — cannot be verified without the editor. Run
   `Unity -batchmode -quit -projectPath . -logFile build.log` on
   2022.3.50f1 and fix whatever the real compiler reports.
2. **Scene component wiring** — because the project historically had no metas,
   scene references to the original author's script GUIDs are unrecoverable and
   must be re-linked in the editor. New deterministic metas prevent this from
   recurring.
3. **3D content** — meshes are placeholder primitive `.obj` boxes/spheres. Needs
   real modeled/textured/rigged characters, weapons, vehicles.
4. **Animation clips** — controllers have no `.anim` motion yet (needs a rig).
5. **Audio integration** — 140 audio files exist but need import settings +
   AudioSource/mixer wiring verified in-editor.
6. **LiveOps** — `StoreRotationManager` is still a hardcoded stub.
7. **Automated tests** — no real unit/integration/network/damage-math tests.
8. **Store readiness** — icons, AndroidManifest permission review, signing
   keystore, IARC/ESRB/PEGI content rating for a violent shooter.

## Process rules going forward
- Never let a self-reported script assert "PASS"/"100%" as proof. Only the Unity
  editor's Editor.log proves compilation; only a non-empty signed APK proves a build.
- Always commit `.meta` files (Editor is set to "Visible Meta Files").
