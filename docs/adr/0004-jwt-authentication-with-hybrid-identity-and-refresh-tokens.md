# ADR-0004: JWT Authentication with Hybrid Identity / Member Model and Refresh Tokens

- **Status:** Accepted
- **Date:** 2026-05-29
- **Deciders:** Project owner
- **Related:** ADR-0001 (Clean Architecture), ADR-0002 (Database First)

## Context

The Library Management System exposes a Web API consumed by a Blazor WebAssembly client. The system must support authentication and authorization for at least three roles (Reader, Librarian, Administrator), and the API must remain stateless to allow horizontal scaling and consumption by future clients (mobile, integrations).

Several decisions were intertwined:

- **Transport of identity.** Cookies, JWTs in the `Authorization` header, or third-party OAuth/OIDC.
- **Storage of user accounts.** ASP.NET Core Identity, a custom user store, or a delegated identity provider.
- **Modeling the domain actor.** Whether the system user (authentication concern) and the library member (domain concern) are the same entity or two related entities.
- **Token lifecycle.** Single short-lived token versus access + refresh, and whether refresh tokens rotate.

The Blazor WebAssembly client runs in a browser and may live on a different origin than the API, making cookie-based authentication harder to configure (cross-origin cookies, CSRF concerns, `SameSite` policies). A bearer-token approach removes the cookie complexity at the cost of new responsibilities: token issuance, storage, expiration, and revocation.

## Decision

The system uses **JWT bearer authentication issued by the API itself**, with the following structure:

1. **ASP.NET Core Identity** provides the user store: `AspNetUsers`, `AspNetRoles`, password hashing, lockout, and related concerns. Identity tables are created manually in SQL as part of the Flyway-managed schema (see ADR-0002).
2. **A separate `Member` entity** models the library domain actor (membership date, active status, loan history). `Member` is linked to `AspNetUsers` by a nullable `UserId` foreign key, supporting a one-to-one relationship where appropriate. Not every Identity user is a Member (an administrator may have no membership), and the domain remains free to evolve without affecting authentication.
3. **Authorization uses policies** built on top of roles. Simple endpoints carry `[Authorize(Roles = "Librarian")]`; complex rules are expressed as named policies registered at startup (`AuthorizationOptions.AddPolicy`).
4. **Tokens come in two kinds:**
   - **Access token:** a JWT signed with HS256, lifetime ~15 minutes, carrying user id and roles as claims. Sent on every request in `Authorization: Bearer ...`.
   - **Refresh token:** an opaque random string (not a JWT), lifetime ~7 days, persisted in a `RefreshTokens` table with columns for hash, user id, expiration, and revocation timestamp. Used only against `POST /auth/refresh` to obtain a new access token.
5. **Logout invalidates the refresh token** (`UPDATE` setting `RevokedAt`). The access token remains valid until natural expiration, accepted as a known trade-off in exchange for stateless API requests.

Token storage on the client follows pragmatic defaults: the access token lives in Blazor service memory (lost on tab close, invisible to other origins); the refresh token lives in `localStorage`. The security implications are documented and a future iteration may move to a BFF (Backend For Frontend) pattern with HttpOnly cookies.

## Alternatives Considered

### Cookie-based authentication with Identity

Idiomatic for server-rendered ASP.NET applications. Rejected because:

- The Blazor WebAssembly client runs on a potentially different origin, making cross-origin cookies awkward.
- Cookie-based authentication does not extend cleanly to non-browser clients (mobile, server-to-server).
- The educational goal includes implementing the full JWT lifecycle, which cookies do not exercise.

### OAuth 2.0 / OpenID Connect with an external identity provider

Production enterprise pattern (Azure AD, Auth0, Keycloak). Rejected for the MVP because:

- Introduces external infrastructure and configuration dependencies that distract from the core learning goals.
- Most of the educational value of implementing tokens, hashing, and refresh logic is lost when an external provider handles it.
- Can be added later by replacing the JWT issuance endpoints; the rest of the system continues to validate JWTs identically.

### A single user table merging authentication and domain data

A pragmatic shortcut that puts membership dates and loan-related state directly on `AspNetUsers`. Rejected because:

- Violates Single Responsibility: authentication concerns and library domain concerns evolve independently.
- Makes the domain layer dependent on Identity types, polluting Clean Architecture boundaries.
- Loses the option to have non-member users (administrators, staff) or to support multiple membership types.

### Custom authentication from scratch (no Identity)

Build the user table, hashing, and lockout logic manually. Rejected because:

- Reinventing password hashing, salting, and account lockout is a recipe for subtle security flaws.
- Identity is well-tested, configurable, and reviewable. Using it is the responsible default.

### Single access token without refresh

Simplest token model. Rejected because:

- Forces the user to log in again every 15-30 minutes, which is unacceptable UX.
- Long-lived access tokens (multiple hours) widen the blast radius of token theft.
- The access + refresh pattern is industry standard and worth implementing for portfolio value.

### Refresh token rotation on every use

Stronger security: each refresh use invalidates the prior refresh token. Considered but deferred because:

- Adds complexity (handling near-simultaneous refresh attempts, race conditions, reuse detection).
- The base access + refresh design is already a significant security improvement over a single token. Rotation can be added later without changing contracts.

## Consequences

### Positive

- **Stateless API.** No server-side session state for ordinary requests. Horizontal scaling and multi-instance deployment require no sticky sessions.
- **Client variety supported.** The same authentication contract serves the Blazor client today and could serve mobile, CLI, or partner integrations tomorrow.
- **Clean separation of concerns.** Authentication (`AspNetUsers`) and the domain (`Member`) evolve independently, each in its own bounded context.
- **Realistic auth lifecycle is exercised.** Login, token issuance, validation middleware, refresh, revocation, and logout are all implemented, providing complete interview-ready material.
- **Policy-based authorization scales.** Starting with role-only authorization keeps the MVP simple while leaving the door open for complex rules without refactoring controllers.

### Negative

- **Token storage is imperfect.** Access tokens in memory survive reloads poorly; refresh tokens in `localStorage` are exposed to XSS. The honest mitigation is good XSS hygiene (CSP, sanitization, no `eval`) and the option to migrate to a BFF pattern.
- **Logout is not instantaneous.** Access tokens remain valid until natural expiration even after logout. Acceptable given the short lifetime; documented as a known trade-off.
- **Identity schema must be authored manually in SQL** (see ADR-0002). The Identity team ships these as EF migrations; here they are SQL files. Identity column types and names must match Identity's expectations exactly.
- **Two tables for what looks like one concept.** `AspNetUsers` and `Member` will sometimes be confused by newcomers; a short architecture note clarifies the distinction.

### Followups / Open Items

- An auth endpoints reference will document `/auth/register`, `/auth/login`, `/auth/refresh`, `/auth/logout`, including request/response shapes and error cases.
- Refresh token rotation will be considered for a security hardening iteration.
- A BFF variant of the Blazor host is a candidate for a future iteration if the project graduates from portfolio to product.
- JWT signing keys must be loaded from secure configuration (User Secrets in development, environment variables or a key vault in production). Hardcoded keys in source are forbidden and verified in code review.
