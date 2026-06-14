# Library Management System

A production-grade library management system built with **.NET 9**, demonstrating Clean Architecture, JWT authentication with refresh token rotation, and a dual data access strategy (Entity Framework Core + ADO.NET).

This is a portfolio project showcasing senior-level decisions across all layers: domain modeling, persistence, application services, REST API, security, and SPA frontend.

---

## Stack

**Backend**
- .NET 9 / C# 13
- ASP.NET Core (Web API + Identity Core)
- Entity Framework Core 9 + ADO.NET (both implementations of the same repositories)
- SQL Server (containerized via Docker)
- FluentValidation, Serilog, OpenAPI + Scalar

**Authentication**
- JWT (HMAC-SHA256) with short-lived access tokens (15 min)
- Refresh token rotation with SHA-256 hashing in DB
- ASP.NET Core Identity for user/password management
- Role-based authorization with named policies

**Frontend**
- Blazor WebAssembly
- HttpClient with `DelegatingHandler` for automatic Bearer injection and transparent token refresh
- localStorage-based token persistence

**Testing**
- xUnit, Moq, FluentAssertions
- 49 unit tests across Domain and Application layers

**Database / DevOps**
- SQL Server 2022 in Docker
- Flyway for versioned schema migrations
- Central Package Management (`Directory.Packages.props`)
- Conventional Commits

---

## Architecture

The solution follows **Clean Architecture** with strict dependency direction (outer layers depend on inner, never the reverse):

┌──────────────────────────────────────────────────────────────────┐

│                         WebApi (REST)                            │

│              Blazor Client (SPA)                                 │

└──────────────────────────────────────────────────────────────────┘

│ depends on

▼

┌──────────────────────────────────────────────────────────────────┐

│                         Infrastructure                           │

│           EF Core + ADO.NET + JWT generation                     │

└──────────────────────────────────────────────────────────────────┘

│ depends on

▼

┌──────────────────────────────────────────────────────────────────┐

│                         Application                              │

│           Services + DTOs + Validators + Mappers                 │

└──────────────────────────────────────────────────────────────────┘

│ depends on

▼

┌──────────────────────────────────────────────────────────────────┐

│                            Domain                                │

│      Entities + Value Objects + Domain exceptions                │

└──────────────────────────────────────────────────────────────────┘

### Layers

| Layer | Responsibility | Key types |
|---|---|---|
| **Domain** | Business invariants, no framework dependencies | `Book`, `Category`, `Loan`, `BaseEntity<TId>`, domain exceptions |
| **Application** | Use cases, orchestration, validation | `CategoryService`, `BookService`, `AuthService`, DTOs, validators |
| **Infrastructure** | Persistence, JWT, external integrations | `EfCategoryRepository`, `AdoCategoryRepository`, `JwtTokenGenerator` |
| **WebApi** | HTTP layer, ProblemDetails, OpenAPI | Controllers, `GlobalExceptionHandler` |
| **BlazorClient** | SPA frontend | Pages, `AuthMessageHandler`, `AuthState` |

---

## Notable design decisions

### Dual data access (EF Core + ADO.NET)

The same repository interfaces (`ICategoryRepository`, `IBookRepository`) have two implementations:
- `EfCategoryRepository` — EF Core with LINQ, change tracking, soft delete via global query filters.
- `AdoCategoryRepository` — Raw ADO.NET with `SqlConnection`/`SqlCommand`, parameterized SQL, manual mapping.

The active provider is selected at startup via `DataAccess:Provider` in `appsettings.json` (`"EntityFramework"` or `"AdoNet"`). This demonstrates working at both abstraction levels and understanding the trade-offs:

| EF Core | ADO.NET |
|---|---|
| Faster development, change tracking, LINQ | Maximum performance, no overhead, explicit SQL |
| Migration tooling, conventions | Full control of every byte over the wire |
| Slight performance cost | Manual mapping required |

### Soft delete with filtered unique indexes

All entities inherit `BaseEntity<TId>` with an `IsDeleted` flag. The `LibraryDbContext` applies a **global query filter** dynamically via Expression Trees so every LINQ query implicitly excludes soft-deleted rows.

To prevent unique constraint violations after restoration, indexes are filtered:
```sql
CREATE UNIQUE INDEX UX_Categories_Name ON Categories(Name) WHERE IsDeleted = 0;
```

### JWT auth with refresh rotation

- **Access tokens**: 15 minutes, stateless, validated by signature against the server-side `SigningKey` (Base64 in user secrets).
- **Refresh tokens**: 7 days, persisted in DB as **SHA-256 hash** (the raw token is shown to the client only once). Rotated on every `/auth/refresh` — the previous token is revoked, so reuse is detectable.
- **Three roles** (Administrator, Librarian, Reader) with **named authorization policies** (`AdminOnly`, `LibrarianOrAdmin`, `ReaderOrAbove`) — applied to controllers via `[Authorize(Policy = ...)]`.

### Pragmatic auth-in-Application

`AuthService` lives in the Application layer and depends on `UserManager<IdentityUser>` directly, a deliberate trade-off: Identity is the de-facto standard in .NET and the cost of abstracting it (a full `IUserAccountService` interface) outweighs the benefit for this project. The rest of Application (CategoryService, BookService) has zero dependency on Identity.

### Defense-in-depth validation

