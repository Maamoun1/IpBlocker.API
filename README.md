
# IP Blocker API

A **.NET 8 Web API** that manages blocked countries and validates IP addresses using the [ipapi.co](https://ipapi.co) geolocation service. No database — fully in-memory with thread-safe collections.

---

## 🏗️ Architecture

```
IpBlocker.API/
├── Controllers/          — Thin HTTP layer (routing, status codes)
├── Services/             — Business logic (blocking rules, orchestration)
├── Repositories/         — Thread-safe in-memory storage
├── ExternalServices/     — ipapi.co HttpClient wrapper
├── BackgroundServices/   — Temporal block cleanup (runs every 5 min)
├── Models/
│   ├── Entities/         — In-memory domain objects
│   ├── Requests/         — Validated input DTOs
│   └── Responses/        — Output DTOs
└── Common/               — Settings, helpers
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A free API key from [ipapi.co](https://ipapi.co) (optional — works without one on free tier)

### Run the API

```bash
# Clone and navigate
cd IpBlocker.API

# (Optional) Set your ipapi.co API key using user secrets
dotnet user-secrets set "GeoLocationApi:ApiKey" "your_api_key_here"

# Run
dotnet run
```

Swagger UI opens at: **http://localhost:5000** (or the port shown in terminal)

---

## 📋 Endpoints

| # | Method | Route | Description |
|---|--------|-------|-------------|
| 1 | POST | `/api/countries/block` | Permanently block a country |
| 2 | DELETE | `/api/countries/block/{countryCode}` | Remove a permanent block |
| 3 | GET | `/api/countries/blocked` | List all blocked countries (paginated + search) |
| 4 | GET | `/api/ip/lookup?ipAddress={ip}` | Look up geolocation for an IP |
| 5 | GET | `/api/ip/check-block` | Check if caller's IP is from a blocked country |
| 6 | GET | `/api/logs/blocked-attempts` | Audit log of all check attempts |
| 7 | POST | `/api/countries/temporal-block` | Block a country for a limited duration |

---

## 📖 Endpoint Details

### 1. Block a Country
```
POST /api/countries/block
Content-Type: application/json

{ "countryCode": "EG" }
```
- **201 Created** — country added
- **400 Bad Request** — invalid format
- **409 Conflict** — already blocked

### 2. Unblock a Country
```
DELETE /api/countries/block/EG
```
- **204 No Content** — removed
- **404 Not Found** — not in blocked list

### 3. List Blocked Countries
```
GET /api/countries/blocked?page=1&pageSize=10&search=egy
```
- Paginated with `totalCount`, `totalPages`, `hasNextPage`
- Search filters by code OR name (case-insensitive)

### 4. IP Lookup
```
GET /api/ip/lookup?ipAddress=8.8.8.8
GET /api/ip/lookup            ← uses caller's IP automatically
```
- **200 OK** — returns country, city, ISP, coordinates
- **400** — invalid IP format
- **502** — geolocation service unavailable

### 5. Check Block Status
```
GET /api/ip/check-block
```
- Detects caller IP automatically (handles reverse proxies via `X-Forwarded-For`)
- Always returns **200 OK** — check `isBlocked` in response body
- Logs every attempt to the audit log

### 6. Audit Logs
```
GET /api/logs/blocked-attempts?page=1&pageSize=20
```
- Returns paginated log of all check-block calls
- Each entry: IP, timestamp, country, blocked status, User-Agent

### 7. Temporal Block
```
POST /api/countries/temporal-block
Content-Type: application/json

{ "countryCode": "US", "durationMinutes": 120 }
```
- **201 Created** — temporal block created
- **400** — duration not 1–1440, or invalid code
- **409** — country already temporarily blocked
- Auto-removed by background service after expiry

---

## 🔒 Thread Safety

| Collection | Used For | Why |
|------------|----------|-----|
| `ConcurrentDictionary<string, BlockedCountry>` | Permanent blocks | Atomic `TryAdd`/`TryRemove` |
| `ConcurrentDictionary<string, TemporalBlock>` | Temporal blocks | Same — prevents duplicate race |
| `ConcurrentQueue<BlockAttemptLog>` | Audit logs | Lock-free append-only |

All repositories are registered as **Singletons** — one shared instance across all requests.

---

## ⚙️ Configuration

`appsettings.json`:
```json
{
  "GeoLocationApi": {
    "BaseUrl": "https://ipapi.co/",
    "ApiKey": ""
  }
}
```

> **Never commit real API keys.** Use `dotnet user-secrets` in development or environment variables in production: `GeoLocationApi__ApiKey=your_key`

---

## 🧠 Design Decisions

**Why `ServiceResult<T>` instead of exceptions?**
Expected business outcomes (duplicate, not found) aren't exceptional. `ServiceResult` keeps flow control explicit and avoids expensive exception construction.

**Why separate `TemporalBlock` from `BlockedCountry`?**
Different lifecycles, different cleanup rules. Mixing them into one model creates hidden conditional logic.

**Why `PeriodicTimer` in the background service?**
`PeriodicTimer` ensures consistent cleanup intervals

**Why `IHttpClientFactory` instead of `new HttpClient()`?**
`new HttpClient()` causes socket exhaustion under load. The factory manages connection pooling with proper `HttpMessageHandler` lifetime rotation.

---

## 📦 NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.Extensions.Http` | `IHttpClientFactory` |
| `Swashbuckle.AspNetCore` | Swagger / OpenAPI UI |

---

## 🧪 Testing the API (Quick Start)

```bash
# 1. Block Egypt
curl -X POST http://localhost:5000/api/countries/block \
  -H "Content-Type: application/json" \
  -d '{"countryCode": "EG"}'

# 2. List blocked countries
curl http://localhost:5000/api/countries/blocked

# 3. Look up an IP
curl "http://localhost:5000/api/ip/lookup?ipAddress=8.8.8.8"

# 4. Check if your IP is blocked
curl http://localhost:5000/api/ip/check-block

# 5. Temporarily block the US for 2 hours
curl -X POST http://localhost:5000/api/countries/temporal-block \
  -H "Content-Type: application/json" \
  -d '{"countryCode": "US", "durationMinutes": 120}'

# 6. View audit logs
curl http://localhost:5000/api/logs/blocked-attempts
```
---
## 👤 Author
**Maamoun Ibrahim** Junior Backend Developer | .NET 8 Enthusiast  
[https://github.com/Maamoun1]