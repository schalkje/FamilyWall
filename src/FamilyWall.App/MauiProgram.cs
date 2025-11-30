using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.LifecycleEvents;
using FamilyWall.Core.Settings;
using FamilyWall.Core.Abstractions;
using FamilyWall.Infrastructure;
using FamilyWall.Infrastructure.Data;
using FamilyWall.Services;
using FamilyWall.Integrations.Graph;
using FamilyWall.Integrations.HA;
using System.Reflection;
#if WINDOWS
using FamilyWall.App.WinUI;
#endif

namespace FamilyWall.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		// Configuration: embedded appsettings.json + optional user override
		using var appSettingsStream = Assembly.GetExecutingAssembly()
			.GetManifestResourceStream("FamilyWall.App.appsettings.json");
		if (appSettingsStream != null)
		{
			builder.Configuration.AddJsonStream(appSettingsStream);
		}

		var userSettingsPath = Path.Combine(FileSystem.AppDataDirectory, "appsettings.user.json");
		builder.Configuration.AddJsonFile(userSettingsPath, optional: true, reloadOnChange: true);

		// Settings
		builder.Services.AddOptions<AppSettings>()
			.Bind(builder.Configuration.GetSection("App"));

		// Infrastructure
		var appDataPath = FileSystem.AppDataDirectory;
		builder.Services.AddSingleton<IDbPathProvider>(sp => new DbPathProvider(appDataPath));

		// Use DbContextFactory for better concurrency support in background services
		builder.Services.AddDbContextFactory<AppDbContext>((sp, options) =>
		{
			var dbPath = sp.GetRequiredService<IDbPathProvider>().GetDbPath();
			options.UseSqlite($"Data Source={dbPath}");
		});

		// Also register DbContext for Blazor pages (scoped)
		builder.Services.AddDbContext<AppDbContext>((sp, options) =>
		{
			var dbPath = sp.GetRequiredService<IDbPathProvider>().GetDbPath();
			options.UseSqlite($"Data Source={dbPath}");
		});

		builder.Services.AddSingleton<ITokenStore>(sp => new CredentialLockerTokenStore(appDataPath));
		builder.Services.AddScoped<IPhotoIndex, PhotoIndex>();
		builder.Services.AddSingleton<FamilyWall.Core.Abstractions.IIndexingStatusService, IndexingStatusService>();
		builder.Services.AddScoped<FamilyWall.Core.Abstractions.IPhotoScanService, FamilyWall.Services.PhotoScanService>();
		builder.Services.AddSingleton<FamilyWall.App.Services.PhotoUrlService>();

		// Presence and Screen State
		builder.Services.AddSingleton<FamilyWall.Core.Abstractions.IScreenStateService, FamilyWall.Services.ScreenStateService>();
		builder.Services.AddSingleton<FamilyWall.Core.Abstractions.IPresenceDetector, FamilyWall.Services.MockPresenceDetector>();

		// Calendar Services
		builder.Services.AddScoped<ICalendarService, CalendarService>();
		builder.Services.AddScoped<ICalendarEventService, CalendarEventService>();

		// Sync Strategies
		builder.Services.AddSingleton<ISyncStrategy, GraphSyncStrategy>();

		// Background Services
		builder.Services.AddHostedService<PresenceService>();

		// Register CalendarSyncService as singleton first, then add as hosted service
		builder.Services.AddSingleton<CalendarSyncService>();
		builder.Services.AddHostedService<CalendarSyncService>(sp => sp.GetRequiredService<CalendarSyncService>());

		// Integrations
		builder.Services.AddSingleton<IGraphClient, GraphClient>();
		builder.Services.AddSingleton<IHomeAssistantClient, HomeAssistantClient>();
		builder.Services.AddSingleton<IMqttClientFactory, MqttClientFactory>();

		// Blazor WebView
		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		// Configure Windows fullscreen kiosk mode
#if WINDOWS
		builder.ConfigureLifecycleEvents(events =>
		{
			events.AddWindows(wndLifeCycleBuilder =>
			{
				wndLifeCycleBuilder.OnWindowCreated(window =>
				{
					// Get window handle and AppWindow
					IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
					Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
					var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

					// Set to fullscreen mode immediately on startup
					appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);

					// Store references for F11 toggle support
					FamilyWall.App.WinUI.WindowStateManager.Initialize(window, appWindow);
				});
			});
		});
#endif

		var app = builder.Build();

		// Ensure database schema is up to date (will recreate if schema changed)
		using (var scope = app.Services.CreateScope())
		{
			var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
			var logger = loggerFactory.CreateLogger("DbMigration");

			DbMigrationHelper.EnsureSchemaUpdatedAsync(dbContext, logger).Wait();
		}

		// Start hosted services manually (MAUI doesn't auto-start them like ASP.NET Core)
		Task.Run(async () =>
		{
			try
			{
				var hostedServices = app.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
				foreach (var service in hostedServices)
				{
					await service.StartAsync(CancellationToken.None);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"ERROR starting hosted services: {ex.Message}");
			}
		});

		// Trigger initial photo scan in background after app starts
		Task.Run(async () =>
		{
			await Task.Delay(3000); // Wait 3 seconds for app to fully initialize

			try
			{
				using var scope = app.Services.CreateScope();
				var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
				var logger = loggerFactory.CreateLogger("PhotoScanStartup");
				logger.LogInformation("=== STARTUP SCAN: Starting initial photo scan ===");

				var photoScanService = scope.ServiceProvider.GetRequiredService<FamilyWall.Core.Abstractions.IPhotoScanService>();
				await photoScanService.ScanAllSourcesAsync();
				logger.LogInformation("=== STARTUP SCAN: Initial photo scan completed ===");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"STARTUP SCAN ERROR: {ex.Message}");
				Console.WriteLine($"Stack: {ex.StackTrace}");
			}
		});

		return app;
	}
}
