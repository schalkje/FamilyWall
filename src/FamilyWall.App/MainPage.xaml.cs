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
#if WINDOWS
		if (e.WebView is Microsoft.UI.Xaml.Controls.WebView2 webView2)
		{
			// Subscribe to CoreWebView2Initialized to ensure CoreWebView2 is ready
			webView2.CoreWebView2Initialized += (s, args) =>
			{
				var coreWebView2 = webView2.CoreWebView2;

				// Subscribe to WebMessageReceived
				coreWebView2.WebMessageReceived += FamilyWall.App.WinUI.WindowStateManager.OnWebMessageReceived;

				// Pass the CoreWebView2 to WindowStateManager
				FamilyWall.App.WinUI.WindowStateManager.SetCoreWebView2(coreWebView2);
			};
		}
#endif
	}
}
