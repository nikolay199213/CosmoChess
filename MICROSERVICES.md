# CosmoChess Microservices Architecture

## 🏗️ Архитектура

```
┌─────────────┐
│   Frontend  │
└──────┬──────┘
       │ HTTP + SignalR
       ▼
┌─────────────────┐      HTTP        ┌──────────────────┐
│  game-service   │◄─────────────────►│ engine-service   │
│   (backend)     │                   │   (Stockfish)    │
└────┬────────┬───┘                   └──────────────────┘
     │        │
     │ Kafka  │ Kafka
     │ Pub    │ Sub
     ▼        ▼
┌────────────────────────────┐
│    Apache Kafka            │
│  • bot-move-requests       │
│  • bot-move-results        │
└─────────────┬──────────────┘
              │
              │ Sub/Pub
              ▼
       ┌──────────────┐
       │ bot-service  │
       └──────────────┘
```

## 📦 Сервисы

### engine-service (порт 5001)
- **Назначение**: REST API обёртка для Stockfish
- **Зависимости**: Stockfish binary (только Linux)
- **Endpoints**:
  - `GET /health` - health check
  - `POST /analyze` - анализ позиции (single-PV)
  - `POST /analyze-multipv` - анализ позиции (multi-PV)

### bot-service
- **Назначение**: Обработка ходов бота
- **Зависимости**: Kafka, engine-service
- **Kafka**:
  - Consumer: `bot-move-requests`
  - Producer: `bot-move-results`

### game-service (backend, порт 5000)
- **Назначение**: Основной API, игровая логика
- **Зависимости**: PostgreSQL, Kafka, engine-service
- **Kafka**:
  - Producer: `bot-move-requests`
  - Consumer: `bot-move-results`

---

## 🚀 Варианты запуска

### Вариант 1: Всё в Docker (рекомендуется для первого запуска)

```bash
# 1. Сборка всех сервисов
docker-compose build

# 2. Запуск
docker-compose up -d

# 3. Проверка статусов
docker-compose ps

# 4. Логи
docker-compose logs -f backend bot-service engine-service

# 5. Остановка
docker-compose down
```

**Доступ:**
- Frontend: http://localhost
- Backend Swagger: http://localhost/swagger
- Kafka UI: http://localhost:8081
- Engine Service: http://localhost:5001

---

### Вариант 2: Инфраструктура в Docker + Backend локально

**Для отладки backend в Visual Studio**

#### Шаг 1: Запустить инфраструктуру

```bash
# Запуск Kafka, PostgreSQL, engine-service, bot-service
docker-compose -f docker-compose.dev.yml up -d

# Проверка
docker-compose -f docker-compose.dev.yml ps
```

#### Шаг 2: Запустить backend в Visual Studio

1. Открыть `backend/CosmoChess.sln`
2. Установить `CosmoChess.Api` как startup project
3. F5 (Debug) или Ctrl+F5 (без отладки)

Backend будет использовать `appsettings.Development.json` для подключения к Docker сервисам.

**Конфигурация (`appsettings.Development.json`):**
```json
{
  "ENGINE_SERVICE_URL": "http://localhost:5001",
  "KAFKA_BOOTSTRAP_SERVERS": "localhost:9092",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=cosmochess;..."
  }
}
```

---

### Вариант 3: Только микросервисы в Docker

**Для работы с уже запущенным backend**

```bash
# Запуск только новых микросервисов
docker-compose up -d kafka zookeeper engine-service bot-service
```

---

## 🧪 Тестирование

### Автоматический тест

```powershell
# PowerShell
.\test-microservices.ps1
```

Скрипт проверит:
- ✅ engine-service health
- ✅ Анализ позиции (single-PV)
- ✅ Анализ позиции (multi-PV)
- ✅ Доступность Kafka UI
- ✅ Статус bot-service

### Ручное тестирование

#### 1. Проверка engine-service

```bash
# Health check
curl http://localhost:5001/health

# Анализ позиции
curl -X POST http://localhost:5001/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "fen": "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
    "depth": 10
  }'

# MultiPV анализ
curl -X POST http://localhost:5001/analyze-multipv \
  -H "Content-Type: application/json" \
  -d '{
    "fen": "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
    "depth": 10,
    "multiPv": 3
  }'
```

#### 2. Проверка Kafka

Открыть Kafka UI: http://localhost:8081

**Топики:**
- `bot-move-requests` - запросы на ход бота
- `bot-move-results` - результаты хода бота

