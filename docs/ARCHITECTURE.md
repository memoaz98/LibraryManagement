# Library Management System — Architecture Overview

A production-grade library management system built to demonstrate modern .NET 9 engineering: Clean Architecture, dual data access (EF Core + ADO.NET), JWT authentication, Database First with Flyway migrations, and a Blazor WebAssembly client.

## Table of Contents

- [Goals](#goals)
- [Technology Stack](#technology-stack)
- [Solution Structure](#solution-structure)
- [Dependency Rule](#dependency-rule)
- [Cross-Cutting Decisions](#cross-cutting-decisions)
- [Request Flow](#request-flow)
- [Domain Model](#domain-model)
- [Authentication Flow](#authentication-flow)
- [Architecture Decision Records](#architecture-decision-records)
- [Out of Scope for the MVP](#out-of-scope-for-the-mvp)

## Goals

This project is a portfolio-grade implementation built to demonstrate, with code, the following engineering competencies:

- Disciplined separation of concerns following **Clean Architecture**.
- Database design and schema versioning using **Database First with Flyway**.
- Concrete trade-off analysis between **Entity Framework Core** and **ADO.NET** by implementing the same contracts with both.
- Modern .NET 9 authentication using **JWT access + refresh tokens** with **ASP.NET Core Identity** for the user store and a separate **`Member`** entity for the library domain actor.
- Production patterns: **Repository**, **Unit of Work**, **Service Layer**, **DTOs**, dependency injection, structured logging, standardized error responses, and OpenAPI documentation.

Business logic is intentionally simple. The complexity is in the **architecture**, not the domain.

## Technology Stack

| Layer / Concern        | Technology                                                |
| ---------------------- | --------------------------------------------------------- |
| Platform               | .NET 9 (C# 13)                                            |
| Frontend               | Blazor WebAssembly                                        |
| Backend                | ASP.NET Core Web API                                      |
| Database               | SQL Server                                                |
| Schema versioning      | Flyway (CLI or Docker)                                    |
| Data access (primary)  | Entity Framework Core 9                                   |
| Data access (parallel) | ADO.NET (`SqlConnection`, `SqlCommand`)                   |
| Authentication         | JWT bearer tokens (access + refresh)                      |
| User store             | ASP.NET Core Identity                                     |
| Authorization          | Policy-based, with role claims                            |
| Validation             | FluentValidation                                          |
| Logging                | Serilog (Console + rolling file sinks)                    |
| API documentation      | `Microsoft.AspNetCore.OpenApi` + Scalar UI                |
| Error handling         | `IExceptionHandler` + RFC 7807 `ProblemDetails`           |
| Object mapping         | Manual (static extension methods, no reflection)          |
| Testing                | xUnit + Moq + FluentAssertions 7 (unit tests in MVP)      |

## Solution Structure

```
LibraryManagement.sln
│
├── src/
│   ├── LibraryManagement.Domain/
│   │   ├── Entities/                  ← Pure POCOs with behavior
│   │   ├── ValueObjects/
│   │   ├── Interfaces/                ← Repository and UoW contracts
│   │   ├── Exceptions/                ← DomainException, NotFoundException
│   │   └── Common/                    ← BaseEntity (Id, IsDeleted, CreatedAt)
│   │
│   ├── LibraryManagement.Application/
│   │   ├── Services/                  ← Use case orchestration
│   │   ├── DTOs/                      ← API contracts (in/out)
│   │   ├── Validators/                ← FluentValidation rules
│   │   ├── Mappers/                   ← Domain ↔ DTO mapping
│   │   └── Options/                   ← Typed configuration sections
│   │
│   ├── LibraryManagement.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── EntityFramework/       ← LibraryDbContext, EF repos, EfUnitOfWork
│   │   │   ├── AdoNet/                ← ADO repos, AdoUnitOfWork
│   │   │   ├── DataModels/            ← Scaffolded EF entities
│   │   │   └── Mappers/               ← Domain ↔ DataModel mapping
│   │   ├── Identity/                  ← Identity configuration, password options
│   │   ├── Authentication/            ← JWT issuance, refresh token service
│   │   └── Logging/                   ← Serilog setup
│   │
│   ├── LibraryManagement.WebApi/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Policies/                  ← Authorization policies
│   │   ├── ExceptionHandlers/         ← Global IExceptionHandler
│   │   ├── appsettings.json
│   │   └── Program.cs
│   │
│   └── LibraryManagement.BlazorClient/
│       ├── Pages/
│       ├── Services/                  ← HttpClient wrappers, token storage
│       ├── Auth/                      ← AuthenticationStateProvider
│       └── Program.cs
│
├── tests/
│   ├── LibraryManagement.Domain.UnitTests/
│   ├── LibraryManagement.Application.UnitTests/
│   └── LibraryManagement.Infrastructure.UnitTests/
│
├── database/
│   ├── migrations/                    ← Flyway-versioned SQL scripts
│   │   ├── V001__create_initial_schema.sql
│   │   ├── V002__create_identity_tables.sql
│   │   └── ...
│   └── README.md                      ← Flyway workflow
│
└── docs/
    ├── ARCHITECTURE.md                ← This document
    └── adr/                           ← Architecture Decision Records
        ├── 0001-clean-architecture-with-pure-domain-entities.md
        ├── 0002-database-first-schema-with-flyway.md
        ├── 0003-dual-data-access-ef-core-and-ado-net.md
        └── 0004-jwt-authentication-with-hybrid-identity-and-refresh-tokens.md
```

## Dependency Rule

Dependencies always point inward toward the domain. No outer layer is referenced by an inner layer.

```
            ┌──────────────────────────────────────┐
            │           WebApi / BlazorClient      │  (presentation, composition root)
            └────────────┬─────────────────────────┘
                         │ depends on
            ┌────────────▼─────────────────────────┐
            │            Infrastructure            │  (EF Core, ADO.NET, JWT, Serilog)
            └────────────┬─────────────────────────┘
                         │ depends on
            ┌────────────▼─────────────────────────┐
            │             Application              │  (services, DTOs, validators)
            └────────────┬─────────────────────────┘
                         │ depends on
            ┌────────────▼─────────────────────────┐
            │               Domain                 │  (entities, interfaces — depends on nothing)
            └──────────────────────────────────────┘
```

The Domain project references no other project. Application references only Domain. Infrastructure references Domain and Application. WebApi references everything it composes. This rule is what makes the dual data access pattern (ADR-0003) possible and what keeps the domain testable in isolation.

## Cross-Cutting Decisions

The following decisions apply across the entire solution and are not the subject of dedicated ADRs:

### Soft delete everywhere

All business entities inherit from `BaseEntity`, which provides `Id`, `IsDeleted`, `CreatedAt`. The EF `DbContext` applies a **global query filter** that excludes deleted rows from every query, and `SaveChangesAsync` is overridden so that calling `Remove()` performs an `UPDATE` setting `IsDeleted = true` rather than an actual `DELETE`. Lookup tables (`CopyStatus`, `LoanStatus`) are exempt because they are reference data.

### Options Pattern for configuration

Tunable values (loan duration, JWT lifetimes, pagination defaults) live in `appsettings.json` and are bound to typed classes via `IOptions<T>` or `IOptionsSnapshot<T>`. No magic numbers in code.

### Structured logging with Serilog

Logs are structured (key-value pairs, not concatenated strings). Each request carries a `TraceId` through W3C Trace Context, and Serilog enrichers add `MachineName`, `EnvironmentName`, and request metadata. In development logs go to Console; in production they roll daily to file and could be shipped to Seq, Elasticsearch, or Application Insights without code changes.

### Standardized errors via `ProblemDetails`

A global `IExceptionHandler` translates domain exceptions and unexpected errors into RFC 7807 `ProblemDetails` responses. Stack traces never leave the server in production. Clients can rely on a consistent error shape regardless of which endpoint failed.

### Manual mapping with strict separation

Three sets of types coexist:

- **Domain entities** (`Book`, `Loan`, ...) carry behavior and invariants.
- **Data models** (`BookDataModel`, ...) are scaffolded from SQL Server for EF Core.
- **DTOs** (`BookListDto`, `CreateLoanDto`, ...) are API contracts.

Mapping between them lives in dedicated static classes (`BookMapper`, `LoanMapper`) inside their respective layers. Mapping is verbose but explicit, debuggable, and free of reflection.

## Request Flow

A typical authenticated request flows through the layers as follows:

1. **Blazor WebAssembly client** issues an HTTP request with `Authorization: Bearer <accessToken>`.
2. **ASP.NET Core middleware** validates the JWT signature, extracts claims, populates `HttpContext.User`.
3. **Authorization policy** checks the user is permitted to invoke this endpoint.
4. **Controller** receives a validated DTO (FluentValidation runs in the pipeline) and calls into an application service.
5. **Application service** orchestrates the use case: invokes domain entities for business logic, calls repositories through their interfaces, and confirms changes through `IUnitOfWork`.
6. **Repository implementation** (EF Core or ADO.NET, depending on configuration) translates the operation into database calls.
7. **Database** executes the SQL.
8. **Response** travels back through mappers (domain entity → DTO), is serialized to JSON, and returned with appropriate HTTP status.
9. **Errors** are caught by the global exception handler and rendered as `ProblemDetails`.

## Domain Model

The library domain centers on six entities:

- **`Book`** — the abstract work (title, ISBN, year). Belongs to a `Category` and to one or more `Author`s.
- **`Author`** — a person who wrote books. Many-to-many with `Book`.
- **`Category`** — a genre or classification. One-to-many with `Book`.
- **`BookCopy`** — a physical copy of a `Book`. A book may have many copies. Each copy has a `CopyStatus` (Available / Borrowed / Maintenance) tracked through a lookup table.
- **`Member`** — the library patron. Linked optionally to an `AspNetUsers` row for authentication.
- **`Loan`** — the act of a `Member` borrowing a `BookCopy` for a period of time. Carries a `LoanStatus` (Active / Returned / Overdue) through a lookup table.

The separation between `Book` (the work) and `BookCopy` (the physical artifact) is deliberate and is enforced in the data model. Loans target copies, not books, because two patrons can borrow the same title simultaneously only when multiple copies exist.

## Authentication Flow

Authentication uses the standard JWT access + refresh dance:

1. **Login** (`POST /auth/login`): credentials are verified through ASP.NET Core Identity. On success the API returns a short-lived access token (15 minutes) and a long-lived opaque refresh token (7 days). The refresh token is persisted server-side in the `RefreshTokens` table.
2. **Authenticated calls**: every request carries the access token in the `Authorization` header. The API validates the signature and extracts claims.
3. **Refresh** (`POST /auth/refresh`): when the access token expires, the client posts the refresh token and receives a new access token. The refresh token's validity is checked against the database.
4. **Logout** (`POST /auth/logout`): the refresh token is revoked in the database; the access token remains valid until it expires naturally (~15 minutes blast radius).

Authorization is enforced through policies built on roles (`Reader`, `Librarian`, `Administrator`). Simple endpoints use `[Authorize(Roles = "...")]`; complex rules are expressed as named policies registered at startup.

Detailed rationale is in [ADR-0004](adr/0004-jwt-authentication-with-hybrid-identity-and-refresh-tokens.md).

## Architecture Decision Records

Significant architectural decisions are documented as ADRs. Each ADR is immutable once accepted; if a decision is reversed, a new ADR is added that references and supersedes the original.

| #         | Title                                                                                                                       | Status   |
| --------- | --------------------------------------------------------------------------------------------------------------------------- | -------- |
| ADR-0001  | [Clean Architecture with Pure Domain Entities](adr/0001-clean-architecture-with-pure-domain-entities.md)                    | Accepted |
| ADR-0002  | [Database First Schema with Flyway Migrations](adr/0002-database-first-schema-with-flyway.md)                               | Accepted |
| ADR-0003  | [Dual Data Access — EF Core and ADO.NET Behind the Same Contracts](adr/0003-dual-data-access-ef-core-and-ado-net.md)        | Accepted |
| ADR-0004  | [JWT Authentication with Hybrid Identity / Member Model and Refresh Tokens](adr/0004-jwt-authentication-with-hybrid-identity-and-refresh-tokens.md) | Accepted |

## Out of Scope for the MVP

The following are intentional non-goals for the initial milestone and are candidates for future iterations:

- **Integration tests** with Testcontainers and a live SQL Server instance. The architecture supports it (the API exposes a `partial class Program` for `WebApplicationFactory<Program>`, connection strings come from configuration), but the work is deferred.
- **End-to-end tests** with Playwright or similar.
- **CQRS with MediatR.** The current service layer is sufficient for the domain's complexity. CQRS can be layered on without changing the dependency structure if the project grows.
- **Result Pattern.** Domain failures currently surface as exceptions translated by the global handler. A `Result<T>` style API is a future refactor with bounded cost.
- **Refresh token rotation.** Implemented today is single-use-per-token-validity; rotation on every refresh is a security hardening for a later iteration.
- **BFF (Backend For Frontend)** with HttpOnly cookies for the Blazor client. Current bearer-token-in-browser is pragmatic but has known XSS exposure.
- **External identity providers** (Azure AD, Auth0, Google, etc.). The JWT issuance can be replaced by external providers without touching the rest of the API.
- **Caching layers** (in-memory, Redis). Not needed at MVP scale.
- **Email notifications, fines, reservations.** Domain expansion candidates beyond the core six entities.
