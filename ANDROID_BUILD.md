# CosmoChess Android Build Guide

Руководство по сборке Android приложения CosmoChess.

## Требования

- **Node.js** 18+
- **Android Studio** (для сборки APK)
- **Java JDK** 17+
- **Android SDK** (устанавливается через Android Studio)

## Быстрый старт

### 1. Установка зависимостей

```bash
cd frontend
npm install
```

### 2. Настройка сервера API

Откройте файл `src/config.js` и замените URL сервера:

```javascript
// Для production
API_BASE_URL: isNative ? 'https://your-server-url.com' : '',

// Для локальной разработки с эмулятором
API_BASE_URL: isNative ? 'http://10.0.2.2' : '',

// Для локальной разработки с реальным устройством
API_BASE_URL: isNative ? 'http://192.168.x.x' : '',
```

### 3. Сборка и синхронизация

```bash
npm run android:build
```

### 4. Открытие в Android Studio

```bash
npm run android:open
```

## Сборка APK

### Debug APK

В Android Studio:
1. **Build** → **Build Bundle(s) / APK(s)** → **Build APK(s)**
2. APK будет в: `android/app/build/outputs/apk/debug/app-debug.apk`

Или через терминал:

```bash
cd android
./gradlew assembleDebug
```

### Release APK (для публикации)

1. Создайте keystore для подписи:

```bash
keytool -genkey -v -keystore cosmochess.keystore -alias cosmochess -keyalg RSA -keysize 2048 -validity 10000
```

2. Создайте файл `android/keystore.properties`:

```properties
storePassword=your_store_password
keyPassword=your_key_password
keyAlias=cosmochess
storeFile=../cosmochess.keystore
```

3. Соберите Release APK:

```bash
cd android
./gradlew assembleRelease
```

APK будет в: `android/app/build/outputs/apk/release/app-release.apk`

## NPM скрипты

| Команда | Описание |
|---------|----------|
| `npm run android:build` | Сборка Vue.js и синхронизация с Android |
| `npm run android:sync` | Синхронизация web assets с Android |
| `npm run android:open` | Открытие проекта в Android Studio |
| `npm run android:run` | Запуск на подключенном устройстве/эмуляторе |

## Структура Android проекта

```
frontend/
├── android/                    # Android проект
│   ├── app/
│   │   ├── src/main/
│   │   │   ├── assets/public/  # Собранное Vue.js приложение
│   │   │   ├── AndroidManifest.xml
│   │   │   └── java/.../MainActivity.java
│   │   └── build.gradle
│   └── gradle/
├── capacitor.config.json       # Конфигурация Capacitor
└── src/config.js               # Конфигурация API URLs
```

## Настройка для разработки

### Эмулятор Android

1. Откройте Android Studio
2. **Tools** → **Device Manager**
3. Создайте эмулятор (рекомендуется Pixel 4 с API 31+)
4. Запустите эмулятор

### Реальное устройство

1. Включите **Developer Options** на устройстве
2. Включите **USB Debugging**
3. Подключите устройство по USB
4. Выполните: `npm run android:run`

## Конфигурация

### capacitor.config.json

```json
{
  "appId": "com.cosmochess.app",
  "appName": "CosmoChess",
  "webDir": "dist",
  "android": {
    "allowMixedContent": true,
    "webContentsDebuggingEnabled": true
  }
}
```

### AndroidManifest.xml

Ключевые настройки:
- `android:usesCleartextTraffic="true"` - разрешает HTTP (для разработки)
- `android.permission.INTERNET` - доступ к интернету

## Решение проблем

### Ошибка "Network Error"

1. Проверьте URL сервера в `src/config.js`
2. Убедитесь что сервер доступен с устройства
3. Для HTTP используйте `usesCleartextTraffic="true"`

### Белый экран

1. Откройте Chrome DevTools Remote Debugging: `chrome://inspect`
2. Проверьте консоль на ошибки
3. Убедитесь что `webContentsDebuggingEnabled: true`

### Ошибка сборки Gradle

```bash
cd android
./gradlew clean
./gradlew build
```

## Публикация в Google Play

1. Соберите Release APK (см. выше)
2. Создайте аккаунт разработчика в Google Play Console
3. Создайте приложение и загрузите APK
4. Заполните информацию о приложении (описание, скриншоты, иконки)
5. Пройдите проверку Google

### Требования для публикации

- **Иконка**: 512x512 px (PNG)
- **Feature Graphic**: 1024x500 px
- **Скриншоты**: минимум 2 для каждого типа устройства
- **Описание**: до 4000 символов
- **Privacy Policy**: обязательна для приложений с аккаунтами

## Дополнительные возможности

### Добавление Splash Screen

Установите плагин:
```bash
npm install @capacitor/splash-screen
npx cap sync
```

### Push уведомления

```bash
npm install @capacitor/push-notifications
npx cap sync
```

### Оффлайн режим

Реализуйте Service Worker для кэширования в `src/main.js`.

## Контакты и поддержка

- GitHub Issues: для багов и предложений
- Документация Capacitor: https://capacitorjs.com/docs
