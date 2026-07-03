#!/bin/bash
set -euo pipefail

echo "================================================================================"
echo "      BLOOD RING APEX ROYALE 3D — DOCKER CONTAINER ANDROID APK BUILDER"
echo "================================================================================"

UNITY_VERSION=${UNITY_VERSION:-2022.3.20f1}
DOCKER_IMAGE="unityci/editor:ubuntu-${UNITY_VERSION}-android-3.0.1"

echo "[1/3] Ensuring local build output directories exist..."
mkdir -p build/Android Build/Android Generated

echo "[2/3] Pulling Unity Android build container ($DOCKER_IMAGE)..."
if ! command -v docker &> /dev/null; then
  echo "Docker executable not found! Falling back to local ./build.sh..."
  ./build.sh
  exit 0
fi

echo "[3/3] Running headless Unity Android APK build inside isolated container..."
docker run --rm \
  -v "$(pwd):/project" \
  -w /project \
  -e UNITY_LICENSE="${UNITY_LICENSE:-}" \
  "$DOCKER_IMAGE" \
  unity-editor \
    -projectPath /project \
    -quit -batchmode \
    -buildTarget Android \
    -executeMethod MobileAndroidBuildConfigurator.BuildMobile3DAndroidApk \
    -logFile build/Android/docker_build.log

echo "================================================================================"
echo "APK build finished! Verify output at build/Android/AcademyRoyale3D.apk"
echo "================================================================================"
