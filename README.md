# 💰 FinanceMonitoring

> A self-hosted personal finance tracker with automatic Monobank synchronization.

[🇬🇧 English](README.md) · [🇺🇦 Українська](README.uk.md)

![.NET](https://img.shields.io/badge/.NET-ASP.NET%20Core-512BD4?logo=dotnet&logoColor=white)
![SQLite](https://img.shields.io/badge/database-SQLite-003B57?logo=sqlite&logoColor=white)
![Monobank API](https://img.shields.io/badge/integration-Monobank%20API-black)
![EF Core](https://img.shields.io/badge/ORM-EF%20Core-512BD4)

---

## 📖 About

**FinanceMonitoring** is a lightweight, self-hosted personal finance tracker built with **ASP.NET Core** and **SQLite**. It automatically pulls your accounts and transactions from the **[Monobank](https://monobank.ua) API**, keeps everything in a local database, and exposes a minimal REST API backed by a simple static frontend served straight from the app — no external services, no cloud dependency, your data stays on your machine.

## ✨ Features

- 🔄 **Automatic Monobank sync** — a background service periodically fetches accounts & transactions via the Monobank API
- ⏱️ **Configurable sync interval and historical backfill** — choose how often to sync and how many days back to pull on first run
- 💵 **Multiple accounts, including cash** — a built-in "Cash" account is auto-created for tracking money outside your bank
- 🗄️ **SQLite storage via EF Core** — zero-setup local database with automatic migrations on startup
- ⚡ **WAL mode enabled** — better concurrency for read/write operations
- 🌐 **Minimal REST API** — clean HTTP endpoints for accounts/transactions, mapped via `MapRestApi()`
- 🖥️ **Built-in static frontend** — served directly from `wwwroot`, no separate frontend server required

## 🛠️ Tech Stack

| Component             | Purpose                                     |
|-------------------------|-----------------------------------------------|
| ASP.NET Core (C#)      | Web host, REST API, background services       |
| Entity Framework Core  | Data access & migrations                       |
| SQLite                 | Local, file-based database                     |
| Monobank API            | Source of accounts & transactions               |
| HttpClient + rate limiter | Safe, throttled requests to Monobank           |
| Static files (wwwroot) | Built-in frontend                               |

## 🚀 Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) matching the version in `global.json`
- A [Monobank](https://monobank.ua) personal API token — get one at [api.monobank.ua](https://api.monobank.ua/)

### Installation

```bash
# Clone the repository
git clone https://github.com/DmitryMagich/FinanceMonitoring.git
cd FinanceMonitoring

# Restore & build
dotnet restore
dotnet build
```

### Configuration

Edit `appsettings.json` (or `appsettings.Development.json`) before the first run:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=finance.db"
  },
  "Monobank": {
    "Token": "your_monobank_token_here"
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

| Key | Description |
|---|---|
| `ConnectionStrings:Default` | SQLite connection string / database file path |
| `Monobank:Token` | Your personal Monobank API token |
| `MonoSync:IntervalMinutes` | How often the background sync job runs |
| `MonoSync:InitialBackfillDays` | How many days of history to pull on the very first sync |
| `MonoSync:RunOnStartup` | Whether to trigger a sync immediately when the app starts |
| `Time:TimeZoneOffsetHours` | UTC offset used for displaying/aggregating transaction dates |

> 🔒 Never commit a real Monobank token to a public repository — keep it in `appsettings.Development.json`, user secrets, or an environment variable, and make sure that file is gitignored.

### Run

```bash
dotnet run
```

On first launch, the app automatically:
1. Applies EF Core migrations and creates `finance.db`.
2. Enables SQLite **WAL** mode.
3. Creates a default **"Cash"** account for tracking money outside of Monobank.
4. Starts the background Monobank sync service (if `RunOnStartup` is `true`).

Then open the app in your browser to use the built-in dashboard served from `wwwroot`.

## 📁 Project Structure

```
FinanceMonitoring/
├── Entities/                  # EF Core entity models (Account, Transaction, etc.)
├── Modules/
│   ├── MonoAPI/                # Monobank API client, rate limiter, sync service
│   └── RestAPI/                # Minimal API endpoint definitions
├── Properties/                 # launchSettings.json etc.
├── wwwroot/                     # Static frontend (served by the app)
├── AppDbContext.cs             # EF Core DbContext
├── Program.cs                  # App composition root & startup
├── appsettings.json            # Base configuration
├── appsettings.Development.json
├── global.json                 # Pinned .NET SDK version
├── FinanceCalculator.sln
└── FinanceCalculator.csproj
```

## 🔐 Security Notes

- The SQLite database file (`finance.db`) contains your real financial data — make sure it's excluded via `.gitignore` and never pushed to the repository.
- Treat your Monobank API token like a password: it grants read access to your bank account data.

## 🤝 Contributing

This is a personal pet project, but suggestions, issues, and pull requests are welcome.
---
