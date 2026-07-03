#!/usr/bin/env bash
# ──────────────────────────────────────────────────────────────────────
#  Blood Ring — Android Signing Keystore Generator
#  Run this ONCE to create your signing keystore.
#  DO NOT commit the keystore file to git!
# ──────────────────────────────────────────────────────────────────────

set -euo pipefail

KEYSTORE_FILE="user.keystore"
KEY_ALIAS="bloodring"
STORE_PASSWORD="${1:-bloodring2026}"
KEY_PASSWORD="${2:-bloodring2026}"

echo "========================================================="
echo "  Blood Ring — Android Signing Keystore Generator"
echo "========================================================="
echo ""
echo "  This will create a signing keystore for Android builds."
echo "  Keep this file secure — it's needed for all future updates."
echo ""

if [ -f "$KEYSTORE_FILE" ]; then
    echo "  ⚠️  $KEYSTORE_FILE already exists!"
    echo "  Delete it first if you want to regenerate."
    exit 1
fi

# Generate keystore
keytool -genkey -v \
    -keystore "$KEYSTORE_FILE" \
    -alias "$KEY_ALIAS" \
    -keyalg RSA \
    -keysize 2048 \
    -validity 10000 \
    -storepass "$STORE_PASSWORD" \
    -keypass "$KEY_PASSWORD" \
    -dname "CN=Blood Ring Studio, OU=GameDev, O=Blood Ring Studio, L=Lagos, ST=FC, C=NG"

echo ""
echo "========================================================="
echo "  ✅ Keystore created: $KEYSTORE_FILE"
echo "  Alias: $KEY_ALIAS"
echo "  Store password: $STORE_PASSWORD"
echo "  Key password: $KEY_PASSWORD"
echo ""
echo "  ⚠️  IMPORTANT:"
echo "  1. Keep this keystore file secure and backed up"
echo "  2. NEVER commit it to git (add to .gitignore)"
echo "  3. If you lose it, you cannot update your app on Play Store"
echo "  4. Set UNITY_KEYSTORE_PASS and UNITY_KEY_PASS in GitHub Secrets"
echo "========================================================="