Every Create/Update operation passes through three layers of validation:
1. **FluentValidation** on the DTO (format, lengths, ranges).
2. **Application service** for cross-entity rules (e.g. category exists when creating a book).
3. **Domain entity** invariants enforced in static factory methods and state-change methods.

### Transparent token refresh in the frontend

The Blazor client uses a `DelegatingHandler` (`AuthMessageHandler`) that:
1. Injects `Authorization: Bearer <access_token>` on every outbound request.
2. On 401, calls `/api/auth/refresh` automatically, stores the new tokens, and **retries the original request** transparently.

The user never sees a 401 unless the refresh itself fails (i.e. the user is truly logged out).

---

## Running locally

### Prerequisites

- .NET 9 SDK
- Docker (for SQL Server and Flyway migrations)
- A code editor (Visual Studio 2026, Rider, or VS Code)

### Steps

#### 1. Start SQL Server

```powershell
docker compose -f database/docker-compose.yml up -d sqlserver
```

#### 2. Run Flyway migrations

```powershell
docker compose -f database/docker-compose.yml run --rm flyway migrate
```

#### 3. Configure user secrets

The `Jwt:SigningKey` and the connection string are sensitive, so they live in user secrets, not in source. Set them:

```powershell
cd src/LibraryManagement.WebApi

dotnet user-secrets set "Jwt:SigningKey" "$(([Convert]::ToBase64String((1..64 | ForEach-Object { [byte](Get-Random -Maximum 256) }))))"
dotnet user-secrets set "ConnectionStrings:LibraryDb" "Server=localhost,1433;Database=LibraryDb;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=True;"
```

#### 4. Run the WebApi

```powershell
dotnet run --project src/LibraryManagement.WebApi --launch-profile https
```

The API will be available at `https://localhost:7281`. The Scalar API reference is at `https://localhost:7281/scalar/v1` (Development only).

#### 5. Run the Blazor client (in another terminal)

```powershell
dotnet run --project src/LibraryManagement.BlazorClient --launch-profile https
```

Open `https://localhost:7167` in your browser.

#### 6. Try it

1. Register a new user → you are logged in automatically (with role `Reader`).
2. Try to create a category → you will receive **403 Forbidden** (role check enforced).
3. Promote yourself to `Administrator` (see SQL snippet below).
4. Logout, login again → your new token has the `Administrator` role.
5. Create categories and view books.

**Promote a user to Administrator**:
```sql
DECLARE @userId NVARCHAR(450) = (SELECT Id FROM dbo.AspNetUsers WHERE Email = 'your@email.com');
DECLARE @readerRoleId NVARCHAR(450) = (SELECT Id FROM dbo.AspNetRoles WHERE Name = 'Reader');
DECLARE @adminRoleId NVARCHAR(450) = (SELECT Id FROM dbo.AspNetRoles WHERE Name = 'Administrator');

DELETE FROM dbo.AspNetUserRoles WHERE UserId = @userId AND RoleId = @readerRoleId;
INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES (@userId, @adminRoleId);
```

### Switch between EF Core and ADO.NET

Edit `src/LibraryManagement.WebApi/appsettings.json`:
```json
"DataAccess": {
  "Provider": "EntityFramework"   // or "AdoNet"
}
```

Restart the API. The same endpoints will now use the other data access stack.

---

## Repository structure

LibraryManagement/

├── database/

│   ├── docker-compose.yml          # SQL Server + Flyway

│   └── migrations/                 # V001 schema, V002 seed data

├── docs/

│   ├── ARCHITECTURE.md             # detailed architecture notes

│   └── adr/                        # Architecture Decision Records

├── src/

│   ├── LibraryManagement.Domain/

│   ├── LibraryManagement.Application/

│   ├── LibraryManagement.Infrastructure/

│   ├── LibraryManagement.WebApi/

│   └── LibraryManagement.BlazorClient/

└── tests/

├── LibraryManagement.Domain.UnitTests/        # 44 tests

├── LibraryManagement.Application.UnitTests/   # 5 tests

└── LibraryManagement.Infrastructure.UnitTests/

---

## Patterns implemented

- **Repository pattern** with `IRepository<T, TId>` base and entity-specific extensions.
- **Unit of Work** (`IUnitOfWork.SaveChangesAsync`).
- **Static factory method** (effective Java, Bloch) for entity construction with invariant enforcement.
- **Aggregate root** boundaries (Book + BookCopies, Loan + Member).
- **DTO mapping** with internal static mapper classes — no AutoMapper to keep mappings explicit and discoverable.
- **Options pattern** for typed configuration (`JwtOptions`).
- **Service layer** orchestrating validation, domain operations, and persistence.
- **Global exception handler** (`IExceptionHandler`) translating domain/application exceptions to ProblemDetails (RFC 7807).
- **DelegatingHandler** in the SPA for transparent auth.
- **Observer pattern** (`AuthState.OnChange`) for client-side reactive UI.

---

## Future work

This project intentionally has scope limits to stay focused on architecture demonstration:

- `Author`, `Member`, `Loan`, `BookCopy` have full domain models and repositories but no Application services or Controllers yet — the pattern is demonstrated with Category and Book.
- The Blazor frontend has minimal styling — focus is on the auth flow and architecture, not visual polish.
- Edit/delete UIs are not implemented for the same reason.

These are intentional trade-offs documented as part of the project scope, not unfinished work.

---

## License

MIT