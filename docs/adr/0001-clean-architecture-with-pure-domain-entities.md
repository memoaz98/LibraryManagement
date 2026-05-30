# ADR-0001: Clean Architecture with Pure Domain Entities

- **Status:** Accepted
- **Date:** 2026-05-29
- **Deciders:** Project owner
- **Related:** ADR-0003 (Dual Data Access), ADR-0004 (Authentication)

## Context

The Library Management System is being built as a portfolio-grade .NET 9 project intended to demonstrate production-ready engineering. Several forces shaped this decision:

- The project deliberately uses **two data access technologies in parallel** (Entity Framework Core and ADO.NET) behind the same repository contracts. This requires a layered structure where domain logic does not depend on any specific persistence technology.
- The project follows **Database First**, meaning data models are scaffolded from an existing SQL Server schema. Scaffolded classes carry EF Core-specific metadata, navigation properties, and conventions that should not leak into business logic.
- The domain has non-trivial invariants (loan due dates, book copy availability, soft-delete semantics) that benefit from being expressed as behavior on entities rather than as anemic data containers.
- The project must remain testable: domain rules should be verifiable without a database, an HTTP host, or any framework infrastructure.

Two layered approaches were considered:

1. **N-tier** with a single set of entities flowing through all layers (`Web → Business → Data`).
2. **Clean Architecture** with explicit dependency inversion and separate models per concern.

## Decision

The solution is organized following **Clean Architecture**, with the dependency rule pointing strictly inward toward the domain.

```
LibraryManagement.Domain          → Pure entities, value objects, repository interfaces
LibraryManagement.Application     → Use case services, DTOs, validation contracts
LibraryManagement.Infrastructure  → EF Core, ADO.NET, JWT, logging implementations
LibraryManagement.WebApi          → Controllers, middleware, composition root
LibraryManagement.BlazorClient    → Blazor WebAssembly frontend
```

Two distinct sets of entity classes coexist:

- **Domain entities** (e.g. `Book`, `Loan`, `BookCopy`) live in `Domain`. They are POCOs with no framework attributes, expose behavior through factory methods and domain operations (`Loan.Return()`, `BookCopy.MarkAsBorrowed()`), and enforce invariants in their constructors and methods.
- **Data models** (e.g. `BookDataModel`, `LoanDataModel`) live in `Infrastructure/Persistence/DataModels`. They are scaffolded from SQL Server, carry EF Core navigation properties, and exist exclusively to bridge the database.

Translation between the two happens explicitly in mapper classes inside `Infrastructure/Persistence/Mappers`. The domain knows nothing about the data models.

## Alternatives Considered

### N-tier with shared entities

A simpler structure with three projects and one set of entities. Rejected because:

- Domain logic would directly depend on EF Core types (navigation properties, lazy loading), preventing the dual EF/ADO implementation from coexisting cleanly.
- Tests of business rules would need a `DbContext`, slowing the feedback loop.
- Scaffolded models are regenerated when the schema changes; embedding business rules in them creates regeneration conflicts.

### Clean Architecture with scaffolded entities reused across layers

A pragmatic compromise where scaffolded entities are also used as "domain" entities. Rejected because:

- The domain layer ends up referencing the infrastructure project, inverting the dependency rule.
- Hides the fact that domain and persistence are different concerns.
- The portfolio goal explicitly seeks to demonstrate disciplined separation, which this hybrid does not achieve.

### Vertical Slice Architecture

Feature-based organization (folders per use case) combined with MediatR. Deferred to a future iteration. Vertical Slice is excellent for large codebases with many small features but introduces conceptual overhead (CQRS, MediatR pipeline) that exceeds the value for a focused MVP.

## Consequences

### Positive

- **Domain isolation.** The `Domain` project compiles and tests in isolation, with no database, no HTTP, no framework dependencies.
- **Dual data access becomes natural.** Two repository implementations (`EfBookRepository`, `AdoBookRepository`) plug into the same domain contracts, selectable by configuration.
- **Schema changes are absorbed at the boundary.** When SQL Server schema changes, only data models and mappers are touched. Domain entities evolve based on business needs, not database needs.
- **Rich domain model.** Business rules live in entities (e.g. `Loan.Create` validates due date logic), preventing the "anemic domain" anti-pattern.
- **Defensible in interviews.** The structure aligns with industry references (Ports and Adapters, Hexagonal, Onion).

### Negative

- **More code per entity.** Every concept requires a domain entity, a data model, and a mapper. Mapping is manual (see ADR for stack auxiliary decisions), which is verbose.
- **Indirection cost.** A new contributor must understand the layering before being productive.
- **Risk of over-engineering for trivial entities.** Lookup tables (`CopyStatus`, `LoanStatus`) get the same treatment as rich aggregates; this is accepted for consistency.

### Followups / Open Items

- Mapper test coverage must be enforced to prevent silent field-loss bugs when entities grow.
- If the codebase scales, consider introducing a source generator (e.g. Mapperly) to reduce mapping boilerplate. Manual mapping was chosen for the MVP for transparency.
