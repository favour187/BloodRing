# 📱 Blood Ring Apex Royale 3D — Android APK Build Workflow & Deployment Guide

This document details the complete production workflows for compiling, signing, and deploying the standalone Android APK (`AcademyRoyale3D.apk`) for **Blood Ring Apex Royale 3D**.

The project is configured for Android API Level 26 (Android 8.0) through API Level 34 (Android 14) using the Universal Render Pipeline (URP) optimized for 60 FPS mobile rendering.

---

## 🛠️ Method 1: Automated CI/CD via GitHub Actions (Recommended for Teams)

We have integrated a GitHub Actions workflow (`.github/workflows/build_android_apk.yml`) powered by **GameCI (`game-ci/unity-builder@v4`)**.

### Automated Execution Loop:
1. **Trigger:** Automatically runs on git push to `main` or `production` branches, or via manual `workflow_dispatch`.
2. **QA Pre-Flight Gate:** Before launching Unity, the runner installs Node.js backend dependencies, verifies `server.js`, runs `equalize_all_assets.py` to balance the 30,000 asset database, and executes `qa_test_runner.py`.
3. **Unity Headless Compilation:** Spins up an isolated Ubuntu Android SDK/NDK container, loading the project and executing `MobileAndroidBuildConfigurator.BuildMobile3DAndroidApk`.
4. **Artifact Delivery:** The generated APK (`BloodRing_ApexRoyale3D_Android_APK`) is uploaded to the GitHub release run for immediate downloading and QA installation.

### Required Repository Secrets:
* `UNITY_LICENSE`: Your base64-encoded Unity license file (`.ulf`).
* `KEYSTORE_PASS` & `KEYALIAS_PASS`: Android release keystore passwords for production APK signing.

---

## 🐳 Method 2: One-Click Local Docker Container Build

For developers working on Linux, macOS, or Windows machines without local Unity Editor installations, use our automated Docker build script:

```bash
# Make script executable and run
chmod +x Tools/docker_build_apk.sh
./Tools/docker_build_apk.sh
```

### What This Script Does:
* Pulls the official `unityci/editor:ubuntu-2022.3.20f1-android-3.0.1` container image with pre-configured Android NDK r23b and OpenJDK 17.
* Mounts your local project workspace into `/project`.
* Executes Unity batchmode compilation in isolation.
* Exports the finished signed APK directly to `build/Android/AcademyRoyale3D.apk`.

---

## 🖥️ Method 3: Command-Line Headless Build (Linux / macOS / CI CI Runner)

If you have Unity installed locally (or set via `UNITY_PATH`), execute our automated root build script:

```bash
export UNITY_PATH=/opt/Unity/Editor/Unity
chmod +x build.sh
./build.sh
```

This script executes:
1. `generate_assets_pipeline.py` & `equalize_all_assets.py` (Asset manifest sync).
2. `qa_test_runner.py` (Full 7-gate verification).
3. Unity `-executeMethod MobileAndroidBuildConfigurator.BuildMobile3DAndroidApk`.
4. Emits release metadata to `Build/release_manifest.json`.

---

## 🎮 Method 4: Standard Unity Editor UI Export

To export the APK directly from the Unity Editor graphical interface:
1. Open the project in **Unity 2022.3 LTS or newer**.
2. Navigate to **File $\rightarrow$ Build Settings**.
3. Select **Android** in the Platform list and click **Switch Platform**.
4. In the top menu bar, click **Blood Ring $\rightarrow$ Build Android APK (Production 3D)** or execute **Build And Run**.
5. The Editor will compile C# scripts, bundle all 34 equalized asset directories from `Resources/`, apply mobile URP shaders, and export the `.apk` file to `build/Android/AcademyRoyale3D.apk`.

---

## 📋 Android APK Build Profile Specification

| Setting Attribute | Configured Value | Description |
| :--- | :--- | :--- |
| **Package Name / Bundle ID** | `com.bloodring.apexroyale3d` | Unique Android application identifier. |
| **Minimum API Level** | API Level 26 (Android 8.0 Oreo) | Ensures compatibility with 98%+ of active Android mobile devices. |
| **Target API Level** | API Level 34 (Android 14) | Meets Google Play Store 2026 security & performance standards. |
| **Scripting Backend** | `IL2CPP` | Compiles IL C# to native ARM64 C++ code for maximum FPS and anti-cheat protection. |
| **Target Architectures** | `ARM64` & `ARMv7` | Supports modern 64-bit gaming phones and mid-tier 32-bit hardware. |
| **Graphics API** | `Vulkan` (Primary), `OpenGLES3` (Fallback) | Eliminates driver draw-call overhead on Snapdragon & MediaTek GPUs. |
| **Frame Rate Enforcer** | `Application.targetFrameRate = 60;` | Enforces 60 FPS cap with `SleepTimeout.NeverSleep` to prevent dimming. |
| **Asset Bundling** | `Resources/` & `StreamingAssets/` | All 1,015+ physical equalized assets and 30,000 DB records packaged offline. |
