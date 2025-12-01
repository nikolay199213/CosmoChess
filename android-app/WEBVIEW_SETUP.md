# WebView Integration Setup

## ✅ Что сделано

### 1. Создан WebViewGameActivity
**Файл:** `app/src/main/java/com/cosmochess/ui/game/WebViewGameActivity.kt`

**Функционал:**
- Загружает фронтенд в WebView
- Автоматически инжектит токен авторизации в localStorage
- JavaScript Bridge для взаимодействия с нативным кодом
- Поддержка отладки WebView (Chrome DevTools)

### 2. JavaScript Bridge
Доступен из фронтенда через `window.Android`:

```javascript
// Показать Toast из WebView
window.Android.showToast("Hello from WebView!");

// Закрыть игру
window.Android.finishGame();

// Логирование в Android logcat
window.Android.log("Debug message");
```

### 3. Обновлена навигация
- `BotGameSetupFragment` теперь открывает WebView
- `OnlineGamesFragment` теперь открывает WebView
- Старый `GameFragment` больше не используется

## 🚀 Как запустить

### Шаг 1: Запустите фронтенд
```bash
cd frontend
npm run dev
```
Фронтенд должен быть доступен на `http://localhost:8080`

### Шаг 2: Убедитесь, что backend запущен
```bash
cd backend
dotnet run
```
Backend должен быть на `http://localhost:5000`

### Шаг 3: Соберите и запустите Android приложение
```bash
# В Android Studio: Build > Rebuild Project
# Или через Gradle:
./gradlew assembleDebug
```

### Шаг 4: Проверьте URL в WebViewGameActivity
По умолчанию используется:
- Backend: `http://10.0.2.2:5000` (эмулятор Android)
- Frontend: `http://10.0.2.2:8080`

Если используете реальное устройство, замените на IP вашего компьютера:
```kotlin
// В WebViewGameActivity.kt:
val frontendUrl = "http://192.168.1.XXX:8080" // ваш IP
```

## 🔍 Отладка

### Chrome DevTools для WebView
1. Откройте Chrome на компьютере
2. Перейдите на `chrome://inspect/#devices`
3. Найдите ваше устройство/эмулятор
4. Нажмите "Inspect" на WebView

Вы увидите полноценный DevTools с:
- Console (лог JavaScript)
- Network (запросы)
- Elements (DOM)
- Sources (отладка JS)

### Логи Android
```bash
# Фильтр только WebView
adb logcat -s WebViewGameActivity

# Все логи приложения
adb logcat | grep com.cosmochess
```

## 📝 Важные моменты

### Автоинжект токена
При загрузке страницы автоматически выполняется:
```javascript
localStorage.setItem('authToken', 'YOUR_TOKEN');
localStorage.setItem('userId', 'USER_ID');
window.dispatchEvent(new Event('auth-ready'));
```

Фронтенд может слушать событие `auth-ready`:
```javascript
window.addEventListener('auth-ready', () => {
  console.log('Auth token ready!');
  // Инициализация приложения
});
```

### CORS
Если возникают проблемы с CORS:
1. Убедитесь, что backend разрешает CORS для `http://10.0.2.2:8080`
2. В `Program.cs` должно быть:
```csharp
app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
```

### Cleartext traffic
`android:usesCleartextTraffic="true"` уже добавлен в `AndroidManifest.xml` для работы с HTTP (не HTTPS).

## 🎯 Преимущества WebView подхода

✅ **Один UI код** для веба и Android
✅ **Красивая шахматная доска** из vue3-chessboard
✅ **Быстрое добавление фич** - пишете только на фронтенде
✅ **Легче поддерживать** - один код вместо двух
✅ **Автоматические обновления** UI при изменении фронтенда

## 🔮 Следующие шаги

### Опционально: Progressive Web App (PWA)
Можно добавить:
- Офлайн режим через Service Workers
- Кеширование для быстрой загрузки
- App-like experience

### Опционально: Улучшение нативной интеграции
```kotlin
// Добавить в AndroidBridge больше функций:
@JavascriptInterface
fun shareGame(gameId: String) {
    // Нативный share dialog
}

@JavascriptInterface
fun vibrate(duration: Int) {
    // Вибрация устройства
}
```

## ❓ Troubleshooting

### Белый экран в WebView
- Проверьте логи: `adb logcat -s WebViewGameActivity`
- Проверьте, что фронтенд запущен на :8080
- Проверьте URL в Chrome DevTools

### Токен не инжектится
- Проверьте, что пользователь залогинен
- Проверьте логи: должно быть "Auth token injected successfully"
- Проверьте localStorage в Chrome DevTools

### SignalR не подключается
- Проверьте, что backend на :5000
- Проверьте CORS настройки
- Проверьте токен в headers

## 📚 Документация
- [Android WebView](https://developer.android.com/guide/webapps/webview)
- [JavaScript Interface](https://developer.android.com/develop/ui/views/layout/webapps/webview#BindingJavaScript)
- [Chrome DevTools для WebView](https://developer.chrome.com/docs/devtools/remote-debugging/webviews)
