# Green Hell Save Manager

Cross-platform desktop manager for Green Hell Steam saves and local backup profiles.

## Development

The Linux development environment uses the configured Debian Distrobox:

```bash
npm run dev
```

## Desktop releases

Keep the version in `package.json`, `src-tauri/Cargo.toml`, and
`src-tauri/tauri.conf.json` synchronized before building a release.

Commit all changes and authenticate GitHub CLI before publishing:

```bash
gh auth login -h github.com -p https -w
```

The publishing script builds the selected desktop package, pushes the current
branch and version tag, and creates or updates the GitHub Release. It does not
produce a browser/web release.

### Linux

Run on the configured Linux development machine:

```bash
yarn publish:linux
```

This publishes AppImage and Debian packages generated under:

```text
src-tauri/target/release/bundle/appimage/
src-tauri/target/release/bundle/deb/
```

### Windows

Run on a Windows machine with the Tauri prerequisites, Node.js, Rust, and
WebView2 installed:

```powershell
yarn publish:windows
```

Run this from Git Bash. It publishes the NSIS installer generated under:

```text
src-tauri\target\release\bundle\nsis\
```

Windows installers must be built on Windows. The Linux Distrobox is only used
for Linux development and release bundles.
