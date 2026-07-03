# Blood Ring — Development Workflow

## Overview

This document defines the complete development workflow for Blood Ring Apex Royale, from local development to production deployment. The project follows a **trunk-based development** model with CI/CD automation.

---

## Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/favour187/AcademyRoyalebackend.git
cd AcademyRoyalebackend

# 2. Open in Unity 2022.3.50f1
#    File > Open Project > Select AcademyRoyalebackend folder

# 3. Install backend dependencies (for local backend testing)
cd backend && npm install && cd ..

# 4. Start local backend (optional)
cd backend && node server.js
# Backend runs at http://localhost:5000

# 5. Update server_config.txt for local development
echo "http://localhost:5000" > Assets/StreamingAssets/server_config.txt
```

---

## Branch Strategy

```
main ────────────────────────────────────────────── Production
 │
 ├── develop ──────────────────────────────────── Staging
 │    ├── feature/character-system ────────────── Feature branches
 │    ├── feature/weapon-balancing
 │    ├── fix/animation-glitch
 │    └── fix/backend-auth
 │
 └── hotfix/critical-crash ────────────────────── Emergency fixes
```

| Branch | Purpose | Deploys To |
|--------|---------|------------|
| `main` | Production-ready code | Render.com (backend), APK builds |
| `develop` | Integration testing | Staging environment |
| `feature/*` | Individual features | CI only |
| `fix/*` | Bug fixes | CI only |
| `hotfix/*` | Emergency patches | Fast-track to main |

---

## Daily Development Workflow

### 1. Start Your Day
```bash
git checkout develop
git pull origin develop
git checkout -b feature/your-feature-name
```

### 2. Make Changes
- Edit C# scripts in `Assets/Scripts/`
- Edit 3D models in `Assets/Resources/Models/`
- Edit audio in `Assets/Audio/`
- Edit animations in `Assets/Resources/Animation/`
- Edit backend in `backend/`

### 3. Test Locally
```bash
# C# structural lint
python3 Tools/check_csharp_compilation.py

# Start backend for API testing
cd backend && node server.js &
curl http://localhost:5000/

# In Unity: Window > General > Console (check for errors)
# In Unity: File > Build Settings > Build (test APK)
```

### 4. Commit and Push
```bash
git add -A
git commit -m "feat: describe your change"
git push origin feature/your-feature-name
```

### 5. Create Pull Request
- Target: `develop` branch
- CI pipeline runs automatically
- Wait for all checks to pass
- Request review

### 6. Merge and Deploy
- Merge PR to `develop` → staging tests
- Merge `develop` to `main` → production deploy

---

## CI/CD Pipeline

### Pipeline 1: CI — Lint, Validate, Test (`.github/workflows/ci.yml`)

**Triggers:** Every push and pull request to `main`/`develop`

| Job | What It Does | Failure Action |
|-----|-------------|----------------|
| `csharp-lint` | Runs `check_csharp_compilation.py` | Block merge |
| `asset-validation` | Validates OBJ files, WAV files, animation clips, orphaned assets | Block merge |
| `backend-test` | Starts backend, tests all API endpoints | Block merge |
| `structure-check` | Verifies required files, Unity version, backend config | Block merge |

### Pipeline 2: Backend Deploy (`.github/workflows/backend-deploy.yml`)

**Triggers:** Push to `main` that changes `backend/**`

1. Render.com auto-deploys from main branch
2. Workflow waits 30s, then smoke tests the live API
3. Tests auth, profile, store, missions endpoints

### Pipeline 3: Android Build (`.github/workflows/build-android.yml`)

**Triggers:** Manual dispatch only (requires Unity license)

1. Uses `game-ci/unity-builder` GitHub Action
2. Builds APK with `BuildScript.BuildAndroid`
3. Uploads APK as GitHub artifact
4. Validates APK size (warns >150MB, fails >500MB)

---

## Asset Pipeline

### 3D Models
```
Blender/Maya → Export as .obj → Assets/Resources/Models/{Category}/
                                   ├── Characters/
                                   ├── Weapons/
                                   ├── Vehicles/
                                   ├── Environment/
                                   └── Props/
```

**Requirements:**
- OBJ format with vertices, normals, UV coordinates
- Reasonable polygon count (<5000 verts for mobile)
- No embedded materials (use Unity materials)

### Audio
```
DAW/Recording → Export as .wav (16-bit, 44100Hz) → Assets/Audio/
                                                      ├── RealProduction/
                                                      ├── SFX/
                                                      └── VO/
```

**Requirements:**
- WAV format, 16-bit, 44100Hz
- Compressed MP3 for voice lines
- Max 5MB per file for mobile

### Animations
```
Mixamo/Blender → Export as .anim → Assets/Resources/Animation/
                                      ├── Player*.anim
                                      ├── AI*.anim
                                      └── Weapon*.anim
```

**Requirements:**
- Unity .anim format
- Assign clips to Animator Controller states
- Test in Play mode before committing

### Textures
```
Photoshop/Substance → Export as .png → Assets/Resources/Art/
                                          ├── Terrain/ (1024×1024 max)
                                          ├── UI/ (appropriate sizes)
                                          └── Effects/ (sprite sheets)
```

**Requirements:**
- Power-of-2 dimensions (256, 512, 1024)
- Max 2MB per texture for mobile
- Compress in Unity import settings

---

## Backend Development

### Local Development
```bash
cd backend
npm install
node server.js
# API at http://localhost:5000

# Update Unity to use local backend
echo "http://localhost:5000" > ../Assets/StreamingAssets/server_config.txt
```

### Production
```bash
# Backend auto-deploys to Render.com on push to main
# Live at: https://academyroyalebackend.onrender.com

# Update Unity to use production backend
echo "https://academyroyalebackend.onrender.com" > Assets/StreamingAssets/server_config.txt
```

### API Endpoints

| Category | Endpoint | Method | Auth |
|----------|----------|--------|------|
| Auth | `/api/auth/register` | POST | No |
| Auth | `/api/auth/login` | POST | No |
| Auth | `/api/auth/guest` | POST | No |
| Profile | `/api/profile` | GET | Yes |
| Profile | `/api/profile/character` | PUT | Yes |
| Store | `/api/store` | GET | Yes |
| Store | `/api/store/buy` | POST | Yes |
| Store | `/api/store/luckyspin` | POST | Yes |
| Store | `/api/store/daily` | POST | Yes |
| Store | `/api/store/missions` | GET | Yes |
| Match | `/api/match/matchmake` | POST | Yes |
| Match | `/api/match/result` | POST | Yes |
| Social | `/api/social/friends` | GET | Yes |
| Social | `/api/social/chat` | GET | Yes |
| Leaderboard | `/api/leaderboard` | GET | No |

---

## Release Workflow

### Alpha → Beta → Production

```
Alpha (internal testing)
  │
  ├── Fix bugs from alpha feedback
  ├── Balance weapons/characters
  ├── Optimize performance
  │
Beta (closed testing, 100-500 users)
  │
  ├── Collect crash reports
  ├── Monitor backend performance
  ├── Balance based on match data
  │
Production (public release)
  │
  ├── Google Play Store submission
  ├── Marketing push
  ├── LiveOps activation
```

### Version Numbering
```
Major.Minor.Patch (e.g., 1.2.3)
  Major: New season, major feature
  Minor: New characters, weapons, modes
  Patch: Bug fixes, balance changes
```

### APK Build Checklist
- [ ] All CI checks pass
- [ ] Backend deployed and healthy
- [ ] `server_config.txt` points to production
- [ ] `ProjectSettings.asset` has correct bundle ID
- [ ] Signing keystore configured
- [ ] App icon set (512×512)
- [ ] Version number incremented
- [ ] Test on real device
- [ ] Submit to Play Store

---

## File Structure

```
AcademyRoyalebackend/
├── .github/workflows/          # CI/CD pipelines
│   ├── ci.yml                  # Lint + validate + test
│   ├── backend-deploy.yml      # Backend deploy verification
│   └── build-android.yml       # APK build (manual)
├── Assets/
│   ├── Audio/                  # Sound effects, music, voice
│   ├── Editor/                 # Unity editor scripts
│   ├── Resources/              # Runtime-loaded assets
│   │   ├── Animation/          # Animator controllers + clips
│   │   ├── Art/3D/             # 3D models (OBJ)
│   │   ├── Art/Terrain/        # Terrain textures
│   │   ├── Art/UI/             # UI buttons, icons
│   │   └── Models/             # Duplicate 3D models (legacy)
│   ├── Scenes/                 # Unity scene files
│   ├── Scripts/                # C# source code
│   │   ├── AI/                 # Bot AI
│   │   ├── LiveOps/            # Battle pass, store, events
│   │   ├── Map/                # Terrain, POIs, interactive objects
│   │   ├── Player/             # Player controller
│   │   ├── Systems/            # Core game systems
│   │   ├── UI/                 # UI controllers
│   │   └── Weapons/            # Weapon data
│   └── StreamingAssets/        # Runtime config (server URL)
├── backend/                    # Node.js/Express API server
│   ├── controllers/            # Route handlers
│   ├── middleware/              # Auth, rate limiting
│   ├── routes/                 # API route definitions
│   └── server.js               # Entry point
├── Build/                      # Build output (gitignored)
├── Docs/                       # Documentation
├── Effects/                    # VFX sprite sheets
├── Packages/                   # Unity package manifest
├── ProjectSettings/            # Unity project settings
├── Tools/                      # Build/validation scripts
└── build_mobile3d_android.sh   # Local Android build script
```

---

## Common Tasks

### Add a New Character
1. Create 3D model → `Assets/Resources/Models/Characters/HeroName.obj`
2. Create animation clips → `Assets/Resources/Animation/HeroName*.anim`
3. Add to `CharacterData` ScriptableObject
4. Add to `CharacterSelectController` character list
5. Add to backend store if premium
6. Test in Unity Play mode

### Add a New Weapon
1. Create 3D model → `Assets/Resources/Models/Weapons/WeaponName.obj`
2. Create `WeaponData` ScriptableObject
3. Add weapon fire/reload sounds
4. Add to `LootSpawner` spawn table
5. Balance damage, fire rate, recoil
6. Test in Training Ground

### Add a New Vehicle
1. Create 3D model → `Assets/Resources/Models/Vehicles/VehicleName.obj`
2. Add to `Vehicles.cs` vehicle type enum
3. Configure speed, health, fuel
4. Add engine sound to `AudioManager`
5. Place on map in `MapGenerator`

### Fix a Bug
1. Reproduce the bug
2. Create branch: `git checkout -b fix/bug-description`
3. Fix the code
4. Test the fix
5. Commit: `git commit -m "fix: describe the fix"`
6. Push and create PR

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Unity won't compile | Run `python3 Tools/check_csharp_compilation.py` for hints |
| Backend not responding | Check Render dashboard, wait 60s for cold start |
| APK build fails | Check Unity Hub has Android Build Support installed |
| Audio not playing | Verify WAV files are valid: `file *.wav` |
| Models invisible | Check OBJ has `f` (face) entries, not just `v` |
| Animations T-pose | Verify `.anim` clips are assigned in Animator Controller |
| Store empty | Check `server_config.txt` URL, verify backend is live |
