# Library Management System

> A production-grade library management system built with .NET 9, demonstrating Clean Architecture, dual data access (EF Core + ADO.NET), JWT authentication, and a Blazor WebAssembly frontend.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## What this project is

A portfolio-grade backend + frontend project intended to demonstrate modern .NET 9 engineering practices in a single, coherent codebase. Business logic is intentionally simple (book lending, member management, loans). The interest is in **how** it is built, not what it does.

For the full reasoning behind every architectural decision, see [`ARCHITECTURE.md`](ARCHITECTURE.md) and the [ADRs](docs/adr/).

## Tech stack

| Layer            | Technology                                    |
|------------------|-----------------------------------------------|
| Platform         | .NET 9 (C# 13)                                |
| Frontend         | Blazor WebAssembly                            |
| Backend          | ASP.NET Core Web API                          |
| Database         | SQL Server 2022 (Docker)                      |
| Schema migration | Flyway (planned)                              |
| Data access      | Entity Framework Core 9 + ADO.NET (dual)      |
| Authentication   | JWT bearer (access + refresh) + Identity      |
| Validation       | FluentValidation                              |
| Logging          | Serilog (Console + rolling File)              |
| API docs         | `Microsoft.AspNetCore.OpenApi` + Scalar UI    |
| Testing          | xUnit + Moq + FluentAssertions 7              |

## Solution structure

LibraryManagement/
├── src/
│   ├── LibraryManagement.Domain/            # Pure entities, interfaces, invariants
│   ├── LibraryManagement.Application/       # Use case services, DTOs, validators
│   ├── LibraryManagement.Infrastructure/    # EF Core, ADO.NET, JWT, Serilog
│   ├── LibraryManagement.WebApi/            # Controllers, middleware, composition root
│   └── LibraryManagement.BlazorClient/      # Blazor WebAssembly frontend
├── tests/
│   ├── LibraryManagement.Domain.UnitTests/
│   ├── LibraryManagement.Application.UnitTests/
│   └── LibraryManagement.Infrastructure.UnitTests/
├── database/
│   ├── docker-compose.yml                   # SQL Server + Flyway
│   └── migrations/                          # Versioned SQL scripts
└── docs/
├── ARCHITECTURE.md
└── adr/                                 # Architecture Decision Records

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- A SQL client of choice (`sqlcmd`, Azure Data Studio, or SSMS) — optional

## Getting started

### 1. Clone the repository

```bash
git clone <your-repo-url>
cd LibraryManagement
```

### 2. Start SQL Server

```bash
cd database
docker compose up -d
```

Wait ~30 seconds for the container to become healthy:

```bash
docker compose ps
```

Status should read `(healthy)`.

### 3. Configure local secrets

Set the JWT signing key and the connection string in User Secrets (they never reach Git):

```bash
cd ../src/LibraryManagement.WebApi

dotnet user-secrets set "Jwt:SigningKey" "<a-64-byte-base64-key-of-your-choice>"
dotnet user-secrets set "ConnectionStrings:LibraryDb" "Server=localhost,1433;Database=LibraryDb;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=True;"
```

To generate a strong signing key:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

### 4. Create the database

```bash
sqlcmd -S "localhost,1433" -U sa -P "Strong!Passw0rd" -C -Q "CREATE DATABASE LibraryDb;"
```

### 5. Build and run

```bash
cd ../..
dotnet build
dotnet run --project src/LibraryManagement.WebApi
```

The API will be available at `https://localhost:<port>` (Kestrel chooses the port — check the console output). Scalar UI is at `/scalar/v1`.

## Useful commands

| Command                                            | Purpose                          |
|----------------------------------------------------|----------------------------------|
| `dotnet build`                                     | Compile the entire solution      |
| `dotnet test`                                      | Run all unit tests               |
| `docker compose -f database/docker-compose.yml ps` | Check SQL Server status          |
| `docker compose -f database/docker-compose.yml logs sqlserver` | Tail SQL Server logs |
| `docker compose -f database/docker-compose.yml down`  | Stop SQL Server (data persists) |
| `docker compose -f database/docker-compose.yml down -v` | Stop and wipe the database     |
| `dotnet user-secrets list --project src/LibraryManagement.WebApi` | Inspect local secrets |

## Architecture decisions

This project is documented with formal Architecture Decision Records:

- [ADR-0001: Clean Architecture with Pure Domain Entities](docs/adr/0001-clean-architecture-with-pure-domain-entities.md)
- [ADR-0002: Database First Schema with Flyway Migrations](docs/adr/0002-database-first-schema-with-flyway.md)
- [ADR-0003: Dual Data Access — EF Core and ADO.NET](docs/adr/0003-dual-data-access-ef-core-and-ado-net.md)
- [ADR-0004: JWT Authentication with Hybrid Identity / Member Model](docs/adr/0004-jwt-authentication-with-hybrid-identity-and-refresh-tokens.md)

For the architectural overview, read [`ARCHITECTURE.md`](ARCHITECTURE.md).

## Status

This project is a learning/portfolio exercise built incrementally. See the [out-of-scope section in ARCHITECTURE.md](ARCHITECTURE.md#out-of-scope-for-the-mvp) for what is deliberately not implemented in the MVP.

## License

MIT — see [LICENSE](LICENSE) if present.