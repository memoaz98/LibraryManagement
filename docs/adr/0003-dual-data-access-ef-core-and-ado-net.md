# ADR-0003: Dual Data Access — EF Core and ADO.NET Behind the Same Contracts

- **Status:** Accepted
- **Date:** 2026-05-29
- **Deciders:** Project owner
- **Related:** ADR-0001 (Clean Architecture), ADR-0002 (Database First)

## Context

The project's portfolio goal is not merely to ship a working library system, but to **demonstrate understanding of the trade-offs between high-level ORMs and low-level data access** in .NET. Most candidates can speak about Entity Framework Core; far fewer can articulate, with first-hand evidence, when ADO.NET is the right tool and what it costs to use it.

The Library Management System therefore implements **the same set of repository contracts twice**: once with Entity Framework Core 9, and once with raw ADO.NET (`SqlConnection`, `SqlCommand`, `IDataReader`). The active implementation is selected by configuration at startup.

The forces shaping this decision:

- **Educational depth.** Implementing the same operations in both technologies surfaces concrete differences in change tracking, transaction handling, projection, identity map behavior, and SQL control.
- **Defensible interview narrative.** Being able to compare both technologies on a specific operation (e.g. "loading a book with its copies and authors") with code from the same project is more credible than abstract opinions.
- **Realistic enterprise pattern.** Legacy .NET systems often combine EF Core for CRUD with ADO.NET for reporting and bulk operations. Modeling both behind one contract teaches the abstraction discipline this requires.

The non-goal: building a production system that ships both implementations to a live environment. The dual setup is a teaching scaffold. In a real product, one implementation would be chosen and the other archived.

## Decision

The project implements every repository interface (e.g. `IBookRepository`, `IMemberRepository`, `ILoanRepository`) and the `IUnitOfWork` contract twice, in parallel:

```
LibraryManagement.Infrastructure/
  Persistence/
    EntityFramework/
      Repositories/
        EfBookRepository.cs
        EfMemberRepository.cs
        EfLoanRepository.cs
        ...
      EfUnitOfWork.cs
    AdoNet/
      Repositories/
        AdoBookRepository.cs
        AdoMemberRepository.cs
        AdoLoanRepository.cs
        ...
      AdoUnitOfWork.cs
```

Both implementations:

- Depend only on **domain entities and interfaces**, never on each other.
- Map between domain entities and persistence representations (data models for EF, `IDataReader` rows for ADO).
- Honor the same transactional semantics through the shared `IUnitOfWork` contract.

The active implementation is chosen by a configuration switch:

```json
// appsettings.json
"DataAccess": {
  "Provider": "EntityFramework"   // or "AdoNet"
}
```

A composition root method in `Infrastructure` (e.g. `AddInfrastructure(IConfiguration)`) reads the value and registers the corresponding implementations in the DI container. The rest of the application (services, controllers) is unaware of which provider is active. This is **Strategy Pattern** at the architectural scale, realized through Dependency Inversion (the "D" in SOLID).

## Alternatives Considered

### EF Core only

The default for new .NET projects. Rejected because:

- Does not satisfy the educational and portfolio goals of the project.
- Misses the chance to demonstrate disciplined use of repository abstractions.

### EF Core for CRUD plus targeted ADO.NET for specific queries

A pragmatic real-world pattern: EF Core handles 90% of operations, with raw ADO.NET (or Dapper) reserved for reports and bulk reads. This is what most teams end up doing in production. Rejected for this project because:

- A partial ADO.NET surface does not produce a symmetric comparison.
- The narrative "I implemented the same contract with both" is stronger and more defensible than "I sprinkled ADO.NET where it was faster."

### Dapper as the alternative to EF Core

A popular micro-ORM that sits between EF Core and raw ADO.NET. Rejected because:

- The educational target is precisely the gap between high-level and low-level access. Dapper is excellent but blurs that contrast.
- Knowing raw ADO.NET is the more transferable skill; Dapper can be learned in an afternoon afterward.

## Consequences

### Positive

- **Clean Architecture is validated by use.** The fact that two unrelated data access technologies plug behind the same domain contracts proves the dependency inversion is real, not cosmetic.
- **Concrete interview material.** Operations like "register a loan" (which involves inserting a `Loan` row and updating the related `BookCopy.StatusId` atomically) can be shown side by side, highlighting how EF Core's `SaveChanges` orchestrates the transaction implicitly while ADO.NET requires explicit `SqlTransaction` management.
- **Performance characteristics are observable.** Bulk reads, projections, and identity-map behavior differ visibly between the two. This grounds otherwise abstract discussions.
- **Strategy + DI become tangible.** The "switch a config flag and the entire data layer changes" effect is a memorable demonstration.

### Negative

- **Double maintenance.** Every new repository operation must be implemented twice. This cost is accepted explicitly and is bounded by the project's size.
- **Test surface doubles.** Each implementation needs its own integration tests once integration tests are introduced (deferred per the testing ADR).
- **Risk of contract drift.** One implementation may add a method or behavior the other lacks. Mitigated by interface-driven development and discipline.
- **Identity and transaction abstractions must be deliberately designed.** EF Core's `DbContext` is both a Unit of Work and an identity map; ADO.NET has neither out of the box. The `IUnitOfWork` contract must hide this difference cleanly.

### Followups / Open Items

- A comparison document (or section in `ARCHITECTURE.md`) will record, per use case, observed differences between the two implementations (lines of code, SQL emitted, transaction handling, performance notes).
- The transaction strategy must be documented: how the EF and ADO `IUnitOfWork` implementations expose equivalent semantics from very different underlying mechanisms.
- If the project is ever taken to production, one implementation will be chosen and the other archived; the abstraction makes this a configuration change, not a refactor.
