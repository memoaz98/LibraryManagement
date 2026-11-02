using LibraryManagement.Domain.Constants;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.WebApi.IntegrationTests.Fixtures;

/// <summary>
/// Inserts and reads rows straight in the test database, bypassing the API
/// and the domain rules. Each test builds exactly the state it needs,
/// including states the API cannot produce, such as past-dated loans or
/// soft-deleted rows.
/// </summary>
/// <remarks>
/// Unique columns (names, ISBNs, inventory codes, emails) get generated
/// values unless the test passes its own.
/// </remarks>
public sealed class TestData
{
    private static readonly DateOnly AcquisitionDate = new(2025, 1, 1);

    private readonly string _connectionString;
    private int _sequence;

    public TestData(string connectionString)
    {
        _connectionString = connectionString;
    }

    // -----------------------------------------------------------------------
    // Inserts
    // -----------------------------------------------------------------------

    /// <summary>Adds a book in a new category.</summary>
    public async Task<long> AddBookAsync(string? title = null, bool isDeleted = false)
    {
        var n = Next();
        var categoryId = await ScalarAsync<int>(
            "INSERT INTO dbo.Categories (Name) OUTPUT INSERTED.Id VALUES (@Name);",
            ("@Name", $"Category {n}"));

        return await ScalarAsync<long>(
            """
            INSERT INTO dbo.Books (CategoryId, Title, ISBN, PublicationYear, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@CategoryId, @Title, @Isbn, 2020, @IsDeleted);
            """,
            ("@CategoryId", categoryId),
            ("@Title", title ?? $"Book {n}"),
            ("@Isbn", $"978{n:D10}"),
            ("@IsDeleted", isDeleted));
    }

    public Task<long> AddCopyAsync(
        long bookId,
        string? inventoryCode = null,
        byte statusId = CopyStatusIds.Available,
        bool isDeleted = false)
    {
        var n = Next();
        return ScalarAsync<long>(
            """
            INSERT INTO dbo.BookCopies (BookId, StatusId, InventoryCode, AcquisitionDate, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@BookId, @StatusId, @InventoryCode, @AcquisitionDate, @IsDeleted);
            """,
            ("@BookId", bookId),
            ("@StatusId", statusId),
            ("@InventoryCode", inventoryCode ?? $"INV-{n:D5}"),
            ("@AcquisitionDate", AcquisitionDate),
            ("@IsDeleted", isDeleted));
    }

    /// <summary>
    /// Adds an Identity user, needed when a member is linked to an account
    /// (<c>Members.UserId</c> is a foreign key). Returns the user id.
    /// </summary>
    public async Task<string> AddUserAsync()
    {
        var userId = Guid.NewGuid().ToString();
        var userName = $"user{Next()}@test.local";

        await ExecuteAsync(
            """
            INSERT INTO dbo.AspNetUsers
                (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
                 PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES
                (@Id, @UserName, UPPER(@UserName), @UserName, UPPER(@UserName), 1, 0, 0, 0, 0);
            """,
            ("@Id", userId),
            ("@UserName", userName));

        return userId;
    }

    public Task<long> AddMemberAsync(
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        bool isActive = true,
        string? userId = null,
        bool isDeleted = false)
    {
        var n = Next();
        return ScalarAsync<long>(
            """
            INSERT INTO dbo.Members (UserId, FirstName, LastName, Email, IsActive, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @FirstName, @LastName, @Email, @IsActive, @IsDeleted);
            """,
            ("@UserId", userId),
            ("@FirstName", firstName ?? $"First{n}"),
            ("@LastName", lastName ?? $"Last{n}"),
            ("@Email", email ?? $"member{n}@test.local"),
            ("@IsActive", isActive),
            ("@IsDeleted", isDeleted));
    }

