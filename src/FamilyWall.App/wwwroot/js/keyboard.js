// Keyboard handler for fullscreen toggle and navigation
window.keyboardHandler = {
    initialize: function () {
        console.log('🎹 Keyboard handler initializing...');
        console.log('🎹 WebView2 available:', !!(window.chrome && window.chrome.webview));

        document.addEventListener('keydown', function (e) {
            console.log('🎹 Key pressed:', e.key, 'Code:', e.code);

            // F11 - Toggle fullscreen (only on Windows)
            if (e.key === 'F11') {
                console.log('🎹 F11 detected! Preventing default and sending message...');
                e.preventDefault();

                if (window.chrome && window.chrome.webview) {
                    console.log('🎹 Sending toggleFullscreen message to WebView2');
                    try {
                        window.chrome.webview.postMessage(JSON.stringify({ type: 'toggleFullscreen' }));
                        console.log('🎹 Message sent successfully');
                    } catch (err) {
                        console.error('🎹 Error sending message:', err);
                    }
                } else {
                    console.warn('🎹 WebView2 not available!');
                }
                return false;
            }

            // ESC - Return to interactive mode (navigate to Calendar)
            if (e.key === 'Escape') {
                console.log('🎹 ESC detected! Preventing default and sending message...');
                e.preventDefault();

                if (window.chrome && window.chrome.webview) {
                    console.log('🎹 Sending navigateInteractive message to WebView2');
                    try {
                        window.chrome.webview.postMessage(JSON.stringify({ type: 'navigateInteractive' }));
                        console.log('🎹 Message sent successfully');
                    } catch (err) {
                        console.error('🎹 Error sending message:', err);
                    }
                } else {
                    console.warn('🎹 WebView2 not available!');
                }
                return false;
            }
        });

        console.log('🎹 Keyboard handler initialized successfully');
    }
};

// Auto-initialize when the DOM is ready
console.log('🎹 keyboard.js loaded, readyState:', document.readyState);

if (document.readyState === 'loading') {
    console.log('🎹 Waiting for DOMContentLoaded...');
    document.addEventListener('DOMContentLoaded', function() {
        console.log('🎹 DOMContentLoaded fired!');
        window.keyboardHandler.initialize();
    });
} else {
    console.log('🎹 DOM already loaded, initializing immediately');
    window.keyboardHandler.initialize();
}
