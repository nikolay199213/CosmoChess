# 📱 Публикация CosmoChess в Google Play

## 🎯 Шаг 1: Тестирование на реальном устройстве

### Для локального тестирования:

1. **Включите USB отладку на телефоне:**
   - Настройки → О телефоне → 7 раз нажмите на "Номер сборки"
   - Настройки → Система → Параметры разработчика → USB-отладка (включить)

2. **Подключите телефон к компьютеру по USB**

3. **Измените URL в `AppConfig.kt`:**

```kotlin
// Строка 20-23
BUILD_TYPE_DEBUG -> {
    // Замените на IP вашего компьютера в локальной сети
    "http://192.168.31.162:8080"  // <-- измените здесь
}
```

4. **Убедитесь что телефон и компьютер в одной WiFi сети**

5. **Запустите backend и frontend:**
```bash
# Backend
cd backend
dotnet run

# Frontend (новый терминал)
cd frontend
npm run dev
```

6. **Запустите приложение из Android Studio**
   - Run → Run 'app'
   - Выберите ваше устройство в списке

---

## 🚀 Шаг 2: Подготовка к Production

### 2.1. Настройка backend на сервере

Вам нужно развернуть backend на сервере с доменом:

**Варианты хостинга:**
- **Heroku** (бесплатно/платно)
- **DigitalOcean** ($5/месяц)
- **Azure** (для .NET оптимально)
- **AWS**

Пример для Azure:
```bash
# Создайте Azure App Service
az webapp up --name cosmochess-api --resource-group cosmochess-rg

# Настройте переменные окружения
az webapp config appsettings set --name cosmochess-api --settings DB_CONNECTION_STRING="..."
```

Ваш backend будет доступен по адресу: `https://cosmochess-api.azurewebsites.net`

### 2.2. Настройка frontend на сервере

**Варианты хостинга:**
- **Vercel** (бесплатно, рекомендуется)
- **Netlify** (бесплатно)
- **GitHub Pages**
- **Firebase Hosting**

Пример для Vercel:
```bash
# Установите Vercel CLI
npm install -g vercel

# Деплой
cd frontend
vercel --prod
```

Ваш frontend будет доступен: `https://cosmochess.vercel.app`

### 2.3. Обновите `AppConfig.kt` с production URL:

```kotlin
BUILD_TYPE_RELEASE -> {
    "https://cosmochess.vercel.app"  // Ваш frontend URL
}
```

Строка 30:
```kotlin
BUILD_TYPE_RELEASE -> "https://cosmochess-api.azurewebsites.net/api"  // Ваш backend URL
```

---

## 🔐 Шаг 3: Создание Release Build

### 3.1. Создайте keystore для подписи приложения

**В Android Studio или Terminal:**

```bash
cd android-app
keytool -genkey -v -keystore cosmochess-release-key.keystore -alias cosmochess -keyalg RSA -keysize 2048 -validity 10000
```

**Введите данные:**
- Пароль keystore (запомните!)
- Имя и фамилию
- Название организации
- Город, область, страну

**⚠️ ВАЖНО:** Сохраните файл `cosmochess-release-key.keystore` в безопасном месте! Без него вы не сможете обновить приложение!

### 3.2. Создайте `keystore.properties`

```bash
cd android-app
```

Создайте файл `keystore.properties`:
```properties
storePassword=ваш_пароль_keystore
keyPassword=ваш_пароль_ключа
keyAlias=cosmochess
storeFile=cosmochess-release-key.keystore
```

**⚠️ НЕ КОМM ИТЬТЕ ЭТОТ ФАЙЛ В GIT!**

Добавьте в `.gitignore`:
```
keystore.properties
*.keystore
```

### 3.3. Обновите `build.gradle` для подписи

`android-app/app/build.gradle.kts`:

```kotlin
android {
    // ... существующий код ...

    signingConfigs {
        create("release") {
            val keystorePropertiesFile = rootProject.file("keystore.properties")
            val keystoreProperties = Properties()
            keystoreProperties.load(FileInputStream(keystorePropertiesFile))

            keyAlias = keystoreProperties["keyAlias"] as String
            keyPassword = keystoreProperties["keyPassword"] as String
            storeFile = file(keystoreProperties["storeFile"] as String)
            storePassword = keystoreProperties["storePassword"] as String
        }
    }

    buildTypes {
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            signingConfig = signingConfigs.getByName("release")
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }
}
```

### 3.4. Соберите Release APK/AAB

