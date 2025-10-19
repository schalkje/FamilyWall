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
	}

	public static void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
	{
		try
		{
			var message = args.WebMessageAsJson;
			if (message.Contains("toggleFullscreen"))
			{
				ToggleFullscreen();
			}
			else if (message.Contains("navigateInteractive"))
			{
				NavigateToInteractive();
			}
		}
		catch
		{
			// Ignore message parsing errors
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
