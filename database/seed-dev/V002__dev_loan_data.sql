-- =============================================================================
-- V002 — DEV LOAN DATA  ⚠️  LOCAL DEVELOPMENT ONLY
-- =============================================================================
-- Members, book copies and a Reader account, so the loan feature can be tried
-- by hand (no endpoint creates members or copies). Like V001, it is applied
-- only through the `flyway-seed` service and can be re-run safely.
--
-- ⚠️ Contains a user with a known password. Never apply it to a shared or
--    production environment.
--
-- Reader account: reader@test.com / Test1234! (role Reader), linked to the
-- member Ana Mora, so GET /api/members/me/loans returns her loans.
-- =============================================================================


-- =============================================================================
-- SECTION 1 — Reader account
-- =============================================================================
-- Same PasswordHash literal as guille@test.com in V001: the salt is embedded
-- in the hash, so it verifies "Test1234!" for any user.
-- =============================================================================

DECLARE @ReaderId     NVARCHAR(450) = N'd0000000-0000-0000-0000-000000000002';
DECLARE @ReaderEmail  NVARCHAR(256) = N'reader@test.com';
DECLARE @ReaderRoleId NVARCHAR(450) = N'a0000000-0000-0000-0000-000000000003';

-- Reuse the real Id if the account was already registered by hand.
SELECT @ReaderId = ISNULL(
    (SELECT Id FROM dbo.AspNetUsers WHERE NormalizedEmail = UPPER(@ReaderEmail)),
    @ReaderId);

IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE Id = @ReaderId)
BEGIN
    INSERT INTO dbo.AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnd, LockoutEnabled, AccessFailedCount
    ) VALUES (
        @ReaderId,
        @ReaderEmail,
        UPPER(@ReaderEmail),
        @ReaderEmail,
        UPPER(@ReaderEmail),
        1,
        N'AQAAAAIAAYagAAAAEPfh6fdGnNJzDiC2gct/rjuJoQWYtqaJ1clYyO0gGcjyMI62AiuT5cY5Sj70aPlkdA==',
        CONVERT(NVARCHAR(50), NEWID()),
        CONVERT(NVARCHAR(50), NEWID()),
        NULL,
        0,
        0,
        NULL,
        1,
        0
    );
END

IF NOT EXISTS (
    SELECT 1 FROM dbo.AspNetUserRoles
    WHERE UserId = @ReaderId AND RoleId = @ReaderRoleId
)
BEGIN
    INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
    VALUES (@ReaderId, @ReaderRoleId);
END


-- =============================================================================
-- SECTION 2 — Members
-- =============================================================================
-- Ana is linked to the Reader account. Bea is inactive, to try the
-- "member_inactive" refusal. Rows are matched by email; a member is skipped if
-- its account is already linked to another member.
-- =============================================================================

INSERT INTO dbo.Members (UserId, FirstName, LastName, Email, IsActive, IsDeleted, CreatedAt)
SELECT v.UserId, v.FirstName, v.LastName, v.Email, v.IsActive, 0, SYSUTCDATETIME()
FROM (VALUES
    (@ReaderId, N'Ana',  N'Mora',    N'reader@test.com',      1),
    (NULL,      N'Luis', N'Zamora',  N'luis.zamora@test.com', 1),
    (NULL,      N'Bea',  N'Quesada', N'bea.quesada@test.com', 0)
) AS v(UserId, FirstName, LastName, Email, IsActive)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Members m WHERE m.Email = v.Email AND m.IsDeleted = 0
)
AND (v.UserId IS NULL OR NOT EXISTS (
    SELECT 1 FROM dbo.Members m WHERE m.UserId = v.UserId AND m.IsDeleted = 0
));


-- =============================================================================
-- SECTION 3 — Book copies
-- =============================================================================
-- Two Available copies (StatusId 1) of each book seeded by V001, matched by
-- ISBN. Books that are missing or soft-deleted get no copies.
-- =============================================================================

INSERT INTO dbo.BookCopies (BookId, StatusId, InventoryCode, AcquisitionDate, IsDeleted, CreatedAt)
SELECT b.Id, 1, v.InventoryCode, '2025-01-15', 0, SYSUTCDATETIME()
FROM (VALUES
    ('9780307277671', 'LIB-0001'),
    ('9780307277671', 'LIB-0002'),
    ('9780132350884', 'LIB-0003'),
    ('9780132350884', 'LIB-0004'),
    ('9780062316097', 'LIB-0005'),
    ('9780062316097', 'LIB-0006')
) AS v(Isbn, InventoryCode)
JOIN dbo.Books b ON b.ISBN = v.Isbn AND b.IsDeleted = 0
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.BookCopies c WHERE c.InventoryCode = v.InventoryCode AND c.IsDeleted = 0
);
