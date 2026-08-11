-- =============================================================================
-- V001 — DEV TEST DATA  ⚠️  SOLO PARA DESARROLLO LOCAL
-- =============================================================================
-- Este script NO forma parte de las migraciones de producción. Vive en
-- database/seed-dev/ y solo se aplica invocando explícitamente el servicio
-- `flyway-seed` del docker-compose (profile: seed).
--
-- Usa su propia tabla de historial (flyway_seed_history) para no interferir
-- con el versionado real del esquema en flyway_schema_history.
--
-- ⚠️ Contiene un usuario con contraseña conocida y rol Administrator.
--    NUNCA lo apliques contra un ambiente compartido o productivo.
--
-- Es idempotente: se puede re-ejecutar sin duplicar datos ni fallar.
-- =============================================================================


-- =============================================================================
-- SECCIÓN 1 — Categorías de prueba
-- =============================================================================
-- Id es IDENTITY, así que no se especifica. El filtro por Name evita duplicados
-- si el script se corre de nuevo tras un reset parcial.
-- =============================================================================

INSERT INTO dbo.Categories (Name, Description, IsDeleted, CreatedAt)
SELECT v.Name, v.Description, 0, SYSUTCDATETIME()
FROM (VALUES
    (N'Ficción',              N'Novela, cuento y narrativa de imaginación'),
    (N'No Ficción',           N'Ensayo, biografía, crónica y divulgación'),
    (N'Ciencia y Tecnología', N'Matemática, física, computación e ingeniería'),
    (N'Historia',             N'Historia universal, regional y biografías históricas'),
    (N'Infantil y Juvenil',   N'Literatura para lectores de 0 a 17 años'),
    (N'Referencia',           N'Diccionarios, enciclopedias y atlas')
) AS v(Name, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Categories c WHERE c.Name = v.Name
);


-- =============================================================================
-- SECCIÓN 2 — Usuario de pruebas
-- =============================================================================
-- Credenciales:  guille@test.com  /  Test1234!
--
-- El PasswordHash es formato ASP.NET Core Identity V3 (PBKDF2-HMAC-SHA512,
-- 100.000 iteraciones, salt de 128 bits embebido). Se generó registrando el
-- usuario vía el endpoint real de la API, así que Identity lo valida sin
-- problema. El salt va dentro del hash — por eso el literal es reutilizable.
--
-- Id con GUID fijo y legible (termina en 001), mismo criterio que los roles
-- de V002 en las migraciones reales: mantiene trazabilidad entre ambientes.
--
-- EmailConfirmed = 1 para saltar el flujo de confirmación en desarrollo.
-- =============================================================================

DECLARE @UserId       NVARCHAR(450) = N'd0000000-0000-0000-0000-000000000001';
DECLARE @Email        NVARCHAR(256) = N'guille@test.com';
DECLARE @AdminRoleId  NVARCHAR(450) = N'a0000000-0000-0000-0000-000000000001';

-- Si el usuario ya existe (p.ej. registrado a mano desde el frontend), se
-- reutiliza su Id real en lugar de intentar insertar uno nuevo y chocar
-- contra el índice único de NormalizedEmail.
SELECT @UserId = ISNULL(
    (SELECT Id FROM dbo.AspNetUsers WHERE NormalizedEmail = UPPER(@Email)),
    @UserId);

IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE Id = @UserId)
BEGIN
    INSERT INTO dbo.AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnd, LockoutEnabled, AccessFailedCount
    ) VALUES (
        @UserId,
        @Email,
        UPPER(@Email),
        @Email,
        UPPER(@Email),
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


-- =============================================================================
-- SECCIÓN 3 — Promoción a Administrator
-- =============================================================================
-- El rol ya existe: lo siembra V002 de las migraciones reales con GUID fijo.
-- Se asigna sin remover otros roles previos (Reader, por ejemplo), porque
-- Identity soporta múltiples roles por usuario y quitarlos no aporta nada.
-- =============================================================================

IF NOT EXISTS (
    SELECT 1 FROM dbo.AspNetUserRoles
    WHERE UserId = @UserId AND RoleId = @AdminRoleId
)
BEGIN
    INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
    VALUES (@UserId, @AdminRoleId);
END
