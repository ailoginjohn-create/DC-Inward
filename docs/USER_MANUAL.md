# User Manual

## Signing in

- Start `InwardDC.exe`.
- Default administrator: user name `admin`, password `Admin@123`.
- You will be asked to change the password on first login. Store it safely.

## Navigation

The left rail lists the modules. Admin-only modules (`Users`) are hidden to normal users.

| Module | Purpose |
| --- | --- |
| Dashboard | Overview counts and recent activity. |
| Inward | Record and manage inward (goods receipt) entries. |
| Dispatch Challans | Create and manage DCs against available stock. |
| Customers / Vendors / Items / Item Categories | Master data. |
| Search | Global search and serial lookup. |
| Reports | Daily / monthly / customer / item summaries and detail exports. |
| Users | Manage logins (admin only). |
| Company Settings | Company profile, prefixes, document defaults. |
| Backup & Restore | Create/restore backups, factory reset. |
| Audit Log | Read-only trail of who did what. |

## Inward entries

1. Go to **Inward > + New Inward**.
2. Fill the header: number (pre-filled), date, type, customer or vendor, references.
3. Add lines. For each line you may select a **master item** (auto-fills name/make/
   model/HSN/unit) or type a free-text item name.
4. Serial-tracked items: enter one serial per line in the **Serials** column. The
   serial count must equal the quantity.
5. **Save**. The entry can be edited until cancelled.

Actions on the list: **Open**, **Cancel** (irreversible status), **Delete**, **PDF**,
**Template** (Excel import layout), **Import**, **Export**.

### Excel import

Use the **Template** to build a sheet. Headers:
`DATE | D.C No | Invoice No | Items Received From | Name of Item | Qty | Serial No |
Purpose | Remarks | Received By | Remarks`

- Rows sharing the same header (date, party, D.C No, invoice, purpose) form a single
  inward entry.
- **Items Received From** is matched against the customer master first, then the vendor
  master; only one is used per row.
- **Purpose** must match an existing purpose (e.g. Evaluation, Testing, Service); leave
  blank if not applicable.
- For serial-tracked items each serial gets its own row; duplicate serials (in-file or
  in-database) are rejected.
- The importer reports every rejected row with a reason.

## Dispatch Challans

A DC dispatches available stock to a customer.

1. Go to **Dispatch Challans > + New Challan**.
2. Fill the header (DC number, date, customer, references).
3. In **Available Stock**, search, select an item, set quantity and — for serial-tracked
   items — tick the exact serials, then **+ Add to Challan**.
4. Repeat for each line, then **Save Challan**.

Rules:

- Quantity cannot exceed the available (undispatched) quantity of the source inward
  line.
- Serial-tracked items require exactly as many serials as the quantity.
- A DC is **immutable** after generation. To change it, **Cancel** the DC (stock is
  returned automatically) and create a new one.

## Search

- **Global Search**: one box across customers, items, inwards and DCs.
- **Serial Lookup**: enter a serial to see its current stock position and full history.

## Reports

Pick a report and date range, then **Generate**. Use **Export Excel / Export PDF** to
save the current result.

## Company Settings

Company name/address/tax numbers (printed on documents), logo path, numbering prefixes
(`INW`, `DC`, `CUS`, ...) and the footer note. Changes apply to newly generated
documents.

## Backup & Restore

- **Create Backup**: ZIP of the database + settings into `<DataDirectory>\Backups`.
- **Restore from File...**: replace current data with a backup (confirmed with a
  warning; a safety copy is kept).
- Right-click a listed backup to restore it.
- **Factory Reset**: deletes all business data and re-seeds defaults. A backup is
  attempted first.

## Data location

Data lives in `%LOCALAPPDATA%\InwardDC` (configurable via `INWARDDC_DATA_DIR`):

```
InwardDC/
├── Database/InwardDC.db
├── Attachments/
├── Backups/
├── Logs/app-YYYYMMDD.log
├── Reports/
└── Temp/
```

## Troubleshooting

- **Forgotten password**: an administrator can reset it from **Users** (temporary
  password `ChangeMe@123`, user must change it at next login).
- **Startup failure**: check `Logs\app-*.log`; restore a backup or run Factory Reset
  if the store is corrupt.
