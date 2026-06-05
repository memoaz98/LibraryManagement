using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Persistence.DataModels;
using LibraryManagement.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Infrastructure.Persistence.Repositories.Ado;

/// <summary>
/// ADO.NET implementation of <see cref="ICategoryRepository"/>.
/// </summary>
/// <remarks>
/// All commands execute on the connection and transaction owned by the
/// shared <see cref="AdoUnitOfWork"/>. This guarantees ACID semantics across
/// repositories that participate in the same business operation.
/// </remarks>
public class AdoCategoryRepository : ICategoryRepository
{
    private readonly AdoUnitOfWork _uow;

    public AdoCategoryRepository(IUnitOfWork uow)
    {
        _uow = (AdoUnitOfWork)uow
            ?? throw new ArgumentNullException(nameof(uow));
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync — single row by primary key
    // -----------------------------------------------------------------------

    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Name, Description, IsDeleted, CreatedAt
            FROM dbo.Categories
            WHERE Id = @Id AND IsDeleted = 0;";

        var connection = await _uow.GetConnectionAsync(cancellationToken);
        var transaction = await _uow.GetTransactionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var dataModel = MapFromReader(reader);
        return CategoryMapper.ToDomain(dataModel);
    }

    // -----------------------------------------------------------------------
    // ListAsync — all non-deleted rows
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Name, Description, IsDeleted, CreatedAt
            FROM dbo.Categories
            WHERE IsDeleted = 0
            ORDER BY Name;";

        var connection = await _uow.GetConnectionAsync(cancellationToken);
        var transaction = await _uow.GetTransactionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Category>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var dataModel = MapFromReader(reader);
            results.Add(CategoryMapper.ToDomain(dataModel));
        }

        return results;
    }

    // -----------------------------------------------------------------------
    // FindByNameAsync — single row by unique-ish field
    // -----------------------------------------------------------------------

    public async Task<Category?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Name, Description, IsDeleted, CreatedAt
            FROM dbo.Categories
            WHERE Name = @Name AND IsDeleted = 0;";

        var connection = await _uow.GetConnectionAsync(cancellationToken);
        var transaction = await _uow.GetTransactionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 100).Value = name;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var dataModel = MapFromReader(reader);
        return CategoryMapper.ToDomain(dataModel);
    }

    // -----------------------------------------------------------------------
    // NameExistsAsync — boolean existence check
    // -----------------------------------------------------------------------

    public async Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP 1 1
            FROM dbo.Categories
            WHERE Name = @Name AND IsDeleted = 0;";

        var connection = await _uow.GetConnectionAsync(cancellationToken);
        var transaction = await _uow.GetTransactionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 100).Value = name;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    // -----------------------------------------------------------------------
    // AddAsync — INSERT
    // -----------------------------------------------------------------------

    public async Task AddAsync(Category entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.Categories (Name, Description, IsDeleted, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Description, @IsDeleted, @CreatedAt);";

        var connection = await _uow.GetConnectionAsync(cancellationToken);
        var transaction = await _uow.GetTransactionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 100).Value = entity.Name;
        command.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar, 500).Value =
            (object?)entity.Description ?? DBNull.Value;
        command.Parameters.Add("@IsDeleted", System.Data.SqlDbType.Bit).Value = entity.IsDeleted;
        command.Parameters.Add("@CreatedAt", System.Data.SqlDbType.DateTime2).Value = entity.CreatedAt;

        var generatedId = await command.ExecuteScalarAsync(cancellationToken);
        if (generatedId is int newId)
        {
            AssignIdViaReflection(entity, newId);
        }

        _uow.RegisterStagedOperation();
    }

    // -----------------------------------------------------------------------
    // Update — UPDATE
    // -----------------------------------------------------------------------

    public void Update(Category entity)
    {
        const string sql = @"
            UPDATE dbo.Categories
            SET Name = @Name,
                Description = @Description,
                IsDeleted = @IsDeleted
            WHERE Id = @Id;";

        var connection = _uow.GetConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
        var transaction = _uow.GetTransactionAsync(CancellationToken.None).GetAwaiter().GetResult();

        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = entity.Id;
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 100).Value = entity.Name;
        command.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar, 500).Value =
            (object?)entity.Description ?? DBNull.Value;
        command.Parameters.Add("@IsDeleted", System.Data.SqlDbType.Bit).Value = entity.IsDeleted;

        command.ExecuteNonQuery();
        _uow.RegisterStagedOperation();
    }

    // -----------------------------------------------------------------------
    // Remove — soft delete via Update
    // -----------------------------------------------------------------------

    public void Remove(Category entity)
    {
        entity.MarkAsDeleted();
        Update(entity);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static CategoryDataModel MapFromReader(SqlDataReader reader)
    {
        return new CategoryDataModel
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                ? null
                : reader.GetString(reader.GetOrdinal("Description")),
            IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    /// <summary>
    /// Assigns the database-generated Id back into the domain entity.
    /// Domain setters are private — reflection is the pragmatic escape hatch
    /// for persistence concerns to write Id without polluting the domain
    /// with public setters.
    /// </summary>
    private static void AssignIdViaReflection(Category entity, int newId)
    {
        var idProperty = typeof(Category).GetProperty(
            "Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        idProperty?.SetValue(entity, newId);
    }
}