```bash
cd android-app
.\gradlew bundleRelease
```

Файл будет здесь: `app/build/outputs/bundle/release/app-release.aab`

---

## 📦 Шаг 4: Публикация в Google Play

### 4.1. Создайте аккаунт разработчика

1. Перейдите на https://play.google.com/console
2. Зарегистрируйтесь как разработчик ($25 одноразово)
3. Заполните профиль

### 4.2. Создайте новое приложение

1. Play Console → **Create app**
2. Заполните:
   - App name: **CosmoChess**
   - Default language: **Russian** или **English**
   - App or game: **Game**
   - Free or paid: **Free**

### 4.3. Подготовьте графические материалы

**Необходимо:**
- **Иконка приложения:** 512x512 PNG
- **Feature graphic:** 1024x500 PNG
- **Скриншоты:**
  - Phone: минимум 2 скриншота (1080x1920)
  - 7" Tablet: минимум 2 скриншота
  - 10" Tablet: минимум 2 скриншота

**Сделайте скриншоты прямо из эмулятора/устройства!**

### 4.4. Заполните Store Listing

1. **App details:**
   - Short description (80 символов)
   - Full description (4000 символов)
   - App category: **Board**
   - Tags: chess, board game, strategy

2. **Graphics:**
   - Загрузите иконку
   - Загрузите feature graphic
   - Загрузите скриншоты

3. **Contact details:**
   - Email
   - Политика конфиденциальности (нужен URL)
   - Website (опционально)

### 4.5. Настройка контента

1. **Content rating:**
   - Заполните анкету (выберите "No violent content")

2. **Target audience:**
   - Выберите возрастные группы (рекомендуется: Everyone)

3. **Privacy policy:**
   - Создайте простую страницу:

```markdown
# Privacy Policy for CosmoChess

CosmoChess does not collect any personal data.
The app requires:
- Internet connection to play online games
- Local storage for authentication

We do not share any data with third parties.

Contact: your-email@example.com
```

Опубликуйте на GitHub Pages или на своем сайте.

### 4.6. Загрузите AAB файл

1. **Production → Create new release**
2. Загрузите `app-release.aab`
3. Заполните Release notes:
   ```
   Initial release
   - Play chess online
   - Play against AI
   - Realtime multiplayer
   ```

4. Нажмите **Save** → **Review release** → **Start rollout to production**

### 4.7. Пройдите ревью

Google проверит приложение (2-7 дней):
- ✅ Работоспособность
- ✅ Соответствие политике
- ✅ Графические материалы

---

## 📊 Шаг 5: Обновления приложения

### Когда хотите обновить:

1. **Увеличьте версию в `build.gradle.kts`:**

```kotlin
android {
    defaultConfig {
        versionCode = 2  // увеличьте на 1
        versionName = "1.1.0"  // новая версия
    }
}
```

2. **Соберите новый AAB:**
```bash
.\gradlew bundleRelease
```

3. **Загрузите в Play Console:**
   - Production → Create new release
   - Загрузите новый AAB
   - Release notes (что нового)
   - Start rollout

---

## ✅ Чеклист перед публикацией

- [ ] Backend развернут и работает
- [ ] Frontend развернут и работает
- [ ] `AppConfig.kt` содержит правильные production URL
- [ ] Протестировано на реальном устройстве
- [ ] Keystore создан и сохранен
- [ ] Release APK/AAB собран
- [ ] Иконка и скриншоты готовы
- [ ] Описание и графика загружены
- [ ] Политика конфиденциальности опубликована
- [ ] Content rating заполнен
- [ ] AAB загружен в Play Console

---

## 🔧 Troubleshooting

### Ошибка "Failed to install"
- Проверьте signingConfig в build.gradle
- Убедитесь что keystore.properties существует

### Приложение не подключается к backend
- Проверьте URL в AppConfig.kt
- Проверьте CORS на backend
- Проверьте SSL сертификат (для HTTPS)

### Play Console отклоняет приложение
- Проверьте политику конфиденциальности
- Убедитесь что все разрешения объяснены
- Проверьте content rating

---

## 📞 Поддержка

Если возникли проблемы:
1. Проверьте логи: `adb logcat`
2. Проверьте Google Play Console → "Pre-launch report"
3. Читайте политику Google Play: https://play.google.com/about/developer-content-policy/

---

## 🎉 Готово!

После публикации ваше приложение будет доступно в Google Play!

Пользователи смогут установить CosmoChess и играть в шахматы онлайн! ♟️
