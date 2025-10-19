// Keyboard handler for fullscreen toggle and navigation
window.keyboardHandler = {
    initialize: function () {
        document.addEventListener('keydown', function (e) {
            // F11 - Toggle fullscreen (only on Windows)
            if (e.key === 'F11') {
                e.preventDefault();
                if (window.chrome && window.chrome.webview) {
                    // Running in WebView2 - send message to Windows
                    window.chrome.webview.postMessage(JSON.stringify({ type: 'toggleFullscreen' }));
                }
                return false;
            }

            // ESC - Return to interactive mode (navigate to Calendar)
            if (e.key === 'Escape') {
                e.preventDefault();
                if (window.chrome && window.chrome.webview) {
                    // Running in WebView2 - send message to Windows
                    window.chrome.webview.postMessage(JSON.stringify({ type: 'navigateInteractive' }));
                }
                return false;
            }
        });

        console.log('Keyboard handler initialized');
    }
};

// Auto-initialize when the DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        window.keyboardHandler.initialize();
    });
} else {
    window.keyboardHandler.initialize();
}
