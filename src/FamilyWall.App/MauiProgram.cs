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
		builder.Services.AddSingleton<IPhotoIndex, PhotoIndex>();

		// Background Services
		builder.Services.AddHostedService<PhotoIndexingService>();
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

		return app;
	}
}
