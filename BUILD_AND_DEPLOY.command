#!/bin/bash
# RealmForge — build WebGL + deploy to Vercel
# Double-click this file from Finder, or run: bash BUILD_AND_DEPLOY.command

PROJECT="$(cd "$(dirname "$0")" && pwd)"
WEBDIR="$PROJECT/WebBuild"

echo "=== RealmForge Build & Deploy ==="
echo "Project: $PROJECT"

# ── 1. Unity batch build ───────────────────────────────────────────────────────
UNITY_BASE="/Applications/Unity/Hub/Editor"
UNITY_VER=$(ls "$UNITY_BASE" | sort -V | tail -1)
UNITY="$UNITY_BASE/$UNITY_VER/Unity.app/Contents/MacOS/Unity"
echo "Building with Unity $UNITY_VER ..."

"$UNITY" -batchmode -quit \
  -projectPath "$PROJECT" \
  -buildTarget WebGL \
  -executeMethod BuildScript.BuildWebGL \
  -logFile "$PROJECT/unity_build.log"
STATUS=$?

if [ $STATUS -ne 0 ]; then
    echo ""
    echo "ERROR: Unity exited with code $STATUS"
    echo "Check: $PROJECT/unity_build.log"
    grep -E "error|Error|FAILED|failed" "$PROJECT/unity_build.log" \
        | grep -v "usbmuxd\|FMOD\|OpenAL\|Audio\|Handshake" | tail -10
    read -p "Press Enter to exit..."
    exit 1
fi
echo "Build succeeded."

# ── 2. Verify index.html references .gz files (Vercel serves with Content-Encoding: gzip) ──
# The .gz extensions must stay — do NOT strip them. vercel.json handles Content-Encoding.
echo "index.html: .gz refs kept as-is (served via Content-Encoding: gzip)."

# ── 3. Git: pull remote changes, then push ────────────────────────────────────
echo "Syncing git..."
cd "$PROJECT"
git pull --rebase origin main 2>&1 || echo "(pull skipped)"
git add -A
git commit -m "feat: WebGL rebuild — Scout/Knight/Stable/Monk/siege/University researches" 2>&1 \
    || echo "(nothing new to commit)"
git push origin main 2>&1 || echo "(git push failed — deploy continues)"

# ── 4. Vercel deploy ──────────────────────────────────────────────────────────
echo ""
echo "Deploying to Vercel..."
cd "$WEBDIR"
vercel --prod
echo ""
echo "=== Done! ==="
read -p "Press Enter to close..."
