# TinderClone
 
Высоконагруженная платформа знакомств с микросервисной архитектурой.
 
## Архитектура
 
- **Domain** - сущности, интерфейсы, enums
- **Application** - бизнес-логика, сервисы
- **Infrastructure** - Persistence, External Services, Cache, Message Queue
- **WebApi** - API контроллеры
- **MatchingService** - микросервис обработки свайпов
 
## Технологии
 
- .NET 8
- PostgreSQL + PostGIS
- Redis (кеширование)
- RabbitMQ (сообщения)
- AWS S3 (фото)
- Docker
 
## Запуск
 
```bash
docker-compose up -d
```
 
## Основные функции
 
- Геолокационный поиск пользователей
- Система свайпов с race condition защитой
- Кеширование профилей и рекомендаций
- Асинхронные уведомления о мэтчах
- Хранение фото в S3 с CDN
 
## Особенности реализации
 
- **Race Condition**: Композитные ключи (User1_Id, User2_Id) с Upsert
- **Геолокация**: PostGIS + NetTopologySuite, GIST индексы
- **Кеширование**: Cache-Aside паттерн для профилей, Deck кеширование
- **Микросервисы**: Отдельный Matching Service с Redis/RabbitMQ
