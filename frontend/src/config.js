import { Capacitor } from '@capacitor/core'

// Определяем платформу и базовый URL
const isNative = Capacitor.isNativePlatform()

// Конфигурация API
// Для веб-версии используем относительные пути (через Vite proxy)
// Для мобильной версии используем полный URL сервера
const config = {
  // Замените на URL вашего сервера для мобильного приложения
  // Примеры:
  // - Локальная разработка: 'http://10.0.2.2' (Android эмулятор)
  // - Локальная разработка: 'http://192.168.x.x' (реальное устройство)
  // - Production: 'https://your-domain.com'
  API_BASE_URL: isNative ? 'https://your-server-url.com' : '',

  // SignalR Hub URL
  get SIGNALR_URL() {
    return isNative
      ? `${this.API_BASE_URL}/api/gamehub`
      : '/api/gamehub'
  },

  // API endpoint prefix
  get API_PREFIX() {
    return isNative
      ? `${this.API_BASE_URL}/api`
      : '/api'
  },

  // Платформа
  isNative,
  platform: Capacitor.getPlatform()
}

export default config
