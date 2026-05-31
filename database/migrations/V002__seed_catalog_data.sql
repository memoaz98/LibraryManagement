-- =============================================================================
-- V002 — SEED CATALOG DATA
-- Datos iniciales obligatorios: estados de catálogo y roles del sistema.
-- Los IDs son INTENCIONALMENTE FIJOS y deben coincidir con las constantes
-- definidas en el código C# (CopyStatusIds, LoanStatusIds, RoleIds).
-- =============================================================================


-- =============================================================================
-- SECCIÓN 1 — CopyStatus (estados de ejemplares)
-- =============================================================================

INSERT INTO dbo.CopyStatus (Id, Name, Description) VALUES
    (1, 'Available',    'Ejemplar disponible para préstamo'),
    (2, 'Borrowed',     'Ejemplar actualmente prestado a un socio'),
    (3, 'Maintenance',  'Ejemplar en reparación o restauración'),
    (4, 'Lost',         'Ejemplar reportado como perdido');


-- =============================================================================
-- SECCIÓN 2 — LoanStatus (estados de préstamos)
-- =============================================================================

INSERT INTO dbo.LoanStatus (Id, Name, Description) VALUES
    (1, 'Active',    'Préstamo en curso, ejemplar en posesión del socio'),
    (2, 'Returned',  'Ejemplar devuelto correctamente'),
    (3, 'Overdue',   'Préstamo vencido — fecha límite superada'),
    (4, 'Lost',      'Préstamo cerrado por extravío del ejemplar');


-- =============================================================================
-- SECCIÓN 3 — AspNetRoles (roles del sistema)
-- =============================================================================
-- GUIDs intencionalmente legibles (terminan en 001/002/003) para identificarlos
-- fácil al inspeccionar la BD. NewId() generaría GUIDs aleatorios y distintos
-- entre ambientes, lo cual destruiría la trazabilidad.
-- ConcurrencyStamp es un GUID nuevo por rol (lo exige Identity).
-- =============================================================================

INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES
    ('a0000000-0000-0000-0000-000000000001',
     'Administrator',
     'ADMINISTRATOR',
     CONVERT(NVARCHAR(50), NEWID())),

    ('a0000000-0000-0000-0000-000000000002',
     'Librarian',
     'LIBRARIAN',
     CONVERT(NVARCHAR(50), NEWID())),

    ('a0000000-0000-0000-0000-000000000003',
     'Reader',
     'READER',
     CONVERT(NVARCHAR(50), NEWID()));