namespace InwardDC.Domain.Catalog;

/// <summary>
/// A module shown in the application navigation. Non-admin users can be restricted
/// to a subset of modules via <see cref="Entities.User.AllowedModules"/>; admins and
/// the Dashboard are always available.
/// </summary>
public sealed record AppModule(string Key, string Label, bool AdminOnly = false)
{
    public static readonly AppModule Dashboard = new("dashboard", "Dashboard");
    public static readonly AppModule Inward = new("inward", "Inward");
    public static readonly AppModule Dispatch = new("dispatch", "Dispatch Challans");
    public static readonly AppModule Customers = new("customers", "Customers");
    public static readonly AppModule Vendors = new("vendors", "Vendors");
    public static readonly AppModule Items = new("items", "Items");
    public static readonly AppModule ItemCategories = new("item-categories", "Item Categories");
    public static readonly AppModule Purposes = new("purposes", "Purposes");
    public static readonly AppModule Search = new("search", "Search");
    public static readonly AppModule Reports = new("reports", "Reports");
    public static readonly AppModule Users = new("users", "Users", AdminOnly: true);
    public static readonly AppModule Settings = new("settings", "Company Settings");
    public static readonly AppModule Backup = new("backup", "Backup & Restore");
    public static readonly AppModule Audit = new("audit", "Audit Log");

    /// <summary>All modules in navigation order.</summary>
    public static IReadOnlyList<AppModule> All { get; } = new[]
    {
        Dashboard, Inward, Dispatch, Customers, Vendors, Items, ItemCategories,
        Purposes, Search, Reports, Users, Settings, Backup, Audit
    };

    /// <summary>
    /// Modules that can be granted/revoked per user. Excludes the Dashboard (always
    /// available) and admin-only modules (already gated by role).
    /// </summary>
    public static IReadOnlyList<AppModule> Restrictable { get; } =
        All.Where(m => !m.AdminOnly && m != Dashboard).ToList();

    public static AppModule? Find(string key) =>
        All.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));
}
