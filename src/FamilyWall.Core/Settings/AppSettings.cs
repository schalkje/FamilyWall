namespace FamilyWall.Core.Settings;

public class AppSettings
{
    public PhotoSettings Photos { get; set; } = new();
    public CalendarSettings Calendar { get; set; } = new();
    public GraphSettings Graph { get; set; } = new();
    public NightModeSettings NightMode { get; set; } = new();
    public HomeAssistantSettings HomeAssistant { get; set; } = new();
    public MqttSettings Mqtt { get; set; } = new();
    public PrivacySettings Privacy { get; set; } = new();
}

public class PhotoSettings
{
    public List<PhotoSource> Sources { get; set; } = new();
    public int CacheSizeMb { get; set; } = 500;
    public int PrefetchCount { get; set; } = 10;
    public int SlideshowIntervalSeconds { get; set; } = 5;
}

public class PhotoSource
{
    public required string Type { get; set; } // "NAS", "OneDrive", "Local"
    public string? Path { get; set; }
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 0;
}

public class CalendarSettings
{
    public List<string> Providers { get; set; } = new() { "Graph" }; // "Graph", "Google", "ICS"
    public int CacheTtlMinutes { get; set; } = 15;
    public bool ShowBirthdays { get; set; } = true;
}

public class GraphSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string TenantId { get; set; } = "consumers"; // "consumers" for personal Microsoft accounts, "common" for work/school + personal, or specific tenant ID
    public string[] Scopes { get; set; } = new[] { "User.Read", "Calendars.Read", "Files.Read" };
    public bool Enabled { get; set; } = true;
    public List<string> CalendarIds { get; set; } = new(); // Empty = sync all calendars, or specify calendar IDs to sync
}

public class NightModeSettings
{
    public TimeOnly StartTime { get; set; } = new(22, 0);
    public TimeOnly EndTime { get; set; } = new(7, 0);
    public bool Enabled { get; set; } = true;
    public int MotionThreshold { get; set; } = 30; // 0-255
    public int InactivityTimeoutSeconds { get; set; } = 30;
}

public class HomeAssistantSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
}

public class MqttSettings
{
    public string Broker { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string TopicPrefix { get; set; } = "home/surface_panel";
    public bool Enabled { get; set; } = false;
}

public class PrivacySettings
{
    public bool RecordingConsent { get; set; } = false;
    public bool NightModeRecordingEnabled { get; set; } = true;
    public int RetentionDays { get; set; } = 3;
}
