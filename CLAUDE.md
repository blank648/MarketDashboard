# MarketDashboard — CLAUDE.md

> **Proiect:** Financial Market Data Dashboard (PAW — Proiectarea Aplicațiilor Web)  
> **Owner:** Adrian Diaconescu, student CS București  
> **Status:** MVP complet (Module 1–14 + A/B/C/D/E). Modul 15 (C++ Integration) — opțional.  
> **Companion project:** `market-data-processor` (C++ — see its own CLAUDE.md)

---

## 1. Arhitectura Proiectului

### Stack

| Layer | Tehnologie | Versiune |
|-------|-----------|---------|
| Web Framework | ASP.NET Core | 8.0 |
| UI | Blazor Server | .NET 8 |
| Real-time | SignalR | .NET 8 built-in |
| ORM | EF Core | 8.0.0 |
| Database driver | Npgsql.EF Core PostgreSQL | 8.0.0 |
| Auth | ASP.NET Core Identity + EF | 8.0.0 |
| API docs | Swashbuckle.AspNetCore | 6.5.0 |
| Database | PostgreSQL 16 | (Docker) |

### Pattern Arhitectural — Clean Architecture, 3 proiecte

```
MarketDashboard.sln
├── src/MarketDashboard.Core/          ← Domeniu pur, zero dependențe externe
│   ├── Entities/                      ← BaseEntity, Symbol, MarketPrice, OhlcvRecord,
│   │                                      WatchlistItem, PriceAlert, AlertHistory
│   ├── Interfaces/                    ← IMarketDataSource, IWatchlistService,
│   │                                      IAlertService, ISymbolService,
│   │                                      IRepository<T>, IOhlcvRepository
│   ├── Enums/                         ← AlertDirection, DataSourceProvider
│   └── DTOs/                          ← PriceUpdateDto, OhlcvDto, MarketSnapshotDto,
│                                          ApiDtos.cs (REST response models)
│
├── src/MarketDashboard.Infrastructure/ ← EF Core, HTTP clients, workers, services
│   ├── Data/                          ← AppDbContext, ApplicationUser,
│   │                                      DesignTimeDbContextFactory
│   ├── Data/Configurations/           ← Fluent API per entitate (IEntityTypeConfiguration<T>)
│   ├── DataSources/                   ← AlphaVantageDataSource, CppSharedDbDataSource
│   ├── Repositories/                  ← Repository<T>, OhlcvRepository
│   ├── Services/                      ← OhlcvService, WatchlistService,
│   │                                      AlertService, SymbolService, AdminService
│   ├── Workers/                       ← MarketDataPollingWorker (IHostedService)
│   └── InfrastructureServiceExtensions.cs
│
└── src/MarketDashboard.Web/           ← Blazor Server UI + REST API
    ├── Program.cs
    ├── Controllers/                   ← SymbolsController, PricesController,
    │                                      OhlcvController, WatchlistController,
    │                                      AlertsController
    ├── Components/
    │   ├── App.razor
    │   ├── Layout/                    ← MainLayout.razor, NavMenu.razor
    │   └── Pages/
    │       ├── Dashboard.razor        ← Real-time SignalR + JS Widget
    │       ├── Watchlist.razor        ← EditForm + IOhlcvRepository
    │       ├── Alerts.razor           ← EditForm + IAlertService
    │       ├── Historical.razor       ← OHLCV chart
    │       ├── Account/               ← Login, Register, Logout (Identity scaffold)
    │       └── Admin/                 ← UserManagement, SymbolManagement,
    │                                      DataSourceStatus (M15)
    ├── Hubs/MarketHub.cs              ← SignalR Hub
    ├── Models/ValidationModels.cs     ← AddSymbolModel, CreateAlertModel, AddWatchlistModel
    ├── Middleware/                    ← ExceptionHandlingMiddleware
    └── wwwroot/
        ├── css/app.css
        └── js/marketApiWidget.js      ← Vanilla JS fetch widget (Module D)
```

---

## 2. Baza de Date

