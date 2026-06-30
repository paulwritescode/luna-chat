#!/usr/bin/env bash
set -euo pipefail

# Build self-contained macOS binaries for luna-chat (arm64 + x64).
# Run from the repo root:  ./build/build-mac.sh

cd "$(dirname "$0")/.."

GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

PROJECT="luna-chat.csproj"

publish() {
  local rid="$1"
  local out="./publish/$rid"
  echo -e "${GREEN}==> Publishing $rid${NC}"
  dotnet publish "$PROJECT" \
    -c Release \
    -r "$rid" \
    --self-contained true \
    -p:UseAppHost=true \
    -o "$out"
}

publish osx-arm64
publish osx-x64

echo -e "${GREEN}==> Done.${NC}"
echo "Outputs:"
echo "  ./publish/osx-arm64"
echo "  ./publish/osx-x64"
echo ""
echo "Optional: create a universal binary with lipo, e.g."
echo "  lipo -create -output ./publish/mac-universal/LunaChat \\"
echo "    ./publish/osx-arm64/LunaChat ./publish/osx-x64/LunaChat"
echo ""
echo "Optional: package an installer with Velopack (vpk) once installed:"
echo "  dotnet tool install -g vpk"
echo "  vpk pack --packId dev.paul.lunachat --packVersion 1.0.0 \\"
echo "    --packDir ./publish/osx-arm64 --mainExe LunaChat --outputDir ./dist/mac"
