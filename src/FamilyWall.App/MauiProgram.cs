using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using FamilyWall.Core.Settings;
using FamilyWall.Core.Abstractions;
using FamilyWall.Infrastructure;
using FamilyWall.Infrastructure.Data;
using FamilyWall.Services;
using FamilyWall.Integrations.Graph;
using FamilyWall.Integrations.HA;
using System.Reflection;

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

		// Background Services
		builder.Services.AddHostedService<PresenceService>();

		// Integrations
		builder.Services.AddHttpClient<IGraphClient, GraphClient>();
		builder.Services.AddSingleton<IHomeAssistantClient, HomeAssistantClient>();
		builder.Services.AddSingleton<IMqttClientFactory, MqttClientFactory>();

		// Blazor WebView
		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Ensure database is created
		using (var scope = app.Services.CreateScope())
		{
			var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			dbContext.Database.EnsureCreated();
		}

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