    /// <summary>
    /// Adds an open loan with status Active. Like a real checkout, it also
    /// marks the copy as Borrowed unless <paramref name="borrowCopy"/> is false.
    /// </summary>
    public Task<long> AddActiveLoanAsync(
        long memberId,
        long bookCopyId,
        DateOnly loanDate,
        DateOnly dueDate,
        bool borrowCopy = true)
    {
        return AddLoanAsync(memberId, bookCopyId, LoanStatusIds.Active, loanDate, dueDate, null, borrowCopy);
    }

    /// <summary>Adds an open loan stored with status Overdue, and marks the copy as Borrowed.</summary>
    public Task<long> AddOverdueLoanAsync(long memberId, long bookCopyId, DateOnly loanDate, DateOnly dueDate)
    {
        return AddLoanAsync(memberId, bookCopyId, LoanStatusIds.Overdue, loanDate, dueDate, null, borrowCopy: true);
    }

    /// <summary>Adds a closed loan. The copy's status is left as it is.</summary>
    public Task<long> AddReturnedLoanAsync(
        long memberId,
        long bookCopyId,
        DateOnly loanDate,
        DateOnly dueDate,
        DateOnly returnDate)
    {
        return AddLoanAsync(memberId, bookCopyId, LoanStatusIds.Returned, loanDate, dueDate, returnDate, borrowCopy: false);
    }

    // -----------------------------------------------------------------------
    // Reads, for asserting what the API persisted
    // -----------------------------------------------------------------------

    public Task<byte> GetCopyStatusAsync(long bookCopyId)
    {
        return ScalarAsync<byte>(
            "SELECT StatusId FROM dbo.BookCopies WHERE Id = @Id;",
            ("@Id", bookCopyId));
    }

    public Task<int> CountLoansForCopyAsync(long bookCopyId)
    {
        return ScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.Loans WHERE BookCopyId = @BookCopyId;",
            ("@BookCopyId", bookCopyId));
    }

    /// <summary>Counts the copy's open loans (Active or Overdue).</summary>
    public Task<int> CountOpenLoansForCopyAsync(long bookCopyId)
    {
        return ScalarAsync<int>(
            """
            SELECT COUNT(*) FROM dbo.Loans
            WHERE BookCopyId = @BookCopyId AND StatusId IN (@Active, @Overdue) AND IsDeleted = 0;
            """,
            ("@BookCopyId", bookCopyId),
            ("@Active", LoanStatusIds.Active),
            ("@Overdue", LoanStatusIds.Overdue));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private async Task<long> AddLoanAsync(
        long memberId,
        long bookCopyId,
        byte statusId,
        DateOnly loanDate,
        DateOnly dueDate,
        DateOnly? returnDate,
        bool borrowCopy)
    {
        var loanId = await ScalarAsync<long>(
            """
            INSERT INTO dbo.Loans (BookCopyId, MemberId, StatusId, LoanDate, DueDate, ReturnDate)
            OUTPUT INSERTED.Id
            VALUES (@BookCopyId, @MemberId, @StatusId, @LoanDate, @DueDate, @ReturnDate);
            """,
            ("@BookCopyId", bookCopyId),
            ("@MemberId", memberId),
            ("@StatusId", statusId),
            ("@LoanDate", loanDate),
            ("@DueDate", dueDate),
            ("@ReturnDate", returnDate));

        if (borrowCopy)
        {
            await ExecuteAsync(
                "UPDATE dbo.BookCopies SET StatusId = @Borrowed WHERE Id = @Id;",
                ("@Borrowed", CopyStatusIds.Borrowed),
                ("@Id", bookCopyId));
        }

        return loanId;
    }

    private int Next()
    {
        return ++_sequence;
    }

    private async Task<T> ScalarAsync<T>(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, sql, parameters);

        var result = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T));
    }

    private async Task ExecuteAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, sql, parameters);

        await command.ExecuteNonQueryAsync();
    }

    private static SqlCommand CreateCommand(
        SqlConnection connection,
        string sql,
        (string Name, object? Value)[] parameters)
    {
        var command = new SqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value switch
            {
                null => DBNull.Value,
                DateOnly date => date.ToDateTime(TimeOnly.MinValue),
                _ => value
            });
        }
        return command;
    }
}
