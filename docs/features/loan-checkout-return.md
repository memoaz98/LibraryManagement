# Feature: Loan checkout and return

- **Status:** Agreed, ready for implementation
- **Last updated:** 2026-11-01

## User story

As a **Librarian**, I want to lend a book copy to a member and register its return, so availability
is always accurate.

## Scope

**In scope**

- Checkout: create a loan and mark the copy as Borrowed.
- Return: close the loan and mark the copy as Available again.
- Reading loans: staff can read every loan; a Reader can read only their own.
- Client: the Angular client only. The Blazor client gets no UI for loans; it only has to keep
  compiling.

**Out of scope**

- The overdue job, so no loan is ever persisted with status Overdue.
- Lost copies.
- Renewals.
- Fines.
- Recording which staff member did a checkout or return (it needs a schema change).
- The AdoNet data-access mode: the feature works in `EntityFramework` mode only.

## Business rules

"Open loan" means a loan whose status is Active or Overdue. Dates are UTC dates, the same as in the
domain today.

| ID | Rule | Enforced in |
|---|---|---|
| BR-01 | The member must exist. A soft-deleted member counts as not found. | Service |
| BR-02 | The member must be active (`Member.IsActive`). | Service |
| BR-03 | A member can have at most **3** open loans, configured in `Loan:MaxActiveLoansPerMember`. | Service |
| BR-04 | A member with any **overdue** loan cannot borrow. Overdue means an open loan whose `DueDate` is before today. It is **computed, not read from the stored status**, because no job persists Overdue. | Service |
| BR-05 | The copy must exist. A soft-deleted copy counts as not found. | Service |
| BR-06 | The copy must be Available. | Service (pre-check) + Domain (`BookCopy.MarkAsBorrowed`) |
| BR-07 | Due date = today + `Loan:DefaultLoanDays` (default 14). The domain allows 1 to 90. The value is checked at startup. | Domain + Options |
| BR-08 | Checkout creates an Active loan and moves the copy to Borrowed, in **one transaction** (`IUnitOfWork`). | Service |
| BR-09 | Only Active or Overdue loans can be returned. A return sets the loan to Returned with `ReturnDate` = today, and moves the copy to Available, in **one transaction**. The loan is identified by its **loan id**. | Domain + Service |
| BR-10 | A copy can have at most one open loan, **even under concurrent requests**: of two simultaneous checkouts, only one succeeds. | Database (see [Concurrency](#concurrency)) |
| BR-11 | Only Librarian or Administrator can check out or return. | Policy `LibrarianOrAdmin` |
| BR-12 | A Reader can read only their own loans. The link is `Member.UserId` = the JWT `sub` claim. A Reader with no linked Member gets an empty list. Asking for someone else's loan returns 404. | Service |
| BR-13 | A return does **not** require the member to be active, or the book or member to be undeleted. A suspended member must still be able to bring a copy back. | Service |
| BR-14 | Librarian and Admin can read all loans, filtered by member, by copy, or to open loans only. | Service |
| BR-15 | Loans are never deleted, so there is no delete operation. | Design |
| BR-16 | A copy whose book is soft-deleted cannot be lent. Copies are not cascade-deleted with their book. | Service |
| BR-17 | A loan becomes overdue the day **after** `DueDate`. A loan due today is not overdue. This matches `Loan.IsCurrentlyOverdue`. | Domain |
| BR-18 | A return always uses today's date, with no backdating. This matches `Loan.Return()`. | Domain |
| BR-19 | A member cannot hold two open loans for copies of the **same book**. | Service |

## Concurrency

**This feature needs concurrency handling.** Today a request:

1. Reads its data *before* `SaveChangesAsync`, under the default READ COMMITTED isolation.
2. Then writes **full rows** from detached entities.

No table has a `rowversion` column, so the last writer wins without any error.

| Race | What happens today | Decision |
|---|---|---|
| Two checkouts of the **same copy** | Both succeed: two open loans for one copy. | Add a **filtered unique index on `Loans(BookCopyId)`** for open, non-deleted rows (Active or Overdue, `IsDeleted = 0`) in a new Flyway script. The loser hits a unique-key violation and its transaction rolls back, copy update included. Translate that to `ConflictException`, which gives a 409. |
| Two checkouts for the **same member**, different copies | Both pass the checks, so the member can exceed 3 loans, get around the overdue rule, or get around BR-19. | **Accepted, documented risk (D-02).** There is one desk and the window is small. |
| Two returns of the **same loan** | Both "succeed" and end in the same state. | Harmless. No action. |
| A return and a checkout of the same copy | Can't happen: a checkout needs the copy Available, and that only becomes true when the return commits. | None. |

**Why the index rather than `rowversion` as the main guard:**

- The index *is* the invariant ("one open loan per copy").
- With `rowversion`, the domain entity would have to carry a version value, and that value would
  have to survive the mapping to a fresh data model that our repositories do on every update. That
  costs more and protects less.

**Exception translation:** catch `DbUpdateException` with SQL error 2601 or 2627 in `EfUnitOfWork`
and rethrow it as `ConflictException`. Infrastructure already references Application, so this keeps
the layering rules. As a side effect, the existing Categories duplicate-name case returns a 409
instead of a 500.

## Existing issues that affect this feature

| # | Issue | Blocking? | Plan |
|---|---|---|---|
| 1 | Creating through EF returns **`Id = 0`**: `EfRepositoryBase.AddAsync` never copies the generated id back to the domain entity. The loan response and the `Location` header would be wrong. | **Yes** | Fix it before the feature, in its own commit. |
| 2 | `Loan.IsCurrentlyOverdue` compares against `CopyStatusIds.Lost` where it should use `LoanStatusIds.Lost`. It works only because both values are 4. | **Yes** | Fix it before the feature, because BR-04 depends on it. |
| 3 | **AdoNet mode drops EF writes**, and Loan and BookCopy only have EF repositories. | No | EF only (D-03). Documented as out of scope. |
| 4 | The config doesn't match the spec. appsettings has `Loan:MaxActiveLoansPerMember = 5`, and no options class exists. | No | Part of the feature: set the max to 3 and add `LoanOptions`, validated at startup. |
| 5 | **Members and copies can't be created or looked up.** No service or endpoint exists for them, register doesn't create a Member, and the dev seed has no members or copies. | No | Part of the feature (D-08): extend the dev seed with members, copies and a Reader user linked to a member, and add the minimal lookups the librarian UI needs. |
| 6 | The current user's id is in the JWT `sub` claim (inbound claim mapping is turned off). | No | The controller reads `sub` and passes it to the service; Application never touches `HttpContext`. |
| 7 | The domain calls `DateTime.UtcNow` directly, which makes overdue scenarios hard to test. | No | `TimeProvider` comes later (D-11). Until then, tests build past-dated loans with `Loan.Reconstitute`. |

## Error contract

| Situation | Exception | HTTP |
|---|---|---|
| Invalid request shape | `ValidationException` | 400 |
| Member, copy or loan not found | `NotFoundException` | 404 |
| Reader reads a loan that isn't theirs | `NotFoundException` | 404 |
| Member inactive, loan limit reached, overdue loan, copy not Available, copy's book deleted, same book already on loan, lost a concurrent checkout | `ConflictException` | 409 |
| Domain invariant broken (last-resort guard) | `DomainException` | 400 |
| Caller is not Librarian or Admin for a write | Authorization policy | 403 |

## Decisions

| ID | Question | Decision |
|---|---|---|
| D-01 | Is the loan limit configurable? | Yes: `Loan:MaxActiveLoansPerMember` = 3. |
| D-02 | Member race: accept the risk or serialize? | Accept it, documented. |
| D-03 | AdoNet mode: EF only, or fix the unit of work? | EF only. |
| D-04 | Status code for a refused checkout | 409 (`ConflictException`). |
| D-05 | Reader reads someone else's loan | 404, so existence isn't revealed. |
| D-06 | Reader with no linked Member | Empty list. |
| D-07 | Identify a return by loan id or by inventory code? | By loan id. |
| D-08 | Prerequisite data for Members and copies | Dev seed, plus the minimal lookups the UI needs (to be defined with the endpoints). |
| D-09 | Can a copy of a soft-deleted book be lent? | No (BR-16). |
| D-10 | One copy per book per member? | Yes (BR-19). |
| D-11 | Clock: `DateTime.UtcNow` or `TimeProvider`? | `TimeProvider` will be introduced, but not yet. Until then the domain keeps `DateTime.UtcNow`. |
| D-12 | Record which staff member did the checkout or return? | No, because it needs a schema change. |
| D-13 | Which clients get the UI? | Angular only. |
