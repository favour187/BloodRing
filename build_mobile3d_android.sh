#!/usr/bin/env bash
set -euo pipefail

# ──────────────────────────────────────────────────────────────────────
#  Blood Ring — Android APK Build Script (Unity 2022.3.50f1)
#  Usage: ./build_mobile3d_android.sh [--headless]
#  Requires: Unity 2022.3.50f1 installed and on PATH or at a known location
# ──────────────────────────────────────────────────────────────────────

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR"
BUILD_DIR="$PROJECT_DIR/Build/Android"
APK_NAME="BloodRing_ApexRoyale3D_Client_v1.0.0.apk"
LOG_FILE="$BUILD_DIR/build_log.txt"

# ── Locate Unity Editor ──────────────────────────────────────────────
UNITY_EDITOR=""

# Check common install locations
for candidate in \
    "/Applications/Unity/Hub/Editor/2022.3.50f1/Editor/Unity" \
    "$HOME/Unity/Hub/Editor/2022.3.50f1/Editor/Unity" \
    "C:/Program Files/Unity/Hub/Editor/2022.3.50f1/Editor/Unity.exe" \
    "$(which unity 2>/dev/null || true)" \
    "$(which Unity 2>/dev/null || true)"
do
    if [ -n "$candidate" ] && [ -x "$candidate" ]; then
        UNITY_EDITOR="$candidate"
        break
    fi
done

# ── FAIL LOUDLY if Unity not found ───────────────────────────────────
if [ -z "$UNITY_EDITOR" ]; then
    echo "========================================================="
    echo "  ❌ BUILD FAILED — Unity 2022.3.50f1 not found"
    echo "========================================================="
    echo ""
    echo "  Unity 2022.3.50f1 is required but was not found on this"
    echo "  system. Please install it via Unity Hub and ensure the"
    echo "  editor executable is accessible."
    echo ""
    echo "  Expected locations checked:"
    echo "    - /Applications/Unity/Hub/Editor/2022.3.50f1/Editor/Unity"
    echo "    - \$HOME/Unity/Hub/Editor/2022.3.50f1/Editor/Unity"
    echo "    - C:/Program Files/Unity/Hub/Editor/2022.3.50f1/Editor/Unity.exe"
    echo ""
    echo "  Alternatively, set UNITY_EDITOR_PATH environment variable:"
    echo "    export UNITY_EDITOR_PATH=/path/to/Unity"
    echo "    ./build_mobile3d_android.sh"
    echo "========================================================="
    exit 1
fi

# Allow environment variable override
if [ -n "${UNITY_EDITOR_PATH:-}" ]; then
    UNITY_EDITOR="$UNITY_EDITOR_PATH"
fi

echo "========================================================="
echo "  Blood Ring — Android APK Build"
echo "  Unity: $UNITY_EDITOR"
echo "  Project: $PROJECT_DIR"
echo "========================================================="

mkdir -p "$BUILD_DIR"
rm -f "$BUILD_DIR/$APK_NAME"
rm -f "$BUILD_DIR/$APK_NAME.placeholder"

echo "[$(date)] Starting Unity build..." | tee "$LOG_FILE"

# ── Run Unity batch-mode build ───────────────────────────────────────
"$UNITY_EDITOR" \
    -batchmode \
    -nographics \
    -quit \
    -projectPath "$PROJECT_DIR" \
    -buildTarget Android \
    -executeMethod BuildScript.BuildAndroid \
    -logFile "$LOG_FILE" \
    2>&1

EXIT_CODE=$?

if [ $EXIT_CODE -ne 0 ]; then
    echo "========================================================="
    echo "  ❌ BUILD FAILED (exit code $EXIT_CODE)"
    echo "  Check build log: $LOG_FILE"
    echo "========================================================="
    exit $EXIT_CODE
fi

# ── Verify APK was created ───────────────────────────────────────────
if [ ! -f "$BUILD_DIR/$APK_NAME" ]; then
    echo "========================================================="
    echo "  ❌ BUILD FAILED — APK not found at:"
    echo "     $BUILD_DIR/$APK_NAME"
    echo "  Check build log: $LOG_FILE"
    echo "========================================================="
    exit 1
fi

APK_SIZE=$(du -h "$BUILD_DIR/$APK_NAME" | cut -f1)

echo "========================================================="
echo "  ✅ BUILD SUCCESSFUL"
echo "  APK: $BUILD_DIR/$APK_NAME ($APK_SIZE)"
echo "  Log: $LOG_FILE"
echo "========================================================="
