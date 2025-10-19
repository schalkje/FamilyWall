using Microsoft.AspNetCore.Components.WebView;

namespace FamilyWall.App;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private void OnBlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("🎹 MainPage: BlazorWebViewInitialized event fired");

#if WINDOWS
		if (e.WebView is Microsoft.UI.Xaml.Controls.WebView2 webView2)
		{
			System.Diagnostics.Debug.WriteLine("🎹 MainPage: WebView2 control found");

			// Subscribe to CoreWebView2Initialized to ensure CoreWebView2 is ready
			webView2.CoreWebView2Initialized += (s, args) =>
			{
				System.Diagnostics.Debug.WriteLine("🎹 MainPage: CoreWebView2 initialized");

				var coreWebView2 = webView2.CoreWebView2;

				// Subscribe to WebMessageReceived
				coreWebView2.WebMessageReceived += FamilyWall.App.WinUI.WindowStateManager.OnWebMessageReceived;
				System.Diagnostics.Debug.WriteLine("🎹 MainPage: Subscribed to WebMessageReceived event");

				// Pass the CoreWebView2 to WindowStateManager
				FamilyWall.App.WinUI.WindowStateManager.SetCoreWebView2(coreWebView2);
			};

			// If CoreWebView2 is already initialized, set it up immediately
			if (webView2.CoreWebView2 != null)
			{
				System.Diagnostics.Debug.WriteLine("🎹 MainPage: CoreWebView2 already initialized, setting up immediately");

				var coreWebView2 = webView2.CoreWebView2;
				coreWebView2.WebMessageReceived += FamilyWall.App.WinUI.WindowStateManager.OnWebMessageReceived;
				System.Diagnostics.Debug.WriteLine("🎹 MainPage: Subscribed to WebMessageReceived event");

				FamilyWall.App.WinUI.WindowStateManager.SetCoreWebView2(coreWebView2);
			}
		}
		else
		{
			System.Diagnostics.Debug.WriteLine("🎹 MainPage: WARNING - e.WebView is not a WebView2 control!");
		}
#else
		System.Diagnostics.Debug.WriteLine("🎹 MainPage: Not running on Windows platform");
#endif
	}
}
