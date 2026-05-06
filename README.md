# MarketDashboard

> **Lucrare universitară** — Proiectarea Aplicațiilor Web  
> **Student:** Adrian Diaconescu  
> **Facultate:** Universitatea din București - Facultatea de Matematică și Informatică 
> **An universitar:** 2025–2026

Aplicație web **Full-Stack** în C# pentru vizualizarea și monitorizarea datelor financiare în timp real, construită pe ASP.NET Core 8, Blazor Server și SignalR.

---

## Descriere

**MarketDashboard** este o platformă web care permite utilizatorilor autentificați să urmărească prețuri bursiere în timp real, să configureze alerte de preț și să vizualizeze istoricul OHLCV (Open/High/Low/Close/Volume) al instrumentelor financiare.

Datele sunt preluate din **Alpha Vantage API** printr-un serviciu de polling configurat ca `IHostedService` și transmise live către browser prin **SignalR WebSockets** — fără reload de pagină.

Proiectul demonstrează aplicarea practică a conceptelor:
- **Clean Architecture** (separare Core / Infrastructure / Web)
- **Repository Pattern** și **Dependency Injection**
- **Real-time communication** prin SignalR
- **Authentication & Authorization** cu ASP.NET Core Identity
- **Code-First database design** cu Entity Framework Core 8

---

## Arhitectură

```
┌─────────────────────────────────────────────────────┐
│              EXTERNAL DATA SOURCES                  │
│   Alpha Vantage REST API  |  C++ Market Processor   │
└──────────────┬──────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────┐
│         ASP.NET Core 8 Application                  │
│                                                     │
│  IMarketDataSource (interface)                      │
│     └── AlphaVantageClient                          │
│     └── CppSharedDbDataSource (optional)            │
│                                                     │
│  MarketDataPollingWorker (IHostedService)           │
│     ├── Polls data → writes to PostgreSQL           │
│     ├── Evaluates price alerts                      │
│     └── Pushes updates via SignalR                  │
│                                                     │
│  SignalR Hub (MarketHub)                            │
│     └── Real-time push to Blazor components         │
│                                                     │
│  Blazor Server UI                                   │
│     ├── Dashboard (live charts)                     │
│     ├── Watchlist                                   │
│     ├── Price Alerts                                │
│     ├── Historical OHLCV View                       │
│     └── Admin Panel                                 │
└─────────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────┐
│         PostgreSQL 16 (Docker)                      │
│  AspNetUsers · Symbols · MarketPrices               │
│  OhlcvRecords · WatchlistItems · PriceAlerts        │
└─────────────────────────────────────────────────────┘
```

---

## Stack Tehnologic

| Categorie | Tehnologie | Versiune |
|---|---|---|
| Framework | ASP.NET Core | 8.0 |
| UI | Blazor Server | 8.0 |
| Real-time | SignalR | 8.0 |
| ORM | Entity Framework Core | 8.0 |
| Bază de date | PostgreSQL | 16 |
| Driver DB | Npgsql | 8.0 |
| Autentificare | ASP.NET Core Identity | 8.0 |
| Date externe | Alpha Vantage API | Free tier |
| Container | Docker / Docker Compose | — |
| Limbaj | C# | 12 |
| Arhitectură | Clean Architecture | 3 straturi |

---

## 📁 Structura Proiectului

```
MarketDashboard/
├── src/
│   ├── MarketDashboard.Core/           # Domeniu — entități, interfețe, DTO-uri
│   │   ├── Entities/                   # BaseEntity, Symbol, MarketPrice, ...
│   │   ├── Interfaces/                 # IMarketDataSource, IWatchlistService, ...
│   │   ├── DTOs/                       # PriceUpdateDto, OhlcvDto, ...
│   │   └── Enums/                      # AlertDirection, DataSourceProvider
│   │
│   ├── MarketDashboard.Infrastructure/ # Date — EF Core, Identity, servicii externe
│   │   ├── Data/                       # AppDbContext, ApplicationUser, migrații
│   │   ├── ExternalData/               # AlphaVantageClient, CppSharedDbDataSource
│   │   └── Services/                   # RoleSeeder, implementări servicii
│   │
│   └── MarketDashboard.Web/            # Prezentare — Blazor, SignalR Hub, Auth
│       ├── Components/                 # Pagini și componente Blazor
│       │   ├── Account/                # Register, Login, Logout
│       │   ├── Dashboard/              # Live dashboard cu grafice
│       │   ├── Watchlist/              # Gestiune watchlist
│       │   ├── Alerts/                 # Configurare alerte de preț
│       │   └── Admin/                  # Panou administrare
│       ├── Hubs/                       # MarketHub (SignalR)
│       ├── Auth/                       # IdentityComponentsEndpointRouteBuilderExtensions
│       └── Program.cs
│
├── docker/
│   └── docker-compose.yml              # PostgreSQL 16 container
└── README.md
```

