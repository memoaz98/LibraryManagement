-- =============================================================================
-- V003 — ONE OPEN LOAN PER COPY
-- =============================================================================
-- A copy can be lent to one member at a time. Checkout checks the copy's
-- status first, but two simultaneous checkouts can both pass that check; this
-- index makes the database refuse the second insert, and the API answers 409.
--
-- "Open" means LoanStatus Active (1) or Overdue (3), the ids seeded by V002.
-- Returned and Lost loans are history and may repeat for the same copy.
-- =============================================================================

CREATE UNIQUE INDEX UX_Loans_BookCopyId_Open
    ON dbo.Loans (BookCopyId)
    WHERE StatusId IN (1, 3) AND IsDeleted = 0;