### Conexiune

```bash
# .env sau variabile de mediu (NICIODATĂ hardcodate în cod)
MARKETDASHBOARD_DB=Host=localhost;Port=5432;Database=marketdashboard;Username=marketdashboard;Password=marketdashboard
ALPHA_VANTAGE_API_KEY=your_key_here
```

### Schema completă

```sql
-- Identity (auto-gestionat de ASP.NET Identity)
AspNetUsers         -- ApplicationUser (Id, Email, UserName, ...)
AspNetRoles         -- Role (Admin, User)
AspNetUserRoles     -- pivot

-- Domeniu
Symbols             -- Id, Ticker (UNIQUE), CompanyName, IsActive
MarketPrices        -- Id, Symbol, Price, Volume, RecordedAt, Source
OhlcvRecords        -- Id, SymbolId (FK), Symbol, Open, High, Low, Close,
                   --    Volume, PeriodStart, PeriodEnd
WatchlistItems      -- Id, UserId (FK → AspNetUsers), Symbol, AddedAt
PriceAlerts         -- Id, UserId (FK), Symbol, ThresholdPrice,
                   --    Direction (Above/Below), IsActive
AlertHistory        -- Id, AlertId (FK → PriceAlerts), TriggeredAt, PriceAtTrigger
```

### Index critic de performanță

```sql
-- Creat în Fluent API Configuration
CREATE INDEX IX_MarketPrices_Symbol_RecordedAt
ON "MarketPrices" ("Symbol", "RecordedAt" DESC);
```

### Migrations

```bash
# Creare migrație
dotnet ef migrations add <NumeMigratie>   --project src/MarketDashboard.Infrastructure   --startup-project src/MarketDashboard.Web

# Aplicare
dotnet ef database update   --project src/MarketDashboard.Infrastructure   --startup-project src/MarketDashboard.Web
```

---

## 3. Comenzi Esențiale

```bash
# Build
dotnet build MarketDashboard.sln

# Run (development)
dotnet run --project src/MarketDashboard.Web --urls http://localhost:5005

# Docker PostgreSQL
docker compose -f docker/docker-compose.yml up -d
docker compose -f docker/docker-compose.yml down

# Verificare DB directă
docker exec marketdashboard-postgres psql   -U marketdashboard -d marketdashboard   -c "SELECT COUNT(*) FROM \"OhlcvRecords\";"

# Seed data check
docker exec marketdashboard-postgres psql   -U marketdashboard -d marketdashboard   -c "SELECT ticker FROM \"Symbols\" ORDER BY ticker;"
```

---

## 4. Convenții de Cod (OBLIGATORII — aplică la toate fișierele generate)

### C# Global

```csharp
// .csproj — toate proiectele
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<TargetFramework>net8.0</TargetFramework>

// File-scoped namespaces (C# 10+)
namespace MarketDashboard.Core.Entities;   // ✅
namespace MarketDashboard.Core.Entities { } // ❌

// Primary constructors (C# 12)
public class OhlcvService(IOhlcvRepository repository, ILogger<OhlcvService> logger) { }

// Records pentru DTOs
public record PriceUpdateDto(string Symbol, decimal Price, DateTime RecordedAt);

// Structured logging — NICIODATĂ string interpolation
logger.LogInformation("Polling {Count} symbols", count);  // ✅
logger.LogInformation($"Polling {count} symbols");         // ❌

// CancellationToken propagat la TOATE apelurile async
await _context.SaveChangesAsync(cancellationToken);  // ✅
await _context.SaveChangesAsync();                   // ❌
```

### Blazor

```razor
// EditForm pattern (Module E — toate formularele)
<EditForm Model="_model" OnValidSubmit="HandleSubmitAsync" FormName="form-name">
    <DataAnnotationsValidator />
    <InputText @bind-Value="_model.Field" />
    <ValidationMessage For="() => _model.Field" class="validation-message" />
    <button type="submit">Submit</button>
</EditForm>

// Auto-uppercase pentru ticker inputs
@bind-Value:after="() => _model.Ticker = _model.Ticker?.ToUpper() ?? string.Empty"

// IAsyncDisposable pe componente cu SignalR
@implements IAsyncDisposable
public async ValueTask DisposeAsync() => await _hubConnection.DisposeAsync();
```

