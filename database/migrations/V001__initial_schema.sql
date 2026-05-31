-- =============================================================================
-- V001 — INITIAL SCHEMA
-- LibraryManagement: tablas de Identity, dominio, y catálogos.
-- =============================================================================
-- Convenciones aplicadas:
--   * Tablas dbo.* — sin esquemas separados por simplicidad inicial.
--   * Identidad numérica para entidades del dominio (BIGINT INT según tamaño).
--   * Identidad GUID-string para Identity (compatible con AspNetUsers default).
--   * UTC siempre en columnas de fecha de auditoría (CreatedAt, etc.).
--   * Soft delete vía columna IsDeleted en entidades de dominio.
--   * Tablas catálogo (LoanStatus, CopyStatus) NO tienen soft delete ni auditoría.
-- =============================================================================


-- =============================================================================
-- SECCIÓN 1 — ASP.NET CORE IDENTITY
-- =============================================================================
-- Estructura exacta que EF Core espera para Identity stores con string keys.
-- =============================================================================

CREATE TABLE dbo.AspNetRoles (
    Id NVARCHAR(450) NOT NULL,
    Name NVARCHAR(256) NULL,
    NormalizedName NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetRoles PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles (NormalizedName)
    WHERE NormalizedName IS NOT NULL;


CREATE TABLE dbo.AspNetUsers (
    Id NVARCHAR(450) NOT NULL,
    UserName NVARCHAR(256) NULL,
    NormalizedUserName NVARCHAR(256) NULL,
    Email NVARCHAR(256) NULL,
    NormalizedEmail NVARCHAR(256) NULL,
    EmailConfirmed BIT NOT NULL,
    PasswordHash NVARCHAR(MAX) NULL,
    SecurityStamp NVARCHAR(MAX) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL,
    PhoneNumber NVARCHAR(MAX) NULL,
    PhoneNumberConfirmed BIT NOT NULL,
    TwoFactorEnabled BIT NOT NULL,
    LockoutEnd DATETIMEOFFSET(7) NULL,
    LockoutEnabled BIT NOT NULL,
    AccessFailedCount INT NOT NULL,
    CONSTRAINT PK_AspNetUsers PRIMARY KEY (Id)
);
CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);
CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName)
    WHERE NormalizedUserName IS NOT NULL;


CREATE TABLE dbo.AspNetRoleClaims (
    Id INT IDENTITY(1,1) NOT NULL,
    RoleId NVARCHAR(450) NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY (Id),
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId
        FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims (RoleId);


CREATE TABLE dbo.AspNetUserClaims (
    Id INT IDENTITY(1,1) NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetUserClaims PRIMARY KEY (Id),
    CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId
        FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims (UserId);


CREATE TABLE dbo.AspNetUserLogins (
    LoginProvider NVARCHAR(450) NOT NULL,
    ProviderKey NVARCHAR(450) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX) NULL,
    UserId NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId
        FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins (UserId);


CREATE TABLE dbo.AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL,
    RoleId NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId
        FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId
        FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles (RoleId);


CREATE TABLE dbo.AspNetUserTokens (
    UserId NVARCHAR(450) NOT NULL,
    LoginProvider NVARCHAR(450) NOT NULL,
    Name NVARCHAR(450) NOT NULL,
    Value NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId
        FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);


-- =============================================================================
-- SECCIÓN 2 — REFRESH TOKENS (JWT)
-- =============================================================================
-- Tabla custom para los refresh tokens del flujo JWT.
-- Persistimos el HASH, no el token plano (defensa en profundidad).
-- =============================================================================

CREATE TABLE dbo.RefreshTokens (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    TokenHash VARBINARY(32) NOT NULL,   -- SHA-256 produce 32 bytes
    ExpiresAt DATETIME2(7) NOT NULL,
    CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT (SYSUTCDATETIME()),
    RevokedAt DATETIME2(7) NULL,
    CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id),
    CONSTRAINT FK_RefreshTokens_AspNetUsers_UserId
        FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON dbo.RefreshTokens (TokenHash);
CREATE INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId);


-- =============================================================================
-- SECCIÓN 3 — TABLAS CATÁLOGO
-- =============================================================================
-- Estados pre-definidos con IDs estables. NO tienen soft delete ni auditoría.
-- Los IDs son intencionales y constantes (no IDENTITY) — el código C# los referencia.
-- =============================================================================

CREATE TABLE dbo.CopyStatus (
    Id TINYINT NOT NULL,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL,
    CONSTRAINT PK_CopyStatus PRIMARY KEY (Id),
    CONSTRAINT UX_CopyStatus_Name UNIQUE (Name)
);


CREATE TABLE dbo.LoanStatus (
    Id TINYINT NOT NULL,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL,
    CONSTRAINT PK_LoanStatus PRIMARY KEY (Id),
    CONSTRAINT UX_LoanStatus_Name UNIQUE (Name)
);


-- =============================================================================
-- SECCIÓN 4 — TABLAS DE DOMINIO
-- =============================================================================
-- Todas heredan el patrón:
--   * Id BIGINT IDENTITY como PK
--   * CreatedAt, IsDeleted para soft delete y auditoría
-- =============================================================================

CREATE TABLE dbo.Categories (
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Categories_IsDeleted DEFAULT (0),
    CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Categories PRIMARY KEY (Id),
    CONSTRAINT UX_Categories_Name UNIQUE (Name)
);


CREATE TABLE dbo.Authors (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    DateOfBirth DATE NULL,
    Biography NVARCHAR(MAX) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Authors_IsDeleted DEFAULT (0),
    CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_Authors_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Authors PRIMARY KEY (Id)
);
CREATE INDEX IX_Authors_LastName ON dbo.Authors (LastName);


CREATE TABLE dbo.Books (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    CategoryId INT NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    ISBN NVARCHAR(13) NOT NULL,
    PublicationYear SMALLINT NOT NULL,
    Synopsis NVARCHAR(MAX) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Books_IsDeleted DEFAULT (0),
    CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_Books_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Books PRIMARY KEY (Id),
    CONSTRAINT FK_Books_Categories_CategoryId
        FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id),
    CONSTRAINT CK_Books_PublicationYear CHECK (PublicationYear BETWEEN 1450 AND 2100)
);
CREATE UNIQUE INDEX UX_Books_ISBN ON dbo.Books (ISBN) WHERE IsDeleted = 0;
CREATE INDEX IX_Books_CategoryId ON dbo.Books (CategoryId);
CREATE INDEX IX_Books_Title ON dbo.Books (Title);


