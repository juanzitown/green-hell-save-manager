#!/usr/bin/env bash

set -Eeuo pipefail

readonly PROJECT_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly DISTROBOX_NAME="${DISTROBOX_NAME:-debian}"
readonly WINDOWS_TARGET="x86_64-pc-windows-msvc"

fail() {
  echo "Windows release build failed: $*" >&2
  exit 1
}

command -v distrobox >/dev/null 2>&1 ||
  fail "distrobox is not installed on the host."

if ! distrobox list --no-color 2>/dev/null | awk 'NR > 1 { print $3 }' |
  grep -Fxq "$DISTROBOX_NAME"; then
  fail "Distrobox '$DISTROBOX_NAME' does not exist. Set DISTROBOX_NAME to an existing Debian/Ubuntu box."
fi

echo "Building the Windows NSIS installer in Distrobox '$DISTROBOX_NAME'..."

distrobox enter "$DISTROBOX_NAME" -- bash -lc '
  set -Eeuo pipefail

  readonly project_root="$1"
  readonly windows_target="$2"
  export PATH="/usr/bin:/bin:$HOME/.cargo/bin"

  cd "$project_root"

  required_packages=(
    build-essential
    ca-certificates
    clang
    curl
    lld
    llvm
    nsis
  )
  missing_packages=()

  for package in "${required_packages[@]}"; do
    if ! dpkg-query -W -f="\${Status}" "$package" 2>/dev/null |
      grep -Fq "install ok installed"; then
      missing_packages+=("$package")
    fi
  done

  if (( ${#missing_packages[@]} > 0 )); then
    echo "Installing Distrobox packages: ${missing_packages[*]}"
    sudo apt-get update
    sudo env DEBIAN_FRONTEND=noninteractive apt-get install -y "${missing_packages[@]}"
  fi

  command -v rustup >/dev/null 2>&1 || {
    echo "rustup was not found inside the Distrobox." >&2
    exit 1
  }
  command -v npm >/dev/null 2>&1 || {
    echo "npm was not found inside the Distrobox." >&2
    exit 1
  }
  command -v makensis >/dev/null 2>&1 || {
    echo "NSIS was installed, but makensis is unavailable." >&2
    exit 1
  }

  rustup target add "$windows_target"

  if ! command -v cargo-xwin >/dev/null 2>&1; then
    echo "Installing cargo-xwin..."
    cargo install --locked cargo-xwin
  fi

  if [[ ! -x node_modules/.bin/tauri ]]; then
    echo "Installing Node dependencies..."
    npm ci
  fi

  npm run tauri build -- \
    --runner cargo-xwin \
    --target "$windows_target" \
    --bundles nsis \
    --ci
' _ "$PROJECT_ROOT" "$WINDOWS_TARGET"

readonly BUNDLE_DIR="$PROJECT_ROOT/src-tauri/target/$WINDOWS_TARGET/release/bundle/nsis"

if [[ ! -d "$BUNDLE_DIR" ]]; then
  fail "Tauri completed without creating the expected NSIS directory: $BUNDLE_DIR"
fi

mapfile -d "" installers < <(
  find "$BUNDLE_DIR" -maxdepth 1 -type f -name "*.exe" -print0
)

(( ${#installers[@]} > 0 )) ||
  fail "No Windows installer was generated in $BUNDLE_DIR"

echo "Windows installer created:"
printf "  %s\n" "${installers[@]}"
