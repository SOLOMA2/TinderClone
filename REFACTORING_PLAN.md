# План рефакторинга и реализации функциональности

## Критические изменения (приоритет 1)

### 1. Структура Swipes - избежание Race Condition ✅
- Переделать структуру: User1_Id, User2_Id (меньший ID всегда первый)
- Decision1, Decision2 (nullable bool)
- Композитный ключ (User1_Id, User2_Id)
- Upsert логика для предотвращения конфликтов

### 2. Необходимые пакеты
- Npgsql.EntityFrameworkCore.PostgreSQL
- Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite (PostGIS)
- AWS SDK для S3 (или Azure.Storage.Blobs / MinIO)
- StackExchange.Redis (уже есть в микросервисе)

### 3. PostGIS для геолокации
- Обновить User entity для использования NetTopologySuite.Point
- Настроить пространственные индексы
- Реализовать гео-запросы

## Важные изменения (приоритет 2)

### 4. S3 хранилище
- Интерфейс IFileStorageService
- Реализация для S3/MinIO/Azure Blob
- Интеграция в UserPhoto

### 5. CDN конфигурация
- Настройка базового URL для CDN
- Генерация CDN URLs

### 6. Redis кеширование
- Cache-Aside паттерн для профилей
- Кеширование колоды (Deck)
- Инвалидация кеша

### 7. RabbitMQ
- Уведомления о матчах (уже частично реализовано)

## Оптимизация (приоритет 3)

### 8. Индексы и оптимизация запросов
- Пространственные индексы (GIST)
- Индексы на внешние ключи
- Фильтрация дубликатов в запросах

