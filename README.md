# BizAnalytics Platform

Дипломный проект: веб-платформа для сбора, хранения, анализа и визуализации бизнес-данных из разных источников.

Эта копия подготовлена для публикации и проверки на GitHub: локальные секреты и ключи заменены на заглушки.

## Технологический стек

- Backend: ASP.NET Core Web API, Entity Framework Core, JWT
- База данных: PostgreSQL
- Frontend: React, Vite, Axios, Recharts

## Структура проекта

- `BizAnalytics.Api` - Web API, доменные сущности, контекст EF Core, контроллеры и бизнес-логика
- `BizAnalytics.Frontend` - клиент на React с авторизацией, организациями, источниками данных, импортом CSV и дашбордом аналитики
- `BizAnalytics.Api.Tests` - интеграционные тесты API

## Требования к окружению

- .NET SDK 9
- Node.js 22+ и `npm`
- PostgreSQL, запущенный локально

## Настройка базы данных

Строка подключения API по умолчанию хранится в [BizAnalytics.Api/appsettings.json](./BizAnalytics.Api/appsettings.json):

```json
"ConnectionStrings": {
  "Db": "Host=localhost;Port=5432;Database=biz_analytics;Username=biz_user;Password=replace_with_local_db_password"
}
```

Перед локальным запуском заполните заглушки в `BizAnalytics.Api/appsettings.json` или передайте значения через переменные окружения.

## Запуск backend

Из корня репозитория:

```powershell
dotnet build BizAnalyticsPlatform.sln
dotnet run --project .\BizAnalytics.Api\BizAnalytics.Api.csproj --launch-profile http
```

API в режиме разработки будет доступен по адресу `http://localhost:5027`.

При необходимости Swagger доступен по адресу `http://localhost:5027/swagger`.

## Запуск frontend

В этой копии локальный файл `BizAnalytics.Frontend/.env.development` не хранится. При необходимости создайте его на основе [`.env.example`](./BizAnalytics.Frontend/.env.example):

```powershell
Copy-Item .\BizAnalytics.Frontend\.env.example .\BizAnalytics.Frontend\.env.development
```

```powershell
cd .\BizAnalytics.Frontend
npm install
npm run dev
```

После запуска откройте `http://localhost:5173`.

Если нужно подключить другой API, измените `VITE_API_BASE_URL` в `BizAnalytics.Frontend/.env.development`.

## Локализация

- Интерфейс поддерживает переключение языков `RU / EN`
- Выбранный язык сохраняется в `localStorage`
- Frontend отправляет язык в API через заголовок `Accept-Language`
- Сообщения API об успехе и ошибках тоже локализованы

## Запуск тестов

Из корня репозитория:

```powershell
dotnet test .\BizAnalytics.Api.Tests\BizAnalytics.Api.Tests.csproj
```

Интеграционные тесты используют in-memory базу EF Core и покрывают:

- регистрацию и вход пользователя
- создание и получение организаций
- импорт CSV
- аналитику: выручка, топ товаров и сводные KPI
- ограничение доступа к чужим организациям
- локализацию ответов API

## Быстрая проверка проекта

Из корня репозитория:

```powershell
dotnet build BizAnalyticsPlatform.sln
dotnet test .\BizAnalytics.Api.Tests\BizAnalytics.Api.Tests.csproj
cd .\BizAnalytics.Frontend
npm install
npm run build
```

Это проверит:

- сборку backend
- интеграционные тесты API
- production-сборку frontend

## Production-конфигурация

- Используйте [`.env.production.example`](./.env.production.example) как шаблон для `.env.production`
- Для публичной проверки оставлен безопасный режим `KGD_REGISTRY_MODE=demo`
- Перед реальным деплоем замените все `replace_with_...` значения на свои

## Ручной сценарий демонстрации

1. Зарегистрируйте нового пользователя.
2. Выполните вход в систему.
3. Создайте организацию.
4. Создайте источник данных типа `CSV`.
5. Загрузите CSV-файл в формате `Date,ProductName,Quantity,Amount`.
6. Откройте страницу аналитики и проверьте:
   - карточки KPI
   - график выручки по дням
   - таблицу топ товаров
   - фильтрацию по периоду
7. Переключите язык `RU / EN` и убедитесь, что интерфейс и ответы API локализуются.

## Пример CSV

```csv
Date,ProductName,Quantity,Amount
2026-03-01,Ноутбук,1,350000
2026-03-02,Мышка,2,12000
2026-03-02,Клавиатура,1,18000
```
