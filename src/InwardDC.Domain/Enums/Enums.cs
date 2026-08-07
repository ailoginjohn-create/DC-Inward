namespace InwardDC.Domain.Enums;

/// <summary>Application roles. Extend with more roles (Manager, Auditor, ...) as the ERP grows.</summary>
public enum UserRole
{
    Admin = 1,
    User = 2
}

/// <summary>Persistence provider abstraction. Maps to the Database.Provider setting.</summary>
public enum DatabaseProviderKind
{
    SQLite = 1,
    PostgreSQL = 2,
    SqlServer = 3,
    MySQL = 4
}

/// <summary>Where an inward (receipt) comes from.</summary>
public enum InwardType
{
    CustomerReturn = 1,
    Purchase = 2,
    ServiceIn = 3,
    Other = 4
}

public enum InwardStatus
{
    Draft = 1,
    Received = 2,
    PartiallyDispatched = 3,
    FullyDispatched = 4,
    Closed = 5,
    Cancelled = 6
}

public enum DispatchStatus
{
    Draft = 1,
    Generated = 2,
    Cancelled = 3
}

public enum SerialStatus
{
    InStock = 1,
    Dispatched = 2
}

/// <summary>Lifecycle event kinds recorded in the item history / timeline.</summary>
public enum ItemEventType
{
    Created = 1,
    InwardReceived = 2,
    Dispatched = 3,
    DispatchCancelled = 4,
    Adjustment = 5,
    Deleted = 6
}

/// <summary>Attachment owner entity type used to store files outside the DB.</summary>
public enum AttachmentEntityType
{
    Customer = 1,
    Vendor = 2,
    Item = 3,
    InwardEntry = 4,
    DispatchChallan = 5,
    User = 6,
    Generic = 7
}

/// <summary>Audit actions captured by the AuditLog table.</summary>
public enum AuditAction
{
    Login = 1,
    Logout = 2,
    Create = 3,
    Update = 4,
    Delete = 5,
    Disable = 6,
    Enable = 7,
    ResetPassword = 8,
    Backup = 9,
    Restore = 10,
    FactoryReset = 11,
    Import = 12,
    Export = 13,
    Print = 14,
    Search = 15,
    GenerateDC = 16,
    CancelDC = 17,
    AttachFile = 18,
    ChangePassword = 19
}
