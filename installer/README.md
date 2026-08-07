# Installer

The installer is built with [Inno Setup](https://jrsoftware.org/isinfo.php) (v6).

## Prerequisites

- A Windows machine (or Wine) with Inno Setup installed (`iscc` on PATH).
- A published build in `publish\win-x64` (run `scripts\publish.ps1` first).

## Building

```powershell
scripts\build-installer.ps1
```

or directly:

```powershell
iscc installer\installer.iss
```

Output: `installer\output\InwardDC-Setup-1.0.0.exe`.

## What the installer does

- Installs to `%ProgramFiles%\InwardDC` (user-level if no admin rights).
- Creates Start Menu and optional desktop shortcuts.
- Launches the app after install.
- On uninstall, asks whether to also delete `%LOCALAPPDATA%\InwardDC` (database,
  attachments, backups). Data is kept by default.

## Notes

- The application is self-contained: no .NET runtime requirement on the target machine.
- The app's data directory (`%LOCALAPPDATA%\InwardDC`) is intentionally separate from
  the install folder so upgrades never touch user data.
