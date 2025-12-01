// API Configuration
// Detects if running in Android WebView and uses direct backend connection

/**
 * Check if we're running in Android WebView
 */
export function isAndroidWebView() {
  return typeof window.Android !== 'undefined'
}

/**
 * Get the appropriate API base URL
 * - In Android WebView: Connect directly to backend (10.0.2.2:5000)
 * - In Browser: Use relative path for Vite proxy
 */
export function getApiBaseUrl() {
  if (isAndroidWebView()) {
    // Android: direct connection to backend
    // Use 10.0.2.2 for emulator, or your computer's IP for real device
    return 'http://192.168.31.162:5000/api'
  }
  // Browser: use Vite proxy
  return '/api'
}

/**
 * Get the appropriate SignalR Hub URL
 */
export function getSignalRHubUrl() {
  if (isAndroidWebView()) {
    // Direct connection to backend
    return 'http://192.168.31.162:5000/api/gamehub'
  }
  // Browser: use Vite proxy
  return '/api/gamehub'
}

console.log('API Config:', {
  isAndroid: isAndroidWebView(),
  apiBaseUrl: getApiBaseUrl(),
  signalRUrl: getSignalRHubUrl()
})
