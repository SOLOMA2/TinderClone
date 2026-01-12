# Архитектура проекта TinderClone

## Структура слоев (Clean Architecture)

### Domain Layer (TinderClone.Domain)
- **Entities** - доменные сущности
- **Enums** - перечисления
- **Interfaces** - интерфейсы репозиториев и UnitOfWork

### Application Layer (TinderClone.Application)
- **Services** - бизнес-логика приложения
- **DTOs** - объекты передачи данных
- **Interfaces** - интерфейсы сервисов

### Infrastructure Layer (TinderClone.Infrastructure)
- **Persistence** - работа с БД (DbContext, репозитории, конфигурации)
- **ExternalServices** - интеграции с внешними сервисами (S3, CDN)
- **Cache** - Redis кеширование
- **MessageQueue** - RabbitMQ

### Presentation Layer (TinderClone.WebApi)
- **Controllers** - API контроллеры
- **Program.cs** - настройка приложения

### Microservices (TinderClone.MatchingService)
- Отдельный микросервис для обработки свайпов и матчей
- Использует Redis для кеширования
- Использует RabbitMQ для асинхронных событий

