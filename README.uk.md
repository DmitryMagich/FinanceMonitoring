# 💰 FinanceMonitoring

> Персональний фінансовий трекер для власного хостингу з автоматичною синхронізацією через Monobank.

[🇬🇧 English](README.md) · [🇺🇦 Українська](README.uk.md)

![.NET](https://img.shields.io/badge/.NET-ASP.NET%20Core-512BD4?logo=dotnet&logoColor=white)
![SQLite](https://img.shields.io/badge/database-SQLite-003B57?logo=sqlite&logoColor=white)
![Monobank API](https://img.shields.io/badge/integration-Monobank%20API-black)
![EF Core](https://img.shields.io/badge/ORM-EF%20Core-512BD4)

---

## 📖 Про проєкт

**FinanceMonitoring** — легкий персональний фінансовий трекер для власного хостингу на **ASP.NET Core** та **SQLite**. Він автоматично підтягує ваші рахунки та транзакції з **[Monobank](https://monobank.ua) API**, зберігає все в локальній базі даних і надає мінімальний REST API поряд із простим статичним фронтендом, який роздається прямо з застосунку — жодних зовнішніх сервісів, жодної залежності від хмари, всі ваші дані залишаються на вашій машині.

## ✨ Можливості

- 🔄 **Автоматична синхронізація з Monobank** — фонова служба періодично підтягує рахунки та транзакції через Monobank API
- ⏱️ **Налаштовуваний інтервал синхронізації та історичний бекфіл** — оберіть, як часто синхронізуватись і за скільки днів завантажити історію при першому запуску
- 💵 **Кілька рахунків, включно з готівкою** — вбудований рахунок "Наличные" створюється автоматично для обліку коштів поза банком
- 🗄️ **Зберігання в SQLite через EF Core** — локальна база без додаткового налаштування, з автоматичними міграціями при старті
- ⚡ **Увімкнено режим WAL** — краща конкурентність при читанні/записі
- 🌐 **Мінімальний REST API** — чисті HTTP-ендпоінти для рахунків/транзакцій через `MapRestApi()`
- 🖥️ **Вбудований статичний фронтенд** — роздається прямо з `wwwroot`, окремий сервер для фронтенду не потрібен

## 🛠️ Технології

| Компонент                 | Призначення                                    |
|-----------------------------|--------------------------------------------------|
| ASP.NET Core (C#)          | Веб-хост, REST API, фонові служби                |
| Entity Framework Core      | Доступ до даних та міграції                        |
| SQLite                     | Локальна файлова база даних                        |
| Monobank API                | Джерело рахунків та транзакцій                     |
| HttpClient + rate limiter  | Безпечні, обмежені за частотою запити до Monobank   |
| Static files (wwwroot)     | Вбудований фронтенд                                 |

## 🚀 Початок роботи

### Вимоги

- [.NET SDK](https://dotnet.microsoft.com/download) версії, вказаної в `global.json`
- Персональний API-токен [Monobank](https://monobank.ua) — отримати можна на [api.monobank.ua](https://api.monobank.ua/)

### Встановлення

```bash
# Клонуємо репозиторій
git clone https://github.com/DmitryMagich/FinanceMonitoring.git
cd FinanceMonitoring

# Відновлення та збірка
dotnet restore
dotnet build
```

### Конфігурація

Відредагуйте `appsettings.json` (або `appsettings.Development.json`) перед першим запуском:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=finance.db"
  },
  "Monobank": {
    "Token": "ваш_токен_monobank"
  },
  "MonoSync": {
    "IntervalMinutes": 30,
    "InitialBackfillDays": 30,
    "RunOnStartup": true
  },
  "Time": {
    "TimeZoneOffsetHours": 3
  }
}
```

| Ключ | Опис |
|---|---|
| `ConnectionStrings:Default` | Рядок підключення SQLite / шлях до файлу бази даних |
| `Monobank:Token` | Ваш персональний API-токен Monobank |
| `MonoSync:IntervalMinutes` | Як часто запускається фонова синхронізація |
| `MonoSync:InitialBackfillDays` | За скільки днів історії завантажити дані під час першої синхронізації |
| `MonoSync:RunOnStartup` | Чи запускати синхронізацію одразу при старті застосунку |
| `Time:TimeZoneOffsetHours` | Зміщення UTC для відображення/агрегації дат транзакцій |

> 🔒 Ніколи не комітьте реальний токен Monobank у публічний репозиторій — зберігайте його в `appsettings.Development.json`, user secrets або змінній середовища, і переконайтесь, що цей файл додано в `.gitignore`.

### Запуск

```bash
dotnet run
```

При першому запуску застосунок автоматично:
1. Застосовує міграції EF Core та створює `finance.db`.
2. Вмикає режим **WAL** у SQLite.
3. Створює типовий рахунок **"Наличные"** для обліку коштів поза Monobank.
4. Запускає фонову службу синхронізації з Monobank (якщо `RunOnStartup` дорівнює `true`).

Потім відкрийте застосунок у браузері, щоб скористатись вбудованою панеллю, яка роздається з `wwwroot`.

## 📁 Структура проєкту

```
FinanceMonitoring/
├── Entities/                  # Моделі сутностей EF Core (Account, Transaction тощо)
├── Modules/
│   ├── MonoAPI/                # Клієнт Monobank API, rate limiter, служба синхронізації
│   └── RestAPI/                # Визначення мінімальних API-ендпоінтів
├── Properties/                 # launchSettings.json тощо
├── wwwroot/                     # Статичний фронтенд (роздається застосунком)
├── AppDbContext.cs             # DbContext EF Core
├── Program.cs                  # Точка складання та запуску застосунку
├── appsettings.json            # Базова конфігурація
├── appsettings.Development.json
├── global.json                 # Зафіксована версія .NET SDK
├── FinanceCalculator.sln
└── FinanceCalculator.csproj
```

## 🔐 Про безпеку

- Файл бази даних SQLite (`finance.db`) містить ваші реальні фінансові дані — переконайтесь, що він виключений через `.gitignore` і ніколи не потрапляє в репозиторій.
- Ставтесь до токена Monobank API як до пароля: він надає доступ на читання до даних вашого банківського рахунку.

## 🤝 Контриб'юції

Це персональний пет-проєкт, але пропозиції, issue та pull request'и вітаються.
---
