package com.cosmochess.config

object AppConfig {
    // Build types
    const val BUILD_TYPE_DEBUG = "debug"
    const val BUILD_TYPE_RELEASE = "release"

    // Get current build type from BuildConfig
    private val buildType = com.cosmochess.BuildConfig.BUILD_TYPE

    /**
     * Frontend URL - автоматически выбирается в зависимости от build type
     *
     * Debug (эмулятор): http://10.0.2.2:8080
     * Debug (реальное устройство): http://192.168.31.162:8080
     * Release (production): https://your-domain.com
     */
    val FRONTEND_URL: String
        get() = when (buildType) {
            BUILD_TYPE_DEBUG -> {
                // Для локальной разработки
                // Измените на IP вашего компьютера для реального устройства
                "http://192.168.31.162:8080"
            }
            BUILD_TYPE_RELEASE -> {
                // Production URL (замените на ваш домен)
                "https://cosmochess.com"
            }
            else -> "http://10.0.2.2:8080"
        }

    /**
     * Backend API URL - автоматически определяется
     */
    val API_BASE_URL: String
        get() = when (buildType) {
            BUILD_TYPE_DEBUG -> "http://192.168.31.162:5000/api"
            BUILD_TYPE_RELEASE -> "https://api.cosmochess.com/api"
            else -> "http://10.0.2.2:5000/api"
        }

    val IS_DEBUG: Boolean
        get() = buildType == BUILD_TYPE_DEBUG

    val IS_PRODUCTION: Boolean
        get() = buildType == BUILD_TYPE_RELEASE
}
