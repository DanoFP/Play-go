#!/bin/bash
# RealmForge — build WebGL and deploy to Vercel
# Usage: ./deploy.sh
# Run from inside the RealmForge project root

set -e

PROJECT="$(cd "$(dirname "$0")" && pwd)"
WEBDIR="$PROJECT/WebBuild"
BUILD="$WEBDIR/Build"

echo "=== RealmForge Deploy ==="
echo "Project: $PROJECT"

# ── 1. Locate Unity ────────────────────────────────────────────────────────────
UNITY_HUB="/Applications/Unity/Hub/Editor"
# Pick newest installed version
UNITY_VERSION=$(ls "$UNITY_HUB" 2>/dev/null | sort -V | tail -1)
if [ -z "$UNITY_VERSION" ]; then
    echo "ERROR: Unity Hub not found at $UNITY_HUB"
    exit 1
fi
UNITY="$UNITY_HUB/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"
echo "Unity: $UNITY ($UNITY_VERSION)"

# ── 2. Build WebGL ─────────────────────────────────────────────────────────────
echo ""
echo "--- Building WebGL (this takes 7-15 min)..."
"$UNITY" \
    -batchmode \
    -quit \
    -projectPath "$PROJECT" \
    -buildTarget WebGL \
    -executeMethod BuildScript.BuildWebGL \
    -logFile "$PROJECT/unity_build.log" 2>&1 &
BUILD_PID=$!

# tail the log while waiting
sleep 3
echo "Build log: $PROJECT/unity_build.log"
tail -f "$PROJECT/unity_build.log" &
TAIL_PID=$!
wait $BUILD_PID
kill $TAIL_PID 2>/dev/null || true
echo "Build process finished."

# Check log for success/failure
if grep -q "Build succeeded" "$PROJECT/unity_build.log" 2>/dev/null || \
   grep -q "WebGL build succeeded" "$PROJECT/unity_build.log" 2>/dev/null; then
    echo "✓ Build succeeded"
elif grep -q "Build failed\|BuildResult.Failed\|build failed" "$PROJECT/unity_build.log" 2>/dev/null; then
    echo "✗ Build FAILED — check $PROJECT/unity_build.log"
    exit 1
fi

# ── 3. Fix index.html (.gz refs → plain filenames) ────────────────────────────
INDEXFILE="$WEBDIR/index.html"
if [ -f "$INDEXFILE" ]; then
    echo ""
    echo "--- Fixing index.html .gz references..."
    sed -i.bak \
        -e 's|Build\.data\.gz|Build.data|g' \
        -e 's|Build\.framework\.js\.gz|Build.framework.js|g' \
        -e 's|Build\.wasm\.gz|Build.wasm|g' \
        "$INDEXFILE"
    echo "✓ index.html fixed"
fi

# ── 4. Git commit + push ───────────────────────────────────────────────────────
echo ""
echo "--- Committing to git (branch: realmforge)..."
cd "$PROJECT"
git add -A
git commit -m "feat: add Scout, Knight, Stable, Monk, siege units, University researches" || echo "(nothing to commit)"
git push origin realmforge || echo "Push failed — check remote"
echo "✓ Git push done"

# ── 5. Deploy to Vercel ────────────────────────────────────────────────────────
echo ""
echo "--- Deploying to Vercel..."
cd "$WEBDIR"
if command -v vercel >/dev/null 2>&1; then
    vercel --prod
else
    echo "vercel CLI not found — trying npx..."
    npx vercel --prod
fi
echo "✓ Deployed!"
echo ""
echo "=== Done ==="
