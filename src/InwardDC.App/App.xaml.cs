using System.IO;
using System.Windows;
using InwardDC.App.Services;
using InwardDC.App.ViewModels;
using InwardDC.App.Views;
using InwardDC.Application;
using InwardDC.Application.Common;
using InwardDC.Infrastructure;
using InwardDC.Infrastructure.Common;
using InwardDC.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace InwardDC.App;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var paths = new AppPaths(configuration["App:DataDirectory"]);
        ConfigureSerilog(paths);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(b => b.AddSerilog(dispose: true));

        services.AddInfrastructureServices(configuration);
        services.AddApplicationServices();

        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDialogService, DialogService>();

        RegisterViewModels(services);
        RegisterWindows(services);

        Services = services.BuildServiceProvider();

        ApplyMigrationsAndSeed();

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        var login = Services.GetRequiredService<LoginWindow>();
        MainWindow = login;
        login.Show();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<InwardListViewModel>();
        services.AddSingleton<DispatchListViewModel>();
        services.AddSingleton<CustomersViewModel>();
        services.AddSingleton<VendorsViewModel>();
        services.AddSingleton<ItemsViewModel>();
        services.AddSingleton<ItemCategoriesViewModel>();
        services.AddSingleton<UsersViewModel>();
        services.AddTransient<InwardEditorViewModel>();
        services.AddTransient<DispatchEditorViewModel>();
        services.AddTransient<CustomerEditorViewModel>();
        services.AddTransient<VendorEditorViewModel>();
        services.AddTransient<ItemEditorViewModel>();
        services.AddTransient<ItemCategoryEditorViewModel>();
        services.AddTransient<UserEditorViewModel>();
        services.AddSingleton<SearchViewModel>();
        services.AddSingleton<ReportsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<BackupViewModel>();
        services.AddSingleton<AuditViewModel>();
    }

    private static void RegisterWindows(IServiceCollection services)
    {
        services.AddTransient<LoginWindow>();
        services.AddTransient<ShellWindow>();
        services.AddTransient<InwardEditorWindow>();
        services.AddTransient<DispatchEditorWindow>();
        services.AddTransient<CustomerEditorWindow>();
        services.AddTransient<VendorEditorWindow>();
        services.AddTransient<ItemEditorWindow>();
        services.AddTransient<ItemCategoryEditorWindow>();
        services.AddTransient<UserEditorWindow>();
    }

    private static void ApplyMigrationsAndSeed()
    {
        try
        {
            using var scope = Services.CreateScope();
            var migrations = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
            migrations.MigrateAsync().GetAwaiter().GetResult();

            var seeder = scope.ServiceProvider.GetRequiredService<SeedService>();
            seeder.SeedAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "The application data store could not be initialized.\n\n" + ex.Message,
                "Inward & DC - Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            System.Windows.Application.Current.Shutdown(1);
        }
    }

    private static void ConfigureSerilog(AppPaths paths)
    {
        Directory.CreateDirectory(paths.LogsDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(paths.LogsDirectory, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
