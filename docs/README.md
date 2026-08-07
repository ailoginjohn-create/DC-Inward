# Inward & DC

An offline-first Windows desktop application for managing **inward (goods receipt) entries**
and **dispatch challans (DCs)** with full serial-number tracking, Excel import/export,
PDF generation, audit logging and one-click backup/restore.

## Highlights

- Fully offline. No paid or cloud dependencies at runtime.
- Clean Architecture: `Domain`, `Application`, `Infrastructure`, `App` (WPF) and `Tests`.
- SQLite + EF Core by default; PostgreSQL / SQL Server / MySQL supported by changing one
  config value (see `docs/MIGRATION.md`).
- GUID primary keys, soft delete everywhere, full audit trail.
- Serial-number tracking with stock validation for inward and dispatch.
- Excel bulk import + template, Excel/PDF exports, item/customer/report screens.
- Backup/restore/factory-reset with WAL checkpointing.

## Documentation

| Document | Purpose |
| --- | --- |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Solution structure, layers, key decisions. |
| [SCHEMA.md](SCHEMA.md) | Data model, tables, relationships, index notes. |
| [MIGRATION.md](MIGRATION.md) | Switching the database provider (PostgreSQL/SQL Server/MySQL). |
| [USER_MANUAL.md](USER_MANUAL.md) | End-user guide for the application. |

## Building

Prerequisites: .NET SDK 8.0 (Windows) with the Windows Desktop workload.

```powershell
dotnet build InwardDC.sln
dotnet test InwardDC.sln
```

## Publishing the Windows app

See `scripts/publish.ps1` (recommended, run on Windows) or `scripts/publish.sh`
(cross-publish from Linux). Output is a single self-contained `InwardDC.exe`.

## Installer

The `installer/installer.iss` Inno Setup script packages the published files into a
setup executable. See `installer/README.md` for build steps.

## First run

- The database is created and migrated automatically on first launch.
- Seed data creates the default administrator: **user name `admin`, password `Admin@123`**.
  Change it immediately after first login.
- Data is stored in `%LOCALAPPDATA%\InwardDC` by default. Override with the
  `INWARDDC_DATA_DIR` environment variable or the `App:DataDirectory` setting.
