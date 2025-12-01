package com.cosmochess

import android.annotation.SuppressLint
import android.os.Bundle
import android.util.Log
import android.webkit.*
import androidx.appcompat.app.AppCompatActivity
import com.cosmochess.config.AppConfig
import com.cosmochess.data.repository.AuthRepository
import com.cosmochess.network.ApiClient

class MainActivity : AppCompatActivity() {
    private val TAG = "MainActivity"
    private lateinit var webView: WebView
    private lateinit var authRepository: AuthRepository

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        val apiClient = ApiClient(this)
        authRepository = AuthRepository(apiClient, this)
        webView = findViewById(R.id.mainWebView)

        setupWebView()
        loadFrontend()
    }

    @SuppressLint("SetJavaScriptEnabled")
    private fun setupWebView() {
        webView.settings.apply {
            javaScriptEnabled = true
            domStorageEnabled = true
            databaseEnabled = true
            mixedContentMode = WebSettings.MIXED_CONTENT_ALWAYS_ALLOW

            // Enable debugging in WebView
            WebView.setWebContentsDebuggingEnabled(true)
        }

        // Add JavaScript bridge for Android communication
        webView.addJavascriptInterface(AndroidBridge(), "Android")

        webView.webChromeClient = object : WebChromeClient() {
            override fun onConsoleMessage(consoleMessage: ConsoleMessage?): Boolean {
                consoleMessage?.let {
                    Log.d(TAG, "WebView Console: ${it.message()} (${it.sourceId()}:${it.lineNumber()})")
                }
                return true
            }
        }

        webView.webViewClient = object : WebViewClient() {
            override fun onPageFinished(view: WebView?, url: String?) {
                super.onPageFinished(view, url)
                Log.d(TAG, "Page loaded: $url")
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

            override fun shouldOverrideUrlLoading(
                view: WebView?,
                request: WebResourceRequest?
            ): Boolean {
                // Let WebView handle all navigation internally
                return false
            }
        }
    }

    private fun loadFrontend() {
        // Load the root URL of the frontend
        val frontendUrl = AppConfig.FRONTEND_URL
        Log.d(TAG, "Loading frontend from: $frontendUrl (${if (AppConfig.IS_DEBUG) "DEBUG" else "RELEASE"})")
        webView.loadUrl(frontendUrl)
    }

    private fun injectAuthToken() {
        val token = authRepository.getToken()
        val user = authRepository.getCurrentUser()

        if (token != null && user != null) {
            val script = """
                (function() {
                    try {
                        localStorage.setItem('authToken', '$token');
                        localStorage.setItem('userId', '${user.id}');
                        console.log('Auth token injected successfully');
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
            Log.d(TAG, "No auth token to inject")
        }
    }

    inner class AndroidBridge {
        @JavascriptInterface
        fun logout() {
            runOnUiThread {
                authRepository.logout()
                // Reload to show login page
                webView.loadUrl(AppConfig.FRONTEND_URL)
            }
        }

        @JavascriptInterface
        fun showToast(message: String) {
            runOnUiThread {
                android.widget.Toast.makeText(
                    this@MainActivity,
                    message,
                    android.widget.Toast.LENGTH_SHORT
                ).show()
            }
        }

        @JavascriptInterface
        fun log(message: String) {
            Log.d(TAG, "WebView Log: $message")
        }
    }

    override fun onBackPressed() {
        if (webView.canGoBack()) {
            webView.goBack()
        } else {
            super.onBackPressed()
        }
    }
}