CREATE TABLE dbo.BookCopies (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    BookId BIGINT NOT NULL,
    StatusId TINYINT NOT NULL,
    InventoryCode NVARCHAR(20) NOT NULL,
    AcquisitionDate DATE NOT NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_BookCopies_IsDeleted DEFAULT (0),
    CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_BookCopies_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_BookCopies PRIMARY KEY (Id),
    CONSTRAINT FK_BookCopies_Books_BookId
        FOREIGN KEY (BookId) REFERENCES dbo.Books(Id),
    CONSTRAINT FK_BookCopies_CopyStatus_StatusId
        FOREIGN KEY (StatusId) REFERENCES dbo.CopyStatus(Id)
);
CREATE UNIQUE INDEX UX_BookCopies_InventoryCode ON dbo.BookCopies (InventoryCode)
    WHERE IsDeleted = 0;
CREATE INDEX IX_BookCopies_BookId ON dbo.BookCopies (BookId);
CREATE INDEX IX_BookCopies_StatusId ON dbo.BookCopies (StatusId);


CREATE TABLE dbo.Members (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    UserId NVARCHAR(450) NULL,   -- FK opcional a AspNetUsers
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    MembershipDate DATE NOT NULL CONSTRAINT DF_Members_MembershipDate DEFAULT (CAST(SYSUTCDATETIME() AS DATE)),
    IsActive BIT NOT NULL CONSTRAINT DF_Members_IsActive DEFAULT (1),
    IsDeleted BIT NOT NULL CONSTRAINT DF_Members_IsDeleted DEFAULT (0),
    CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_Members_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Members PRIMARY KEY (Id),
    CONSTRAINT FK_Members_AspNetUsers_UserId
        FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE SET NULL
);
CREATE UNIQUE INDEX UX_Members_Email ON dbo.Members (Email) WHERE IsDeleted = 0;
CREATE UNIQUE INDEX UX_Members_UserId ON dbo.Members (UserId)
    WHERE UserId IS NOT NULL AND IsDeleted = 0;


CREATE TABLE dbo.Loans (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    BookCopyId BIGINT NOT NULL,
    MemberId BIGINT NOT NULL,
    StatusId TINYINT NOT NULL,
    LoanDate DATE NOT NULL CONSTRAINT DF_Loans_LoanDate DEFAULT (CAST(SYSUTCDATETIME() AS DATE)),
    DueDate DATE NOT NULL,
    ReturnDate DATE NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Loans_IsDeleted DEFAULT (0),
    CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_Loans_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Loans PRIMARY KEY (Id),
    CONSTRAINT FK_Loans_BookCopies_BookCopyId
        FOREIGN KEY (BookCopyId) REFERENCES dbo.BookCopies(Id),
    CONSTRAINT FK_Loans_Members_MemberId
        FOREIGN KEY (MemberId) REFERENCES dbo.Members(Id),
    CONSTRAINT FK_Loans_LoanStatus_StatusId
        FOREIGN KEY (StatusId) REFERENCES dbo.LoanStatus(Id),
    CONSTRAINT CK_Loans_DueDate_After_LoanDate CHECK (DueDate >= LoanDate),
    CONSTRAINT CK_Loans_ReturnDate_After_LoanDate
        CHECK (ReturnDate IS NULL OR ReturnDate >= LoanDate)
);
CREATE INDEX IX_Loans_BookCopyId ON dbo.Loans (BookCopyId);
CREATE INDEX IX_Loans_MemberId ON dbo.Loans (MemberId);
CREATE INDEX IX_Loans_StatusId ON dbo.Loans (StatusId);
CREATE INDEX IX_Loans_DueDate ON dbo.Loans (DueDate) WHERE ReturnDate IS NULL;


-- =============================================================================
-- SECCIÓN 5 — TABLA INTERMEDIA N:N
-- =============================================================================
-- Book ↔ Author es muchos a muchos.
-- =============================================================================

CREATE TABLE dbo.BookAuthors (
    BookId BIGINT NOT NULL,
    AuthorId BIGINT NOT NULL,
    CONSTRAINT PK_BookAuthors PRIMARY KEY (BookId, AuthorId),
    CONSTRAINT FK_BookAuthors_Books_BookId
        FOREIGN KEY (BookId) REFERENCES dbo.Books(Id) ON DELETE CASCADE,
    CONSTRAINT FK_BookAuthors_Authors_AuthorId
        FOREIGN KEY (AuthorId) REFERENCES dbo.Authors(Id) ON DELETE CASCADE
);
CREATE INDEX IX_BookAuthors_AuthorId ON dbo.BookAuthors (AuthorId);