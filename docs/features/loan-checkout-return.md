# Feature: Loan checkout and return

- **Status:** Agreed, endpoints defined
- **Last updated:** 2026-11-01

## User story

As a **Librarian**, I want to lend a book copy to a member and register its return, so availability
is always accurate.

## Scope

**In scope**

- Checkout: create a loan and mark the copy as Borrowed.
- Return: close the loan and mark the copy as Available again.
- Reading loans: staff can read every loan; a Reader can read only their own.
- Lookups the checkout form needs: search members, and list a book's copies.
- Client: the Angular client only. The Blazor client gets no UI for loans; it only has to keep
  compiling.

**Out of scope**

- The overdue job, so no loan is ever persisted with status Overdue.
- Lost copies.
- Renewals.
- Fines.
- Recording which staff member did a checkout or return (it needs a schema change).
- The AdoNet data-access mode: the feature works in `EntityFramework` mode only.
- Creating, editing or deleting members and copies.
- Paging: lists are unpaged, like the existing endpoints.

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
| BR-09 | Only Active or Overdue loans can be returned. A return sets the loan to Returned with `ReturnDate` = today, and moves the copy to Available, in **one transaction**. The loan is identified by its **loan id**. | Service (pre-check) + Domain (`Loan.Return`) |
| BR-10 | A copy can have at most one open loan, **even under concurrent requests**: of two simultaneous checkouts, only one succeeds. | Database (see [Concurrency](#concurrency)) |
| BR-11 | Only Librarian or Administrator can check out or return. | Policy `LibrarianOrAdmin` |
| BR-12 | A Reader can read only their own loans, through `GET /api/members/me/loans` and `GET /api/loans/{id}`. The link is `Member.UserId` = the JWT `sub` claim. A Reader with no linked Member gets an empty list. Asking for someone else's loan returns 404. | Service |
| BR-13 | A return does **not** require the member to be active, or the book or member to be undeleted. A suspended member must still be able to bring a copy back. | Service |
| BR-14 | Librarian and Admin can list all loans, filtered by member, by copy and by status (`GET /api/loans`). | Service |
| BR-15 | Loans are never deleted, so there is no delete operation. | Design |
| BR-16 | A copy whose book is soft-deleted cannot be lent. Copies are not cascade-deleted with their book. | Service |
| BR-17 | A loan becomes overdue the day **after** `DueDate`. A loan due today is not overdue. This matches `Loan.IsOverdueOn(today)`. | Domain |
| BR-18 | A return always uses today's date, with no backdating. This matches `Loan.Return(today)`. | Domain |
| BR-19 | A member cannot hold two open loans for copies of the **same book**. | Service |

## API

All routes use `{id:long}`-style constraints. Every error is a ProblemDetails body. Every endpoint
can also return 401 when there is no valid token.

### Loans

| Endpoint | Policy | Success | Errors |
|---|---|---|---|
| `POST /api/loans` with body `{ memberId, bookCopyId }` | `LibrarianOrAdmin` | 201 + `LoanDto` + `Location: /api/loans/{id}` | 400 invalid body · 403 · 404 member or copy · 409 member inactive, overdue loan, loan limit, copy's book deleted, copy not Available, same book already on loan, concurrent checkout |
| `POST /api/loans/{id}/return` (no body) | `LibrarianOrAdmin` | 200 + `LoanDto` | 403 · 404 loan · 409 loan already returned or lost |
| `GET /api/loans/{id}` (route name `GetLoanById`) | `ReaderOrAbove` | 200 + `LoanDto` | 404 not found, or a Reader asking for a loan that isn't theirs (D-05) |
| `GET /api/loans?memberId=&bookCopyId=&status=` | `LibrarianOrAdmin` | 200 + `LoanDto[]` | 400 invalid `status` · 403 |
| `GET /api/members/me/loans?status=` | `ReaderOrAbove` | 200 + `LoanDto[]` (empty if no linked Member) | 400 invalid `status` |

**`status` filter:** `active` returns open loans (Active or Overdue), `returned` returns closed
loans, and leaving it out returns all loans. Any other value is a 400.

**Order:** lists are sorted by `LoanDate` (newest first), then by `Id`.

### Lookups for the checkout form

| Endpoint | Policy | Success | Errors |
|---|---|---|---|
| `GET /api/members?search=` | `LibrarianOrAdmin` | 200 + `MemberDto[]` | 403 |
| `GET /api/books/{bookId}/copies?status=` | `LibrarianOrAdmin` | 200 + `BookCopyDto[]` | 400 invalid `status` · 403 · 404 book |

- `search` matches first name, last name or email (contains, case-insensitive). Leaving it out
  returns every member.
- Members are sorted by last name, then first name.
- Inactive members are included, with `isActive`, so the UI can show why a checkout would fail.
- The copies `status` filter accepts `available`. Leaving it out returns every copy. Copies are
  sorted by inventory code.

### DTOs

| DTO | Fields |
|---|---|
| `CreateLoanDto` (request) | `memberId`, `bookCopyId` |
| `LoanDto` | `id`, `memberId`, `memberName`, `bookCopyId`, `inventoryCode`, `bookId`, `bookTitle`, `status` (stored status name), `loanDate`, `dueDate`, `returnDate`, `isOverdue` (computed, BR-17) |
| `MemberDto` | `id`, `firstName`, `lastName`, `email`, `isActive` |
| `BookCopyDto` | `id`, `bookId`, `inventoryCode`, `status`, `acquisitionDate` |

`memberName`, `inventoryCode` and `bookTitle` must load **even when the member, copy or book is
soft-deleted**, so loan history still displays.

### Order of checks

When several rules fail at once, the first failing check decides the response, so tests can
expect a specific error.

**Checkout**

1. 400: invalid body.
2. 404: member not found.
3. 404: copy not found.
4. 409: member inactive (BR-02).
5. 409: member has an overdue loan (BR-04).
6. 409: loan limit reached (BR-03).
7. 409: the copy's book is deleted (BR-16).
8. 409: copy not Available (BR-06).
9. 409: same book already on loan (BR-19).
10. 409: lost a concurrent checkout (BR-10), detected at commit.

**Return**

1. 404: loan not found.
2. 409: loan already returned or lost (BR-09).

## Concurrency

**This feature needs concurrency handling.** Today a request:

1. Reads its data *before* `SaveChangesAsync`, under the default READ COMMITTED isolation.
2. Then writes **full rows** from detached entities.

No table has a `rowversion` column, so the last writer wins without any error.

| Race | What happens today | Decision |
|---|---|---|
| Two checkouts of the **same copy** | Both succeed: two open loans for one copy. | Add a **filtered unique index on `Loans(BookCopyId)`** for open, non-deleted rows (Active or Overdue, `IsDeleted = 0`) in a new Flyway script. The loser hits a unique-key violation and its transaction rolls back, copy update included. Translate that to `ConflictException`, which gives a 409. |
| Two checkouts for the **same member**, different copies | Both pass the checks, so the member can exceed 3 loans, get around the overdue rule, or get around BR-19. | **Accepted, documented risk (D-02).** There is one desk and the window is small. |
| Two returns of the **same loan** | One after the other: the second gets 409 from the pre-check. Truly simultaneous: both succeed and end in the same state. | Harmless. No action. |
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

## Edge cases

| ID | Case | Expected behavior |
|---|---|---|
| E-01 | Double-click on "Check out" | The second request gets 409 (copy not Available, or the index). The UI disables the button while saving. |
| E-02 | Loan history of a deleted member or book | It still displays: the display fields load without the soft-delete filter. |
| E-03 | A copy is soft-deleted while on loan | Can't happen today, because nothing deletes copies. Whenever copy deletion is added, it must refuse a copy with an open loan, or the loan could never be returned. |
| E-04 | A Reader whose Member is deactivated | They can still read their own loans. |
| E-05 | A Reader whose Member is soft-deleted | Treated as having no linked Member, so they get an empty list. |
| E-06 | A staff user who is also linked to a Member | `GET /api/members/me/loans` returns their own loans, like any user. |
| E-07 | `GET /api/loans` filtered by a member or copy that doesn't exist | Empty list. It is a search, not a resource. |
| E-08 | A user's role changes | It only applies after a new login, because roles travel inside the token. |
| E-09 | The access token expires (15 min) during desk work | The Angular client doesn't refresh on 401 (known gap), so the librarian is sent back to login. Not fixed in this feature. |
| E-10 | Late-evening checkout in a timezone behind UTC | `LoanDate` is already the next UTC day. Accepted: all dates are UTC. |

## Existing issues that affect this feature

| # | Issue | Status | Plan |
|---|---|---|---|
| 1 | Creating through EF returned **`Id = 0`**, which made the loan response and the `Location` header wrong. | **Fixed** (`e3dca1a`) | Done. |
| 2 | `Loan.IsCurrentlyOverdue` compared against `CopyStatusIds.Lost`. | **Fixed** (`9e644e0`) | Done. |
| 3 | **AdoNet mode drops EF writes**, and Loan and BookCopy only have EF repositories. | Out of scope | EF only (D-03). |
| 4 | The config doesn't match the spec. appsettings has `Loan:MaxActiveLoansPerMember = 5`, and no options class exists. | Part of the feature | Set the max to 3 and add `LoanOptions`, validated at startup. |
| 5 | **Members and copies can't be looked up**, register doesn't create a Member, and the dev seed has no members or copies. | Part of the feature | Lookup endpoints (D-16), and a dev seed with members, copies and a Reader user linked to a member. |
| 6 | The current user's id is in the JWT `sub` claim (inbound claim mapping is turned off). | Note | The controller reads `sub` and passes it to the service; Application never touches `HttpContext`. |
| 7 | The domain calls `DateTime.UtcNow` directly, which makes overdue scenarios hard to test. | Addressed for loans | D-11: `Loan` takes today's date as a parameter, and `LoanService` and `EfLoanQueries` read it from `TimeProvider`. Integration tests swap in a fake clock. The rest of the code keeps `DateTime.UtcNow`. |

## Error contract

| Situation | Exception | HTTP |
|---|---|---|
| Invalid body or invalid `status` filter | `ValidationException` | 400 |
| Member, copy, book or loan not found | `NotFoundException` | 404 |
| Reader reads a loan that isn't theirs | `NotFoundException` | 404 |
| Checkout refused: member inactive, overdue loan, loan limit, copy's book deleted, copy not Available, same book already on loan, lost a concurrent checkout | `ConflictException` | 409 |
| Return refused: loan already returned or lost | `ConflictException` (service pre-check) | 409 |
| Domain invariant broken (last-resort guard) | `DomainException` | 400 |
| A Reader calls a staff-only endpoint | Authorization policy | 403 |

## Decisions

| ID | Question | Decision |
|---|---|---|
| D-01 | Is the loan limit configurable? | Yes: `Loan:MaxActiveLoansPerMember` = 3. |
| D-02 | Member race: accept the risk or serialize? | Accept it, documented. |
| D-03 | AdoNet mode: EF only, or fix the unit of work? | EF only. |
| D-04 | Status code for a refused checkout or return | 409 (`ConflictException`). |
| D-05 | Reader reads someone else's loan | 404, so existence isn't revealed. |
| D-06 | Reader with no linked Member | Empty list. |
| D-07 | Identify a return by loan id or by inventory code? | By loan id. |
| D-08 | Prerequisite data for Members and copies | Dev seed, plus the lookups in D-16. |
| D-09 | Can a copy of a soft-deleted book be lent? | No (BR-16). |
| D-10 | One copy per book per member? | Yes (BR-19). |
| D-11 | Clock: `DateTime.UtcNow` or `TimeProvider`? | `TimeProvider`, for the loan feature only. The domain gets dates as parameters (`Loan.Create(..., today)`, `Return(today)`, `IsOverdueOn(today)`, `MarkAsOverdue(today)`), so it stays clock-free. `LoanService` and `EfLoanQueries` read `TimeProvider`, registered in DI. Other entities, validators and auth keep `DateTime.UtcNow` for now. |
| D-12 | Record which staff member did the checkout or return? | No, because it needs a schema change. |
| D-13 | Which clients get the UI? | Angular only. |
| D-14 | How does a Reader get their own loans? | `GET /api/members/me/loans`, resolved from the JWT `sub` claim. |
| D-15 | How does staff list loans? | `GET /api/loans` with `memberId`, `bookCopyId` and `status` filters. It replaces the per-member `GET /api/members/{memberId}/loans`. |
| D-16 | Which lookups does the checkout form get? | `GET /api/members?search=` and `GET /api/books/{bookId}/copies?status=available`. |
| D-17 | What does `LoanDto` contain? | Ids, display fields (member name, inventory code, book title) and the computed `isOverdue`. |
| D-18 | How are `LoanDto`'s display fields loaded? | A read-only query interface in Application (`ILoanQueries`), implemented in Infrastructure with an EF projection that joins the tables and ignores the soft-delete filter. It follows the `IRefreshTokenRepository` precedent. Writes go through the repositories as usual. |