---

## Schema Bazei de Date

```
AspNetUsers ──< WatchlistItems (userId FK)
AspNetUsers ──< PriceAlerts   (userId FK)
PriceAlerts ──< AlertHistory  (alertId FK)
MarketPrices (symbol, price, recordedAt, source)
OhlcvRecords (symbol, open, high, low, close, volume, periodStart)
Symbols      (ticker, companyName, isActive)
```

---

## Stadiul Implementării

| Modul | Descriere | Status |
|---|---|---|
| **Module 1** | Solution Scaffold & NuGet Setup | ✅ Complet |
| **Module 2** | Domain Entities & Core Interfaces | ✅ Complet |
| **Module 3** | EF Core DbContext & Migrations | ✅ Complet |
| **Module 4** | ASP.NET Core Identity — Auth | ✅ Complet |
| **Module 5** | Alpha Vantage Data Source | 🔄 În progres |
| **Module 6** | IHostedService Polling Worker | ⏳ Planificat |
| **Module 7** | SignalR Hub (MarketHub) | ⏳ Planificat |
| **Module 8** | Blazor App Shell & Auth UI | ⏳ Planificat |
| **Module 9** | Watchlist Feature | ⏳ Planificat |
| **Module 10** | Real-Time Dashboard | ⏳ Planificat |
| **Module 11** | Price Alerts Feature | ⏳ Planificat |
| **Module 12** | Historical OHLCV View | ⏳ Planificat |
| **Module 13** | Admin Panel | ⏳ Planificat |
| **Module 14** | Seed Data & Demo Mode | ⏳ Planificat |
| **Module 15** | C++ Integration Point | ⏳ Planificat |

---

## Rulare Locală

### Cerințe

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [dotnet-ef tool](https://docs.microsoft.com/en-us/ef/core/cli/dotnet)

```bash
dotnet tool install --global dotnet-ef
```

### Pași

**1. Clonare repository:**
```bash
git clone https://github.com/<username>/MarketDashboard.git
cd MarketDashboard
```

**2. Pornire bază de date:**
```bash
docker compose -f docker/docker-compose.yml up -d
```

**3. Aplicare migrații:**
```bash
dotnet ef database update \
  --project src/MarketDashboard.Infrastructure \
  --startup-project src/MarketDashboard.Web
```

**4. Rulare aplicație:**
```bash
dotnet run --project src/MarketDashboard.Web
```

**5. Deschide în browser:** `http://localhost:5000`

---

## Configurare

Creează fișierul `src/MarketDashboard.Web/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=marketdashboard;Username=marketdashboard;Password=marketdashboard"
  },
  "AlphaVantage": {
    "ApiKey": "demo"
  }
}
```

> **Notă:** Cheia `demo` funcționează doar pentru simbolul `IBM`. Pentru date complete, înregistrează-te gratuit pe [alphavantage.co](https://www.alphavantage.co/support/#api-key) și obține o cheie personală.

---

## Roluri utilizatori

| Rol | Permisiuni |
|---|---|
| **User** | Dashboard, Watchlist, Alerte, Vizualizare historice |
| **Admin** | Toate permisiunile User + gestiune simboluri, gestiune utilizatori |

---

## Concepte Demonstrate

- **Clean Architecture** — dependențe unidirecționale Core ← Infrastructure ← Web
- **SOLID Principles** — în special Open/Closed prin `IMarketDataSource`
- **Dependency Injection** — toate serviciile înregistrate în DI container
- **Repository Pattern** — acces la date prin EF Core + interfețe
- **Observer Pattern** — SignalR pentru notificări în timp real
- **Background Services** — `IHostedService` pentru polling asincron
- **Code-First Migrations** — schema DB gestionată prin EF Core
- **Role-Based Authorization** — Identity cu roluri Admin/User

---

## Resurse

- [ASP.NET Core 8 Documentation](https://docs.microsoft.com/aspnet/core)
- [Blazor Server Documentation](https://docs.microsoft.com/aspnet/core/blazor)
- [SignalR Documentation](https://docs.microsoft.com/aspnet/core/signalr)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Alpha Vantage API](https://www.alphavantage.co/documentation/)

---

*Proiect realizat pentru materia Proiectarea Aplicațiilor Web — 2025/2026*
