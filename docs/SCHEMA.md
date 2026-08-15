# Schema

All tables use GUID primary keys (`Id`) and inherit the soft-delete/audit columns from
`EntityBase`:

- `Id` (PK, uniqueidentifier/text)
- `IsDeleted`, `DeletedOn`, `DeletedBy`
- `CreatedOn`, `CreatedBy`, `ModifiedOn`, `ModifiedBy`

## Tables

### Users (`Users`)
| Column | Type | Notes |
| --- | --- | --- |
| UserName | string | unique |
| PasswordHash / PasswordSalt | string | PBKDF2, 100k iterations |
| FullName, Email, Phone | string | |
| Role | int | `Admin` / `User` |
| IsActive | bool | disabled users cannot sign in |
| MustChangePassword | bool | true after admin-created/reset accounts |
| LastLoginOn | datetime | |

### Customers (`Customers`) / Vendors (`Vendors`)
Shared shape: `Code` (unique, e.g. `CUS/2026/0001`), `Name`, `ContactPerson`,
`Phone`, `Mobile`, `Email`, `AddressLine1/2`, `City`, `State`, `Pincode`, `Country`,
`GSTIN`, `PAN`, `Notes`, `IsActive`.

### ItemCategories (`ItemCategories`)
`Code` (unique), `Name`, `Description`, `IsActive`.

### Purposes (`Purposes`)
`Name` (unique), `Description`, `IsActive`. Referenced by `InwardEntries.PurposeId`
and `DispatchChallans.PurposeId`.

### Items (`Items`)
| Column | Notes |
| --- | --- |
| Code | unique (`ITM/2026/0001`) |
| Name, Make, Model, Unit, HsnCode, Description | |
| CategoryId | FK -> ItemCategories (nullable) |
| IsSerialTracked | drives serial validation on inward/dispatch |

### InwardEntries (`InwardEntries`)
| Column | Notes |
| --- | --- |
| InwardNo | unique (`INW/2026/0001`) |
| InwardDate | |
| InwardType | `CustomerReturn` / `Purchase` / `TransferIn` / `Other` |
| CustomerId / VendorId | nullable FKs; at least one required by rule |
| ReferenceInvoiceNo, ReferenceInvoiceDate | |
| ChallanNo, TransportDetails, ReceivedBy, Remarks | |
| PurposeId | nullable FK to `Purposes` |
| Status | `Draft` / `Received` / `Cancelled` |
| TotalQuantity, TotalAmount | denormalized for reporting |

### InwardItems (`InwardItems`)
Lines of an inward entry: `InwardEntryId` (FK), `ItemId` (nullable FK for free-text
items), `ItemName`, `ItemMake`, `ItemModel`, `HsnCode`, `Unit`, `Quantity`, `Rate`,
`Amount`, `DispatchedQuantity` (how much has been sent out), `Remarks`.

### SerialNumbers (`SerialNumbers`)
One row per physical serial: `SerialNo` (unique), `ItemId`, `InwardItemId` (source
inward line, locks availability), `InwardEntryId`, `Status` (`InStock` / `Dispatched` /
`Returned` / `Scrapped`), plus `DispatchItemId` / `DispatchChallanId` when dispatched.

### DispatchChallans (`DispatchChallans`)
| Column | Notes |
| --- | --- |
| DcNo | unique (`DC/2026/0001`) |
| DcDate | |
| CustomerId | FK, required |
| SourceInwardEntryId | FK, nullable (multi-inward DCs leave this null) |
| ReferenceChallanNo, InvoiceNo, TransportDetails, Remarks | |
| PaymentStatus, ModeOfDispatch, PodNo | free text (set from manual entry or Excel import) |
| Status | `Draft` / `Generated` / `Cancelled` |
| TotalQuantity, TotalAmount | denormalized |

### DispatchItems (`DispatchItems`)
`DispatchChallanId` (FK), `SourceInwardItemId` (FK -> InwardItems, locks availability),
`ItemId` (nullable), `ItemName`, `ItemMake`, `ItemModel`, `HsnCode`, `Unit`,
`Quantity`, `Rate`, `Amount`, `Remarks`.

### Attachments (`Attachments`)
File metadata only; bytes live on disk under `<DataDirectory>/Attachments`:
`EntityType`, `EntityId`, `FileName`, `StoredPath`, `ContentType`, `FileSize`, `Notes`,
`UploadedOn`.

### AuditLogs (`AuditLogs`)
`UserId`, `UserName`, `FullName`, `Action` (`Create`/`Update`/`Delete`/`Login`/...),
`EntityType`, `EntityId`, `Description`, `Details` (JSON), `IpAddress`, `Timestamp`.

### Settings (`Settings`)
Key/value store: `Key` (PK), `Value`, `Group`, `Description`, `DataType`,
`IsSystem`. Used for company profile and numbering prefixes (see
`SettingsService.Keys`).

### SequenceCounters (`SequenceCounters`)
Atomic per-type sequence: `CounterType` (e.g. `Inward`, `DC`, `Customer`), `Year`,
`LastValue`. Used to allocate unique numbers safely.

### ItemEvents (`ItemEvents`)
Item/serial timeline: `ItemId`, `SerialNo`, `EventType`, `ReferenceType`,
`ReferenceNumber`, `Quantity`, `Notes`, `UserName`, `EventedOn`.

## Index & uniqueness notes

- Unique indexes on all business codes/numbers (`InwardNo`, `DcNo`, serial numbers,
  customer/vendor/item/category codes, `UserName`).
- Soft-delete filters use `IsDeleted`; providers that support filtered unique indexes
  (SQLite, Npgsql, SqlServer) apply `WHERE IsDeleted = 0`; MySQL uses a plain unique
  constraint so deleted codes must be recycled manually.
- Audit/transaction tables are indexed on the columns used by search filters
  (`CustomerId`, `ItemId`, `SerialNo`, `Status`, date ranges, `EntityType`).