### REST Controllers

```csharp
// Toate controllerele mapează la DTOs — NICIODATĂ entități directe în răspuns
return Ok(_mapper.Map<SymbolDto>(entity));  // ✅ (sau manual map)
return Ok(entity);                          // ❌

// Status codes corecte
return CreatedAtAction(nameof(GetById), new { id = created.Id }, dto);  // 201
return NotFound(new { message = $"Symbol {id} not found." });            // 404
return BadRequest(ModelState);                                            // 400
```

---

## 5. Arhitectura Serviciilor & DI

### Lifetimes

| Serviciu | Lifetime | Motiv |
|---------|---------|-------|
| `AppDbContext` | Scoped | EF Core standard |
| `IRepository<T>` | Scoped | Urmează DbContext |
| `IOhlcvRepository` | Scoped | Urmează DbContext |
| `IOhlcvService` | Scoped | Depinde de Repository |
| `IWatchlistService` | Scoped | Depinde de DbContext |
| `IAlertService` | Scoped | Depinde de DbContext |
| `IMarketDataSource` | Scoped | HTTP calls per request |
| `MarketDataPollingWorker` | Singleton | IHostedService |

### Pattern critic — Worker Singleton cu DbContext Scoped

```csharp
// MarketDataPollingWorker — NICIODATĂ inject direct AppDbContext
public class MarketDataPollingWorker(
    IServiceScopeFactory scopeFactory,  // ✅
    ILogger<MarketDataPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOhlcvService>();
        // ...
    }
}
```

### Repository Pattern

```
IRepository<T>          ← Core/Interfaces/ (generic: CRUD + FindAsync + AnyAsync)
IOhlcvRepository        ← Core/Interfaces/ (specializat: GetBySymbolAsync etc.)
Repository<T>           ← Infrastructure/Repositories/ (implementare generică EF)
OhlcvRepository         ← Infrastructure/Repositories/ (moștenește Repository<OhlcvRecord>)
```

> **Regulă:** `OhlcvService` folosește `IOhlcvRepository`. Toate celelalte servicii
> (`AdminService`, `DatabaseSeeder`, `WatchlistService`) folosesc `IDbContextFactory<AppDbContext>`
> direct — **nu le modifica.**

---

## 6. Middleware Pipeline (ordinea contează)

```csharp
// Program.cs — ordinea exactă
app.UseGlobalExceptionHandler();   // 1. PRIMUL — interceptează toate erorile
app.UseHttpsRedirection();         // 2.
app.UseStaticFiles();              // 3.
app.UseRouting();                  // 4.
app.UseAuthentication();           // 5. înaintea Authorization
app.UseAuthorization();            // 6.
app.MapControllers();              // 7. REST API
app.MapBlazorHub();                // 8. SignalR pentru Blazor
app.MapFallbackToPage("/_Host");   // 9. (sau MapRazorComponents)
```

### ExceptionHandlingMiddleware — comportament

```json
// Răspuns JSON structurat pentru ORICE eroare pe /api/*
{
  "status": 404,
  "error": "Not Found",
  "message": "Symbol with id 99999 not found.",
  "traceId": "00-abc123..."
}
```

---

## 7. REST API — Endpoints

