# ADR-0002: Database First Schema with Flyway Migrations

- **Status:** Accepted
- **Date:** 2026-05-29
- **Deciders:** Project owner
- **Related:** ADR-0001 (Clean Architecture), ADR-0003 (Dual Data Access)

## Context

The Library Management System uses SQL Server as its primary data store. Two complementary decisions had to be made:

1. **Who owns the schema?** Should C# classes (Code First) be the source of truth, or should SQL scripts (Database First) be authoritative?
2. **How is schema change versioned?** Whichever direction is chosen, the schema must evolve safely across environments (development, staging, production).

The default in modern .NET projects is **Code First with EF Core Migrations**: entities are declared in C#, migrations are generated automatically, and the database is updated by running `dotnet ef database update`. This is appropriate when EF Core is the sole data access technology and the team works exclusively in C#.

This project, however, has two characteristics that complicate the default choice:

- **Dual data access** (see ADR-0003). EF Core and ADO.NET coexist. ADO.NET expects an explicit schema to write SQL against; an EF-generated schema is implicit and partially under EF Core control (shadow columns, change tracking conventions). A schema explicitly designed and maintained is friendlier to raw-SQL consumers.
- **Learning intent.** A core goal of the project is to demonstrate understanding of relational design and SQL Server features (indexes, constraints, normalization choices, lookup tables) without leaning on EF abstractions.

## Decision

The project follows **Database First**. The SQL Server schema is the source of truth and is authored as SQL scripts maintained by hand in the repository.

Schema versioning uses **Flyway**, executed as a CLI tool (or Docker container) outside the .NET solution.

The workflow is:

1. Schema changes are authored as versioned SQL scripts under `database/migrations/` (e.g. `V001__create_initial_schema.sql`, `V002__add_isbn_index.sql`).
2. Flyway applies pending migrations in order, tracking applied versions in a `flyway_schema_history` table.
3. EF Core entities are regenerated from the database with `dotnet ef dbcontext scaffold` into `Infrastructure/Persistence/DataModels`. These are partial classes; manual customizations live in partial files that survive regeneration.
4. ADO.NET repositories write SQL directly against the schema, with table and column names documented in a schema reference.

## Alternatives Considered

### Code First with EF Core Migrations

The modern default. Rejected because:

- Schema becomes implicit and partially controlled by EF Core conventions, making raw SQL harder to reason about.
- ADO.NET consumers would have to follow EF Core's naming and structural decisions instead of explicit design choices.
- The project explicitly seeks to teach SQL Server fundamentals; auto-generated schema obscures them.

### Database First with FluentMigrator or DbUp

Both are .NET-native migration libraries. Rejected in favor of Flyway because:

- Flyway is language-agnostic and is the de-facto standard in enterprise environments running multiple stacks. Knowing Flyway transfers to non-.NET roles.
- Flyway runs independently of the .NET process, which fits projects where database deployment is owned by a separate pipeline or team.
- For a portfolio project, Flyway signals broader exposure than a .NET-only tool.

### Manual SQL scripts without a migration tool

Just running `.sql` files by hand or with custom scripts. Rejected because:

- Idempotency must be re-implemented in every script.
- No reliable tracking of applied versions across environments.
- This anti-pattern is a frequent cause of production incidents in legacy systems and is not defensible in an interview.

## Consequences

### Positive

- **Schema is explicit, reviewable, and ownable.** A SQL file is the source of truth for `CREATE TABLE`, indexes, constraints, and seed data.
- **ADO.NET is first-class.** Raw SQL is written against a known, stable schema, not against EF-generated artifacts.
- **Industry-standard versioning.** Flyway is widely known, used by large organizations, and decoupled from the application runtime.
- **Forced clarity on schema decisions.** Indexes, FK actions, check constraints, and unique constraints are explicit, not implicit by-products of attributes or fluent configuration.

### Negative

- **More tooling.** Developers need Flyway CLI (or Docker) in addition to the .NET SDK. Setup documentation must cover this.
- **Manual scaffolding step.** When the schema changes, `dotnet ef dbcontext scaffold` must be re-run. Customizations to the `DbContext` must live in partial classes to survive regeneration.
- **No automatic down migrations.** Flyway prefers forward-only migration; rolling back a schema change requires an explicit reverse migration. This is the dominant industry practice but requires discipline.
- **ASP.NET Core Identity tables must be created manually.** Identity ships its schema via EF Core conventions; in Database First mode, the same tables must be authored in SQL by hand, matching Identity's expected structure. This is a one-time setup cost.

### Followups / Open Items

- A `database/README.md` will document the Flyway workflow, naming conventions for migrations, and the Identity schema requirements.
- Seed data for lookup tables (`CopyStatus`, `LoanStatus`) lives in dedicated migration files (`V010__seed_copy_status.sql`) for clarity.
- Production deployments must run Flyway against the target database before deploying the API, never the reverse.
