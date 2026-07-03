# Blood Ring 3D Mobile Android Client

This repository has been upgraded with a runtime-generated 3D mobile battle-royale presentation layer while preserving the existing project structure and files.

## What was added

- `Assets/Scripts/Systems/MobileBattleRoyaleKit.cs`
  - Premium 3D Splash / Logo scene
  - Startup / Initialization scene
  - Login scene with guest/online flow buttons
  - Premium 3D Loading scene
  - Main Lobby command hub
  - Events page
  - Store scene with 3D weapon presentation
  - Character page
  - Inventory scene with 3D loadout presentation
  - Settings scene
  - Matchmaking scene
  - Waiting Island scene
  - Main Battle Royale map scene
  - Training Ground scene
  - Result / Victory screen
  - Clan / Social scene
  - Profile scene
  - Reconnect / Error scene
  - Mobile safe-area handling
  - Android Back button handling
  - Animated touch-friendly world-space buttons
  - Mobile HUD control overlay
  - Runtime LOD assignment for generated 3D objects
  - Runtime object pools for projectiles/effects
  - Mobile performance defaults

- `Assets/Editor/MobileAndroidBuildConfigurator.cs`
  - One-click Android configuration
  - Batchmode build method
  - Android package settings
  - IL2CPP + ARMv7/ARM64
  - Landscape mobile UX settings
  - Generated client config in `Assets/StreamingAssets/mobile_client_config.json`

## Build in Unity

Use Unity `2022.3.50f1` or compatible 2022.3 LTS.

1. Open the project folder in Unity.
2. Let Unity import packages.
3. Select **Build > Configure Mobile 3D Android**.
4. Select **Build > Build Mobile 3D Android APK**.
5. APK output: `build/Android/BloodRing3D.apk`.

## Build from command line

```bash
/opt/Unity/Editor/Unity \
  -projectPath . \
  -quit -batchmode \
  -buildTarget Android \
  -executeMethod MobileAndroidBuildConfigurator.BuildMobile3DAndroidApk \
  -logFile build-mobile3d.log
```

Or run:

```bash
./build_mobile3d_android.sh
```

Set `UNITY_PATH` if Unity is installed somewhere else.

## Notes

- Visual style is a modern mobile battle-royale look inspired by the genre only. No third-party assets, trademarks, layouts, branding, or proprietary materials are copied.
- Existing assets in `Assets/Resources` are used first where available, including OBJ models and textures.
- All requested screens are backed by included project assets or generated original 3D art; missing optional mesh references are covered by procedural original 3D models so the scene remains complete.
- Gameplay remains 3D. UI is overlay/world-space only for navigation and controls.
- Existing scenes are preserved; the upgrade layer injects 3D content at runtime so scene files are not destroyed or replaced.

## Performance checklist included

- Target 60 FPS with disabled vSync.
- Conservative shadow distance and cascade count.
- LODGroups assigned to generated/presented objects.
- Static flags on generated environment props.
- Object pools for frequent gameplay objects.
- Mobile-safe shaders through `ProceduralArt.GetSafeShader`.
- Safe-area fitting for Android notches/cutouts.
- Landscape mobile orientation.

## Main build scenes

The build now includes the requested premium scene flow:

1. `SplashLogo`
2. `StartupInitialization`
3. `LoginScene`
4. `LoadingScene`
5. `MainLobby`
6. `EventsPage`
7. `StoreScene`
8. `CharacterPage`
9. `InventoryScene`
10. `SettingsScene`
11. `MatchmakingScene`
12. `WaitingIsland`
13. `MainBattleRoyaleMap`
14. `TrainingGround`
15. `ResultVictoryScreen`
16. `ClanSocial`
17. `ProfileScene`
18. `ReconnectErrorScene`

Legacy compatibility scenes are also kept in build settings: `SplashScreen`, `MainMenu`, `CharacterSelect`, `LobbyScene`, `GameScene`, and `GameOver`.

## Final premium art pass

Added `Assets/Scripts/Systems/HighPremiumOriginal3DArtPack.cs` for a higher quality original 3D art layer across every requested scene. It creates premium mobile battle-royale style art direction while avoiding any copied third-party assets, exact interfaces, trademarks, or proprietary layouts.

Coverage includes:

- Original armored humanoid/agent models
- Premium lobby champion display
- Drop ship
- Tactical vehicle prop
- 3D store weapon showcases
- 3D inventory armory wall
- 3D backpack and equipment props
- Startup/auth/loading data cores
- Events totems
- Clan/social hall elements
- Profile badge wall
- Battle royale island map art
- Training target range
- Victory stage
- Reconnect/error signal art
- Hologram-styled world-space UI panels
- Neon runway, skyline, lights, rings, crates, and map props

The visual direction is modern mobile battle royale, but the theme, geometry, UI panels, characters, props, and effects are original to Blood Ring.


