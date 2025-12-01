package com.cosmochess.ui.game

import android.annotation.SuppressLint
import android.os.Bundle
import android.util.Log
import android.webkit.*
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.cosmochess.ChessApplication
import com.cosmochess.R

class WebViewGameActivity : AppCompatActivity() {

    private val TAG = "WebViewGameActivity"
    private lateinit var webView: WebView
    private lateinit var gameId: String

    private val authRepository by lazy {
        (application as ChessApplication).authRepository
    }

    private val apiClient by lazy {
        (application as ChessApplication).apiClient
    }

    @SuppressLint("SetJavaScriptEnabled")
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_webview_game)

        gameId = intent.getStringExtra("gameId") ?: run {
            Toast.makeText(this, "No game ID provided", Toast.LENGTH_SHORT).show()
            finish()
            return
        }

        webView = findViewById(R.id.webView)

        setupWebView()
        loadGame()
    }

    @SuppressLint("SetJavaScriptEnabled")
    private fun setupWebView() {
        webView.settings.apply {
            javaScriptEnabled = true
            domStorageEnabled = true
            databaseEnabled = true
            mixedContentMode = WebSettings.MIXED_CONTENT_ALWAYS_ALLOW

            // Enable debugging in debug builds
            if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.KITKAT) {
                WebView.setWebContentsDebuggingEnabled(true)
            }
        }

        // Add JavaScript interface for native communication
        webView.addJavascriptInterface(AndroidBridge(), "Android")

        // Setup WebView client
        webView.webViewClient = object : WebViewClient() {
            override fun shouldOverrideUrlLoading(view: WebView?, request: WebResourceRequest?): Boolean {
                return false
            }

            override fun onPageFinished(view: WebView?, url: String?) {
                super.onPageFinished(view, url)
                Log.d(TAG, "Page finished loading: $url")

                // Inject authentication token
                injectAuthToken()
            }

            override fun onReceivedError(
                view: WebView?,
                request: WebResourceRequest?,
                error: WebResourceError?
            ) {
                super.onReceivedError(view, request, error)
                Log.e(TAG, "WebView error: ${error?.description}")
            }
        }

        webView.webChromeClient = object : WebChromeClient() {
            override fun onConsoleMessage(consoleMessage: ConsoleMessage?): Boolean {
                consoleMessage?.let {
                    Log.d(TAG, "WebView Console: ${it.message()} (${it.sourceId()}:${it.lineNumber()})")
                }
                return true
            }
        }
    }

    private fun loadGame() {
        // Get base URL from API client (e.g., http://10.0.2.2:5000/)
        val baseUrl = apiClient.getBaseUrl()

        // Frontend is typically served on port 8080
        val frontendUrl = baseUrl.replace(":5000", ":8080")
        val gameUrl = "${frontendUrl}game/$gameId"

        Log.d(TAG, "Loading game URL: $gameUrl")
        webView.loadUrl(gameUrl)
    }

    private fun injectAuthToken() {
        val token = authRepository.getToken()
        val user = authRepository.getCurrentUser()

        if (token != null && user != null) {
            val userId = user.id

            // Inject token and userId into localStorage
            val script = """
                (function() {
                    try {
                        localStorage.setItem('authToken', '$token');
                        localStorage.setItem('userId', '$userId');
                        console.log('Auth token injected successfully');

                        // Trigger a custom event to notify the frontend
                        window.dispatchEvent(new Event('auth-ready'));
                    } catch (e) {
                        console.error('Error injecting auth:', e);
                    }
                })();
            """.trimIndent()

            webView.evaluateJavascript(script) { result ->
                Log.d(TAG, "Auth injection result: $result")
            }
        } else {
            Log.w(TAG, "No token or user available for injection")
        }
    }

    /**
     * JavaScript Bridge for communication between WebView and native Android
     */
    inner class AndroidBridge {
        @JavascriptInterface
        fun finishGame() {
            runOnUiThread {
                Toast.makeText(this@WebViewGameActivity, "Game finished", Toast.LENGTH_SHORT).show()
                finish()
            }
        }

        @JavascriptInterface
        fun showToast(message: String) {
            runOnUiThread {
                Toast.makeText(this@WebViewGameActivity, message, Toast.LENGTH_SHORT).show()
            }
        }

        @JavascriptInterface
        fun log(message: String) {
            Log.d(TAG, "JS Bridge: $message")
        }
    }

    override fun onBackPressed() {
        if (webView.canGoBack()) {
            webView.goBack()
        } else {
            super.onBackPressed()
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        webView.destroy()
    }
}
