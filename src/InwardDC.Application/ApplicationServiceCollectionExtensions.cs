using InwardDC.Application.Interfaces;
using InwardDC.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InwardDC.Application;

/// <summary>
/// Registers the business layer. UI/Infrastructure call this from their own
/// composition roots. ICurrentUserService is intentionally NOT registered here —
/// each host (desktop, web, mobile) provides its own implementation.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IItemCategoryService, ItemCategoryService>();
        services.AddScoped<IInwardService, InwardService>();
        services.AddScoped<IDispatchService, DispatchService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
