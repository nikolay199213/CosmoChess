# PowerShell script для тестирования микросервисной архитектуры CosmoChess

Write-Host "=== CosmoChess Microservices Test ===" -ForegroundColor Cyan
Write-Host ""

# 1. Проверка engine-service
Write-Host "1. Проверка engine-service..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "http://localhost:5001/health" -Method Get
    Write-Host "   ✓ engine-service работает" -ForegroundColor Green
    Write-Host "   Response: $healthResponse" -ForegroundColor Gray
} catch {
    Write-Host "   ✗ engine-service недоступен" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}
Write-Host ""

# 2. Проверка анализа позиции
Write-Host "2. Тест анализа начальной позиции..." -ForegroundColor Yellow
try {
    $analyzeBody = @{
        fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
        depth = 10
    } | ConvertTo-Json

    $analyzeResponse = Invoke-RestMethod -Uri "http://localhost:5001/analyze" `
        -Method Post `
        -Body $analyzeBody `
        -ContentType "application/json"

    Write-Host "   ✓ Анализ выполнен успешно" -ForegroundColor Green
    Write-Host "   Best move: $($analyzeResponse.bestMove)" -ForegroundColor Gray
    Write-Host "   Score: $($analyzeResponse.score)" -ForegroundColor Gray
} catch {
    Write-Host "   ✗ Ошибка анализа" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}
Write-Host ""

# 3. Проверка MultiPV анализа
Write-Host "3. Тест MultiPV анализа..." -ForegroundColor Yellow
try {
    $multiPvBody = @{
        fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
        depth = 10
        multiPv = 3
    } | ConvertTo-Json

    $multiPvResponse = Invoke-RestMethod -Uri "http://localhost:5001/analyze-multipv" `
        -Method Post `
        -Body $multiPvBody `
        -ContentType "application/json"

    Write-Host "   ✓ MultiPV анализ выполнен" -ForegroundColor Green
    Write-Host "   Lines count: $($multiPvResponse.lines.Count)" -ForegroundColor Gray
    Write-Host "   Top 3 moves:" -ForegroundColor Gray
    $multiPvResponse.lines | Select-Object -First 3 | ForEach-Object {
        Write-Host "     $($_.rank). $($_.move) (score: $($_.score))" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ✗ Ошибка MultiPV анализа" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}
Write-Host ""

# 4. Проверка Kafka
Write-Host "4. Проверка Kafka..." -ForegroundColor Yellow
Write-Host "   Kafka UI доступен по адресу: http://localhost:8081" -ForegroundColor Gray
Write-Host "   Откройте браузер для проверки топиков:" -ForegroundColor Gray
Write-Host "   - bot-move-requests" -ForegroundColor Gray
Write-Host "   - bot-move-results" -ForegroundColor Gray
Write-Host ""

# 5. Проверка bot-service (через логи)
Write-Host "5. Проверка bot-service..." -ForegroundColor Yellow
Write-Host "   Выполните: docker logs cosmochess-bot --tail 20" -ForegroundColor Gray
Write-Host "   Должно быть: 'BotMoveWorker started' и 'Subscribed to topic'" -ForegroundColor Gray
Write-Host ""

# 6. Тест полной цепочки (если backend запущен)
Write-Host "6. Проверка game-service (backend)..." -ForegroundColor Yellow
try {
    $backendHealth = Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get -ErrorAction SilentlyContinue
    Write-Host "   ✓ Backend доступен" -ForegroundColor Green
} catch {
    Write-Host "   ℹ Backend не запущен (запустите через Visual Studio или Docker)" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "=== Итого ===" -ForegroundColor Cyan
Write-Host "Для полного теста запустите игру с ботом через фронтенд" -ForegroundColor White
Write-Host "Цепочка: User Move → backend → bot-move-request (Kafka) → bot-service → engine-service → bot-move-result (Kafka) → backend → SignalR → frontend" -ForegroundColor Gray
Write-Host ""
Write-Host "Мониторинг:" -ForegroundColor Cyan
Write-Host "  • Kafka UI: http://localhost:8081" -ForegroundColor White
Write-Host "  • Backend logs: docker-compose logs -f backend" -ForegroundColor White
Write-Host "  • Bot logs: docker-compose logs -f bot-service" -ForegroundColor White
Write-Host "  • Engine logs: docker-compose logs -f engine-service" -ForegroundColor White
