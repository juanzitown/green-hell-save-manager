# Green Hell Save Manager

Cross-platform desktop manager for Green Hell Steam saves and local backup profiles.

## Development

The Linux development environment uses the configured Debian Distrobox:

```bash
npm run dev
```

## Releases

Keep the version in `package.json`, `src-tauri/Cargo.toml`, and
`src-tauri/tauri.conf.json` synchronized before building a release.

### Linux

Run on the configured Linux development machine:

```bash
yarn release:linux
```

This builds AppImage and Debian packages under:

```text
src-tauri/target/release/bundle/appimage/
src-tauri/target/release/bundle/deb/
```

### Windows

Run on a Windows machine with the Tauri prerequisites, Node.js, Rust, and
WebView2 installed:

```powershell
yarn release:windows
```

This builds an NSIS installer under:

```text
src-tauri\target\release\bundle\nsis\
```

Windows installers must be built on Windows. The Linux Distrobox is only used
for Linux development and release bundles.
