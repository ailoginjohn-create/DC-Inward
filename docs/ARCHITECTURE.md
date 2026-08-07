# Architecture

## Solution layout

```
InwardDC.sln
├── src/
│   ├── InwardDC.Domain          Entities, enums, criteria, domain exceptions,
│   │                            repository + unit-of-work interfaces.
│   ├── InwardDC.Application     DTOs, service interfaces, service implementations,
│   │                            password hashing, current-user abstraction.
│   ├── InwardDC.Infrastructure  EF Core DbContext, migrations, repositories,
│   │                            UnitOfWork, Excel (ClosedXML), PDF (QuestPDF),
│   │                            backup/restore, seeding, path management.
│   └── InwardDC.App             WPF shell (MVVM via CommunityToolkit.Mvvm), views.
└── tests/
    └── InwardDC.Tests           xUnit tests against a real SQLite database.
```

## Layering rules

- **Domain** has no dependencies on any other project.
- **Application** depends only on Domain.
- **Infrastructure** depends on Application + Domain.
- **App** depends on Application + Infrastructure (composition root).
- **Tests** depend on Infrastructure/Application/Domain.

## Key abstractions

| Abstraction | Lives in | Implemented by |
| --- | --- | --- |
| `IUserRepository`, `ICustomerRepository`, `IInwardRepository`, `IDCRepository`, etc. | Domain.Interfaces | Infrastructure.Repositories |
| `IUnitOfWork` | Domain.Interfaces | Infrastructure.UnitOfWork |
| `ICurrentUserService` | Application.Common | App.Services.CurrentUserService |
| `IExcelService`, `IPdfService`, `IBackupService` | Application.Interfaces | Infrastructure.Services |
| `IDialogService` | App.Services | App.Services.DialogService |

Because all data access is behind repository interfaces and DTOs, the persistence
strategy can be swapped (EF Core today, REST API tomorrow) without touching services
or the UI.

## MVVM and navigation

- `App.xaml.cs` is the composition root: builds `IServiceProvider`, configures Serilog,
  applies migrations and seeds, then shows the `LoginWindow`.
- `ShellWindow` hosts a left navigation rail. Each navigation target is a
  `NavItem(Label, ViewModelType, RequiresAdmin)`; the shell resolves the ViewModel from
  DI and renders it through a `DataTemplate` keyed by ViewModel type (declared in
  `App.xaml`). Admin-gated modules show a warning for non-admin users.
- Every ViewModel derives from `ViewModelBase`, which provides busy state, status/error
  surfacing and consistent exception-to-message mapping for `DomainException` family.
- Long-running operations are wrapped in `RunAsync(...)`.

## Business rules (Domain enforced)

- **Serial tracking**: if an item is serial-tracked, the serial count on an inward line
  must equal the line quantity. Duplicate serials within a file/entry or already in the
  database are rejected.
- **Dispatch** consumes stock: quantity cannot exceed the available (undispatched)
  quantity of the source inward line; serial-tracked items require an exact set of
  serials. A DC is immutable after generation — cancel and recreate to change it.
- **Cancel reversal**: cancelling a DC returns `DispatchedQuantity` to the inward line
  and flips serial statuses back to `InStock`.
- **Soft delete**: all entities inherit `EntityBase` (`Id`, `IsDeleted`, `DeletedOn`,
  `DeletedBy`, `CreatedOn`, `CreatedBy`, `ModifiedOn`, `ModifiedBy`). Repositories
  exclude deleted records; deletes are recoverable via restore of a backup.
- **Audit**: service layer writes `AuditLog` rows via `IAuditService` for create/update/
  delete/status/login actions.

## Numbering

Sequential numbers (`INW/2026/0001`, `DC/2026/0001`, master codes, ...) are allocated
atomically through `SequenceCounter` rows; prefixes are configurable in Company Settings.

## Excel / PDF / Backup

- **Excel**: `ExcelService` (ClosedXML) implements import (grouped by header), template
  generation, and exports for inwards, dispatches, reports and audit logs.
- **PDF**: `PdfService` (QuestPDF) renders inward/DC documents and report exports.
- **Backup**: `BackupService` bundles a `manifest.json` + `settings.json` + the SQLite
  database into a ZIP. WAL is checkpointed before bundling. Restore stages to a temp
  folder and swaps directories with a pre-restore safety copy. Factory reset clears
  business tables (constant allow-list) and re-seeds.

## Configuration

`appsettings.json` (App project) carries `Database:Provider` and `App:DataDirectory`.
The design-time factory (`InwardDcDesignTimeFactory`) reads the same keys so `dotnet ef`
migrations work against each provider.
