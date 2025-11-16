# Google OAuth Setup Guide

## Настройка Google OAuth для CosmoChess

### 1. Создание Google Cloud Project

1. Перейдите в [Google Cloud Console](https://console.cloud.google.com/)
2. Создайте новый проект или выберите существующий
3. Включите Google+ API для вашего проекта

### 2. Настройка OAuth 2.0 Credentials

1. Перейдите в раздел "APIs & Services" > "Credentials"
2. Нажмите "Create Credentials" > "OAuth 2.0 Client ID"
3. Выберите тип приложения "Web application"
4. Добавьте авторизованные JavaScript origins:
   - `http://localhost:5173` (для разработки)
   - Ваш production домен (например, `https://cosmochess.com`)
5. Скопируйте Client ID

### 3. Конфигурация Frontend

Откройте файл `/frontend/src/views/LoginView.vue` и замените `YOUR_GOOGLE_CLIENT_ID` на ваш Client ID:

```html
<div id="g_id_onload"
     data-client_id="ВАШ_GOOGLE_CLIENT_ID"
     data-callback="handleGoogleCallback">
</div>
```

### 4. Как это работает

1. Пользователь нажимает кнопку "Sign in with Google"
2. Google показывает окно авторизации
3. После успешной авторизации Google отправляет ID token
4. Frontend отправляет ID token на backend (`POST /api/auth/google`)
5. Backend проверяет токен через Google API
6. Если пользователь новый, создается новая запись в БД
7. Backend возвращает JWT токен
8. Frontend сохраняет токен и перенаправляет на `/games`

### 5. Безопасность

- ID токен проверяется на backend через официальную библиотеку Google.Apis.Auth
- Пароль не требуется для Google пользователей
- GoogleId хранится в БД для идентификации пользователя

### 6. Тестирование

1. Запустите backend и frontend
2. Перейдите на страницу логина
3. Нажмите кнопку "Sign in with Google"
4. Войдите через Google аккаунт
5. Вы должны быть перенаправлены на страницу игр

## Troubleshooting

### Кнопка Google не появляется
- Проверьте, что скрипт Google GSI загружен в `index.html`
- Откройте консоль браузера для проверки ошибок

### Ошибка "Invalid Google token"
- Проверьте, что Client ID правильно указан
- Убедитесь, что домен добавлен в авторизованные origins

### Ошибка при создании пользователя
- Проверьте, что миграция БД применена
- Проверьте логи backend для деталей
