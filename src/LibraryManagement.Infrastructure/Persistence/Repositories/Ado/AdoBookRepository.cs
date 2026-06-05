using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Persistence.DataModels;
using LibraryManagement.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Infrastructure.Persistence.Repositories.Ado;

/// <summary>
/// ADO.NET implementation of <see cref="IBookRepository"/>.
/// </summary>
public class AdoBookRepository : IBookRepository
{
    private readonly AdoUnitOfWork _uow;

    public AdoBookRepository(IUnitOfWork uow)
    {
        _uow = (AdoUnitOfWork)uow
            ?? throw new ArgumentNullException(nameof(uow));
    }

    private const string BaseSelect = @"
        SELECT Id, CategoryId, Title, ISBN, PublicationYear, Synopsis, IsDeleted, CreatedAt
        FROM dbo.Books";

    // -----------------------------------------------------------------------
    // Reads
    // -----------------------------------------------------------------------

    public async Task<Book?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = BaseSelect + " WHERE Id = @Id AND IsDeleted = 0;";

        var connection = await _uow.GetConnectionAsync(cancellationToken);
        var transaction = await _uow.GetTransactionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", System.Data.SqlDbType.BigInt).Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return BookMapper.ToDomain(MapFromReader(reader));
    }

    public async Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken = default)
    {
        var sql = BaseSelect + " WHERE IsDeleted = 0 ORDER BY Title;";

        var connection = await _uow.GetConnectionAsync(cancellationToken);
        var transaction = await _uow.GetTransactionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Book>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(BookMapper.ToDomain(MapFromReader(reader)));
        }

        return results;
    }

    public async Task<Book?> FindByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        var sql = BaseSelect + " WHERE ISBN = @Isbn AND IsDeleted = 0;";

        var connection = await _uow.GetConnectionAsync(cancellationToken);
        var transaction = await _uow.GetTransactionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Isbn", System.Data.SqlDbType.NVarChar, 13).Value = isbn;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return BookMapper.ToDomain(MapFromReader(reader));
    }

    public async Task<bool> IsbnExistsAsync(string isbn, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP 1 1
            FROM dbo.Books
            WHERE ISBN = @Isbn AND IsDeleted = 0;";

        var connection = await _uow.GetConnectionAsync(cancellationToken);
        var transaction = await _uow.GetTransactionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Isbn", System.Data.SqlDbType.NVarChar, 13).Value = isbn;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task<IReadOnlyList<Book>> FindByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var sql = BaseSelect + " WHERE CategoryId = @CategoryId AND IsDeleted = 0 ORDER BY Title;";

        var connection = await _uow.GetConnectionAsync(cancellationToken);
        var transaction = await _uow.GetTransactionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@CategoryId", System.Data.SqlDbType.Int).Value = categoryId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Book>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(BookMapper.ToDomain(MapFromReader(reader)));
        }

        return results;
    }

    // -----------------------------------------------------------------------
    // Writes
    // -----------------------------------------------------------------------

    public async Task AddAsync(Book entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.Books
                (CategoryId, Title, ISBN, PublicationYear, Synopsis, IsDeleted, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES
                (@CategoryId, @Title, @Isbn, @PublicationYear, @Synopsis, @IsDeleted, @CreatedAt);";

        var connection = await _uow.GetConnectionAsync(cancellationToken);
        var transaction = await _uow.GetTransactionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@CategoryId", System.Data.SqlDbType.Int).Value = entity.CategoryId;
        command.Parameters.Add("@Title", System.Data.SqlDbType.NVarChar, 255).Value = entity.Title;
        command.Parameters.Add("@Isbn", System.Data.SqlDbType.NVarChar, 13).Value = entity.Isbn;
        command.Parameters.Add("@PublicationYear", System.Data.SqlDbType.SmallInt).Value = entity.PublicationYear;
        command.Parameters.Add("@Synopsis", System.Data.SqlDbType.NVarChar, -1).Value =
            (object?)entity.Synopsis ?? DBNull.Value;
        command.Parameters.Add("@IsDeleted", System.Data.SqlDbType.Bit).Value = entity.IsDeleted;
        command.Parameters.Add("@CreatedAt", System.Data.SqlDbType.DateTime2).Value = entity.CreatedAt;

        var generatedId = await command.ExecuteScalarAsync(cancellationToken);
        if (generatedId is long newId)
        {
            AssignIdViaReflection(entity, newId);
        }

        _uow.RegisterStagedOperation();
    }

    /// <remarks>
    /// Only mutable fields are updated. ISBN and PublicationYear are
    /// intentionally immutable in the domain and therefore not part of the
    /// UPDATE statement.
    /// </remarks>
    public void Update(Book entity)
    {
        const string sql = @"
            UPDATE dbo.Books
            SET CategoryId = @CategoryId,
                Title = @Title,
                Synopsis = @Synopsis,
                IsDeleted = @IsDeleted
            WHERE Id = @Id;";

        var connection = _uow.GetConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
        var transaction = _uow.GetTransactionAsync(CancellationToken.None).GetAwaiter().GetResult();

        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", System.Data.SqlDbType.BigInt).Value = entity.Id;
        command.Parameters.Add("@CategoryId", System.Data.SqlDbType.Int).Value = entity.CategoryId;
        command.Parameters.Add("@Title", System.Data.SqlDbType.NVarChar, 255).Value = entity.Title;
        command.Parameters.Add("@Synopsis", System.Data.SqlDbType.NVarChar, -1).Value =
            (object?)entity.Synopsis ?? DBNull.Value;
        command.Parameters.Add("@IsDeleted", System.Data.SqlDbType.Bit).Value = entity.IsDeleted;

        command.ExecuteNonQuery();
        _uow.RegisterStagedOperation();
    }

    public void Remove(Book entity)
    {
        entity.MarkAsDeleted();
        Update(entity);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static BookDataModel MapFromReader(SqlDataReader reader)
    {
        return new BookDataModel
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Isbn = reader.GetString(reader.GetOrdinal("ISBN")),
            PublicationYear = reader.GetInt16(reader.GetOrdinal("PublicationYear")),
            Synopsis = reader.IsDBNull(reader.GetOrdinal("Synopsis"))
                ? null
                : reader.GetString(reader.GetOrdinal("Synopsis")),
            IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static void AssignIdViaReflection(Book entity, long newId)
    {
        var idProperty = typeof(Book).GetProperty(
            "Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        idProperty?.SetValue(entity, newId);
    }
}