| Controller | Endpoint | Auth | Status |
|-----------|---------|------|--------|
| Symbols | `GET /api/symbols` | Public | 200 |
| Symbols | `POST /api/symbols` | Admin | 201 |
| Symbols | `PUT /api/symbols/{id}` | Admin | 204 |
| Symbols | `DELETE /api/symbols/{id}` | Admin | 204 |
| Prices | `GET /api/prices` | Auth | 200 |
| Prices | `GET /api/prices/latest` | Auth | 200 |
| Prices | `GET /api/prices/{id}` | Auth | 200/404 |
| OHLCV | `GET /api/ohlcv` | Auth | 200 |
| OHLCV | `GET /api/ohlcv/symbols` | Auth | 200 |
| OHLCV | `GET /api/ohlcv/{id}` | Auth | 200/404 |
| Watchlist | `GET /api/watchlist` | Auth (user-scoped) | 200 |
| Watchlist | `POST /api/watchlist` | Auth | 201 |
| Watchlist | `DELETE /api/watchlist/{id}` | Auth | 204 |
| Alerts | `GET /api/alerts` | Auth (user-scoped) | 200 |
| Alerts | `POST /api/alerts` | Auth | 201 |
| Alerts | `PUT /api/alerts/{id}` | Auth | 204 |
| Alerts | `DELETE /api/alerts/{id}` | Auth | 204 |
| Alerts | `GET /api/alerts/admin/all` | Admin | 200 |

**Swagger UI:** `/api-docs/index.html` (RoutePrefix = "api-docs")

---

## 8. SignalR Hub

```csharp
// Hub path: /hubs/market
// Client join group:
await hubConnection.InvokeAsync("JoinGroup", symbol);

// Server push event:
await Clients.Group(symbol).SendAsync("ReceivePriceUpdate", dto);

// IMPORTANT: NavigationManager.NavigateTo după sign-in
// TREBUIE forceLoad: true (altfel Blazor auth state nu se resetează)
NavigationManager.NavigateTo("/dashboard", forceLoad: true);
```

---

## 9. JS Widget (Module D)

```javascript
// Fișier: wwwroot/js/marketApiWidget.js
// Endpoint consumat: GET /api/prices/latest
// Fallback (unauthenticated): GET /api/symbols

// Inițializare din Blazor:
// await JS.InvokeVoidAsync("MarketWidget.init", "widget-container-id");

// Distrugere la dispose:
// await JS.InvokeVoidAsync("MarketWidget.destroy");

// 3 stări implementate: loading spinner, data table, error + retry
// Auto-refresh: setInterval 30 secunde
// credentials: "include" — necesare pentru cookie auth
```

---

## 10. Form Validation (Module E)

### Modele de validare

```csharp
// src/MarketDashboard.Web/Models/ValidationModels.cs
AddSymbolModel    // Ticker: [Required][StringLength(10)][RegularExpression @"^[A-Za-z0-9]+$"]
                  // CompanyName: [Required][StringLength(100, MinimumLength = 2)]

CreateAlertModel  // Symbol: [Required][StringLength(10)]
                  // ThresholdPrice: [Required][Range(0.01, 1_000_000)]
                  // Direction: [Required][Range(1, 2)]

AddWatchlistModel // Ticker: [Required][StringLength(10)][RegularExpression @"^[A-Za-z0-9]+$"]
```

### Formulare refactorizate

- `SymbolManagement.razor` — EditForm cu `AddSymbolModel`
- `Alerts.razor` — EditForm cu `CreateAlertModel` + `InputNumber` + `InputSelect`
- `Watchlist.razor` — EditForm cu `AddWatchlistModel`
- `Register.razor` — Identity scaffold (deja complet, neatins)

---

## 11. Integrarea C++ (Modul 15 — Opțional)

### Arhitectura integrării

```
C++ Processor ──► PostgreSQL (MarketPrices, Source="CppProcessor") ◄── CppSharedDbDataSource
                                                                          ↓
                                                              HybridMarketDataSource
                                                          (try C++, fallback AlphaVantage)
                                                                          ↓
                                                              MarketDataPollingWorker
```

### Configurare

```json
// appsettings.json
{
  "DataSource": {
    "Provider": "Hybrid",
    "CppStalenessThresholdMinutes": 5
  }
}
// Provider: "AlphaVantage" | "CppProcessor" | "Hybrid"
// Default: "Hybrid" — dacă C++ nu scrie date recente, fallback automat
```

### Fișiere adăugate de M15

