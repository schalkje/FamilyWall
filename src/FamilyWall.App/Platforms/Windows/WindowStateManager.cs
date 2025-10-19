using Microsoft.UI.Windowing;
using Microsoft.Web.WebView2.Core;

namespace FamilyWall.App.WinUI;

/// <summary>
/// Manages window state and F11 fullscreen toggle for Windows platform.
/// </summary>
public static class WindowStateManager
{
	private static AppWindow? _appWindow;
	private static bool _isFullScreen = true;
	private static CoreWebView2? _coreWebView;

	public static void Initialize(Microsoft.UI.Xaml.Window window, AppWindow appWindow)
	{
		_appWindow = appWindow;
	}

	public static void SetCoreWebView2(CoreWebView2 coreWebView)
	{
		_coreWebView = coreWebView;
		System.Diagnostics.Debug.WriteLine("🎹 WindowStateManager: CoreWebView2 set successfully");
	}

	public static void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
	{
		try
		{
			var message = args.WebMessageAsJson;
			System.Diagnostics.Debug.WriteLine($"🎹 WindowStateManager: Received message: {message}");

			if (message.Contains("toggleFullscreen"))
			{
				System.Diagnostics.Debug.WriteLine("🎹 WindowStateManager: Toggling fullscreen");
				ToggleFullscreen();
			}
			else if (message.Contains("navigateInteractive"))
			{
				System.Diagnostics.Debug.WriteLine("🎹 WindowStateManager: Navigating to interactive mode");
				NavigateToInteractive();
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"🎹 WindowStateManager: Error processing message: {ex.Message}");
		}
	}

	private static void NavigateToInteractive()
	{
		// Navigate to Calendar page (last interactive mode)
		try
		{
			_coreWebView?.ExecuteScriptAsync("window.location.href = '/calendar';");
		}
		catch
		{
			// Ignore navigation errors
		}
	}

	public static void ToggleFullscreen()
	{
		if (_appWindow == null) return;

		_isFullScreen = !_isFullScreen;

		if (_isFullScreen)
		{
			// Switch to fullscreen mode
			_appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
		}
		else
		{
			// Switch to windowed mode and maximize
			_appWindow.SetPresenter(AppWindowPresenterKind.Default);

			// Maximize the window for better visibility
			if (_appWindow.Presenter is OverlappedPresenter presenter)
			{
				presenter.Maximize();
			}
		}
	}
}
