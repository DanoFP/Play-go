#!/bin/bash
# Run from Desktop/RealmForge:  bash build_and_deploy.sh
set -e
PROJECT="$(cd "$(dirname "$0")" && pwd)"
WEBDIR="$PROJECT/WebBuild"

# 1. Unity batch build
UNITY_BASE="/Applications/Unity/Hub/Editor"
UNITY_VER=$(ls "$UNITY_BASE" | sort -V | tail -1)
UNITY="$UNITY_BASE/$UNITY_VER/Unity.app/Contents/MacOS/Unity"
echo "Building with Unity $UNITY_VER …"
"$UNITY" -batchmode -quit \
  -projectPath "$PROJECT" \
  -buildTarget WebGL \
  -executeMethod BuildScript.BuildWebGL \
  -logFile "$PROJECT/unity_build.log"

# 2. Fix .gz references in index.html
sed -i '' \
  -e 's|Build\.data\.gz|Build.data|g' \
  -e 's|Build\.framework\.js\.gz|Build.framework.js|g' \
  -e 's|Build\.wasm\.gz|Build.wasm|g' \
  "$WEBDIR/index.html"
echo "index.html fixed."

# 3. Git push
git -C "$PROJECT" push origin main

# 4. Vercel deploy
cd "$WEBDIR"
vercel --prod
echo "=== Deploy complete! ==="