**Проверка сообщений:**
```bash
# Отправить тестовое сообщение
echo '{"GameId":"123e4567-e89b-12d3-a456-426614174000","Fen":"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1","Difficulty":2,"Style":0,"Timestamp":"2026-02-17T00:00:00Z"}' | \
  docker exec -i cosmochess-kafka kafka-console-producer \
    --bootstrap-server localhost:9092 \
    --topic bot-move-requests

# Прочитать результат
docker exec cosmochess-kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic bot-move-results \
  --from-beginning \
  --max-messages 1
```

#### 3. Логи сервисов

```bash
# Все логи
docker-compose logs -f

# Конкретный сервис
docker-compose logs -f bot-service
docker-compose logs -f engine-service
docker-compose logs -f backend

# Последние 50 строк
docker logs cosmochess-bot --tail 50
```

---

## 🎮 Полный тест через игру

1. Запустить всё в Docker или частично локально
2. Открыть frontend: http://localhost
3. Создать игру с ботом
4. Сделать ход белыми
5. Наблюдать цепочку:

```
User Move → backend (MakeMoveCommand)
          ↓
          BotMoveBackgroundService
          ↓
          Kafka Producer → bot-move-requests
                          ↓
                          bot-service (Consumer)
                          ↓
                          engine-service (HTTP)
                          ↓
                          Stockfish analysis
                          ↓
                          bot-service (logic)
                          ↓
                          Kafka Producer → bot-move-results
                                          ↓
                                          backend (Consumer)
                                          ↓
                                          Database + SignalR
                                          ↓
                                          Frontend (ход бота)
```

**Мониторинг:**

Откройте 3 терминала:
```bash
# Терминал 1: backend logs
docker-compose logs -f backend

# Терминал 2: bot-service logs
docker-compose logs -f bot-service

# Терминал 3: engine-service logs
docker-compose logs -f engine-service
```

Ищите в логах:
- `BotMoveBackgroundService`: "Sending bot move request to Kafka"
- `bot-service`: "Received bot move result", "Bot made move"
- `engine-service`: "Engine analysis completed"
- `backend Consumer`: "Processing bot move result", "SignalR notification sent"

---

## 🐛 Отладка

### Проблема: bot-service не получает сообщения

**Проверка:**
```bash
# Убедиться что топик создан
docker exec cosmochess-kafka kafka-topics \
  --bootstrap-server localhost:9092 \
  --list

# Проверить consumer group
docker exec cosmochess-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --describe \
  --group bot-workers
```

**Решение:**
```bash
# Пересоздать топики
docker exec cosmochess-kafka kafka-topics \
  --bootstrap-server localhost:9092 \
  --create \
  --topic bot-move-requests \
  --partitions 1 \
  --replication-factor 1

# Перезапустить bot-service
docker restart cosmochess-bot
```

### Проблема: engine-service недоступен

```bash
# Проверка контейнера
docker ps | grep engine

# Логи
docker logs cosmochess-engine

# Проверка Stockfish
docker exec cosmochess-engine which stockfish
docker exec cosmochess-engine stockfish --version
```

### Проблема: backend не подключается к Kafka

```bash
# Проверка network
docker network ls
docker network inspect cosmochess_cosmochess-network

# Проверка переменных окружения
docker exec cosmochess-backend env | grep KAFKA
```

---

## 📊 Мониторинг

### Kafka UI (http://localhost:8081)
- Топики и сообщения
- Consumer groups
- Лаги обработки

### Docker Stats
```bash
docker stats
```

### Логи в реальном времени
```bash
# Все сервисы
docker-compose logs -f

# Фильтр по ключевым словам
docker-compose logs -f | grep -E "(ERROR|Bot made move|SignalR)"
```

---

## 🔄 Обновление после изменений

```bash
# Пересборка конкретного сервиса
docker-compose build bot-service
docker-compose up -d bot-service

# Пересборка всех
docker-compose build
docker-compose up -d

# С полной пересборкой (no cache)
docker-compose build --no-cache
```

---

## 📝 Полезные команды

```bash
# Остановить всё
docker-compose down

# Остановить и удалить volumes (БД будет очищена!)
docker-compose down -v

# Пересоздать контейнеры
docker-compose up -d --force-recreate

# Посмотреть используемые порты
netstat -ano | findstr "5000 5001 9092 5432"

# Очистка Docker
docker system prune -a
```
