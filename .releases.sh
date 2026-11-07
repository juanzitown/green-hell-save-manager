#!/usr/bin/env bash

set -Eeuo pipefail

readonly PROJECT_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$PROJECT_ROOT"

usage() {
  cat <<'EOF'
Usage: ./.releases.sh <linux|windows>

Builds the desktop installer, pushes the current branch and version tag, and
creates or updates the matching GitHub Release.

Examples:
  ./.releases.sh linux
  ./.releases.sh windows  # Run on Windows from Git Bash
EOF
}

fail() {
  echo "Release failed: $*" >&2
  exit 1
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "Required command '$1' was not found."
}

readonly PLATFORM="${1:-}"
if [[ "$PLATFORM" != "linux" && "$PLATFORM" != "windows" ]]; then
  usage
  exit 1
fi

require_command git
require_command gh
require_command node
require_command yarn

readonly PACKAGE_VERSION="$(node -p "require('./package.json').version")"
readonly TAURI_VERSION="$(node -p "require('./src-tauri/tauri.conf.json').version")"
readonly CARGO_VERSION="$(awk '
  /^\[package\]$/ { in_package = 1; next }
  in_package && /^version = / {
    sub(/^version = "/, "")
    sub(/"$/, "")
    print
    exit
  }
' src-tauri/Cargo.toml)"

[[ -n "$PACKAGE_VERSION" ]] || fail "package.json does not contain a version."
[[ "$PACKAGE_VERSION" == "$TAURI_VERSION" ]] ||
  fail "package.json ($PACKAGE_VERSION) and tauri.conf.json ($TAURI_VERSION) versions differ."
[[ "$PACKAGE_VERSION" == "$CARGO_VERSION" ]] ||
  fail "package.json ($PACKAGE_VERSION) and Cargo.toml ($CARGO_VERSION) versions differ."

if ! git diff --quiet || ! git diff --cached --quiet; then
  fail "Commit your changes before creating a release."
fi

readonly BRANCH="$(git branch --show-current)"
[[ -n "$BRANCH" ]] || fail "Releases cannot be created from a detached HEAD."

readonly TAG="v$PACKAGE_VERSION"
readonly HEAD_COMMIT="$(git rev-parse HEAD)"
local_tag_exists=false
remote_tag_exists=false

gh auth status >/dev/null 2>&1 ||
  fail "GitHub CLI is not authenticated. Run: gh auth login -h github.com -p https -w"

if git rev-parse --verify --quiet "refs/tags/$TAG" >/dev/null; then
  readonly TAG_COMMIT="$(git rev-list -n 1 "$TAG")"
  [[ "$TAG_COMMIT" == "$HEAD_COMMIT" ]] ||
    fail "$TAG already points to another commit. Increase the app version first."
  local_tag_exists=true
fi

readonly REMOTE_TAG_REFS="$(
  git ls-remote --tags origin "refs/tags/$TAG" "refs/tags/$TAG^{}"
)"

if [[ -n "$REMOTE_TAG_REFS" ]]; then
  remote_tag_exists=true
  REMOTE_TAG_COMMIT="$(
    awk '$2 ~ /\^\{\}$/ { print $1; exit }' <<<"$REMOTE_TAG_REFS"
  )"
  if [[ -z "$REMOTE_TAG_COMMIT" ]]; then
    REMOTE_TAG_COMMIT="$(awk 'NR == 1 { print $1 }' <<<"$REMOTE_TAG_REFS")"
  fi
  readonly REMOTE_TAG_COMMIT
  [[ "$REMOTE_TAG_COMMIT" == "$HEAD_COMMIT" ]] ||
    fail "Remote $TAG already points to another commit. Increase the app version first."
fi

echo "Building Green Hell Save Manager v$PACKAGE_VERSION for $PLATFORM..."
yarn "release:$PLATFORM"

assets=()
if [[ "$PLATFORM" == "linux" ]]; then
  assets=(
    "src-tauri/target/release/bundle/appimage/Green Hell Save Manager_${PACKAGE_VERSION}_amd64.AppImage"
    "src-tauri/target/release/bundle/deb/Green Hell Save Manager_${PACKAGE_VERSION}_amd64.deb"
  )
else
  while IFS= read -r -d '' installer; do
    assets+=("$installer")
  done < <(
    find src-tauri/target/release/bundle/nsis \
      -maxdepth 1 -type f -name "*${PACKAGE_VERSION}*.exe" -print0
  )
fi

[[ "${#assets[@]}" -gt 0 ]] || fail "No $PLATFORM release installers were generated."
for asset in "${assets[@]}"; do
  [[ -f "$asset" ]] || fail "Expected installer was not generated: $asset"
done

git push origin "$BRANCH"

if [[ "$local_tag_exists" == false && "$remote_tag_exists" == false ]]; then
  git tag -a "$TAG" -m "Green Hell Save Manager $TAG"
  local_tag_exists=true
fi

if [[ "$remote_tag_exists" == false ]]; then
  git push origin "refs/tags/$TAG"
fi

if gh release view "$TAG" >/dev/null 2>&1; then
  gh release upload "$TAG" "${assets[@]}" --clobber
  echo "Updated GitHub Release $TAG with the $PLATFORM installers."
else
  gh release create "$TAG" "${assets[@]}" \
    --verify-tag \
    --title "Green Hell Save Manager $TAG" \
    --generate-notes
  echo "Created GitHub Release $TAG with the $PLATFORM installers."
fi
