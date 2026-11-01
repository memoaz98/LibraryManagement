using FluentAssertions;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Persistence;
using LibraryManagement.Infrastructure.Persistence.Repositories.Ef;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibraryManagement.Infrastructure.UnitTests.Persistence.Repositories;

public class EfRepositoryBaseTests
{
    private readonly LibraryDbContext _db;
    private readonly EfCategoryRepository _repo;
    private readonly EfUnitOfWork _uow;

    public EfRepositoryBaseTests()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new LibraryDbContext(options);
        _repo = new EfCategoryRepository(_db);
        _uow = new EfUnitOfWork(_db);
    }

    [Fact]
    public async Task AddAsync_ThenSaveChanges_AssignsGeneratedIdToDomainEntity()
    {
        // Arrange
        var category = Category.Create("Fiction");

        // Act
        await _repo.AddAsync(category);
        await _uow.SaveChangesAsync();

        // Assert
        category.Id.Should().BePositive();
        var stored = await _repo.GetByIdAsync(category.Id);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("Fiction");
    }

    [Fact]
    public async Task AddAsync_TwoEntitiesInOneSave_AssignsDistinctIds()
    {
        // Arrange
        var fiction = Category.Create("Fiction");
        var history = Category.Create("History");

        // Act
        await _repo.AddAsync(fiction);
        await _repo.AddAsync(history);
        await _uow.SaveChangesAsync();

        // Assert
        fiction.Id.Should().BePositive();
        history.Id.Should().BePositive();
        fiction.Id.Should().NotBe(history.Id);
    }

    [Fact]
    public async Task AddAsync_BeforeSaveChanges_LeavesIdUnassigned()
    {
        // Arrange
        var category = Category.Create("Fiction");

        // Act
        await _repo.AddAsync(category);

        // Assert
        category.Id.Should().Be(0);
    }
}