```
Infrastructure/DataSources/CppSharedDbDataSource.cs   ← citește MarketPrices WHERE Source="CppProcessor"
Infrastructure/DataSources/HybridMarketDataSource.cs  ← composite: C++ cu fallback AlphaVantage
Web/Components/Pages/Admin/DataSourceStatus.razor     ← read-only panel (Admin only)
```

### Relația cu market-data-processor (C++)

Procesorul C++ scrie în aceeași bază de date PostgreSQL (`marketdashboard`).
Câmpul `Source` din `MarketPrices` diferențiază originea datelor:
- `"AlphaVantage"` — scris de .NET Worker
- `"CppProcessor"` — scris de procesorul C++

**Nu există comunicare directă între procese** — shared DB este singurul canal.
Dacă `market-data-processor` nu rulează, `HybridMarketDataSource` fallback automat.

---

## 12. Debugging Rapid

### Erori comune și fix-uri

| Eroare | Cauză | Fix |
|-------|-------|-----|
| `Cannot consume scoped service from singleton` | Worker injectează DbContext direct | Folosește `IServiceScopeFactory` în worker |
| `No authentication scheme was specified` | Ordinea middleware greșită | `AddIdentity` înainte de `UseAuthentication` |
| `Cannot redirect after headers sent` | `NavigateTo` fără `forceLoad: true` | `NavigateTo("/dashboard", forceLoad: true)` |
| `SignalR Hub nu primește mesaje` | Componenta nu apelează `JoinGroup` | Verifică `OnInitializedAsync` → `InvokeAsync("JoinGroup", symbol)` |
| `EF Core timestamptz mismatch` | `UseTimestampTzDateTimeKind` lipsă | Adaugă în `Program.cs`: `o.UseTimestampTz DateTimeKind()` în Npgsql options |
| `401 body gol pe /api/*` | `[Authorize]` respinge înainte de middleware | Normal — status 401 corect, body gol este comportament Identity standard |

### Health check rapid

```bash
# Build curat?
dotnet build MarketDashboard.sln 2>&1 | tail -3

# App pornește?
curl -s -o /dev/null -w "%{http_code}" http://localhost:5005
# Expected: 200

# API public funcționează?
curl -s http://localhost:5005/api/symbols | python3 -m json.tool | grep ticker
# Expected: 6 simboluri (AAPL, AMZN, GOOGL, IBM, MSFT, TSLA)

# Swagger accesibil?
curl -s -o /dev/null -w "%{http_code}" http://localhost:5005/api-docs/index.html
# Expected: 200

# DB are date seed?
docker exec marketdashboard-postgres psql -U marketdashboard -d marketdashboard \
  -c "SELECT COUNT(*) FROM \"OhlcvRecords\";"
# Expected: >= 180
```

---

## 13. Credențiale & Seed Data

```json
// appsettings.json → SeedData section
// Admin account creat la startup de DatabaseSeeder
// Verifică DatabaseSeeder.cs pentru email/parola exactă

// Demo account (creat manual în browser):
// Email: demo@test.com
// Password: Demo1234!
// Role: User (default)
```

---

## 14. Checklist Pre-Prezentare

- [ ] `dotnet build` → 0 errors
- [ ] `docker compose up -d` → PostgreSQL running
- [ ] `dotnet run` → app pornește, 0 excepții în primele 30s
- [ ] `/api/symbols` → 200 + 6 simboluri JSON
- [ ] `/api-docs/index.html` → Swagger UI vizibil
- [ ] `/dashboard` → prețuri live, widget JS vizibil
- [ ] `/admin/symbols` → submit gol → mesaje de validare per câmp
- [ ] `/alerts` → submit gol → mesaje de validare per câmp
- [ ] DevTools → Network → WS tab → conexiune SignalR activă
- [ ] `OhlcvRecords` → >= 180 rows în PostgreSQL

---

*MarketDashboard CLAUDE.md — generat Mai 2026*  
*Companion: vezi `market-data-processor/CLAUDE.md` pentru contextul C++*
