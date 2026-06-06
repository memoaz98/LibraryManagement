using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using LibraryManagement.Application.Common.Exceptions;
using LibraryManagement.Application.Dtos.Categories;
using LibraryManagement.Application.Services;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using LibraryManagement.Domain.Interfaces;
using Moq;
using Xunit;

namespace LibraryManagement.Application.UnitTests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IValidator<CreateCategoryDto>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateCategoryDto>> _updateValidatorMock;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _repoMock = new Mock<ICategoryRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _createValidatorMock = new Mock<IValidator<CreateCategoryDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateCategoryDto>>();

        // Default: validators pass (no errors)
        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateCategoryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateCategoryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _service = new CategoryService(
            _repoMock.Object,
            _uowMock.Object,
            _createValidatorMock.Object,
            _updateValidatorMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsCategoryAndSavesChanges()
    {
        // Arrange
        var dto = new CreateCategoryDto("Fiction", "Literary works");

        _repoMock.Setup(r => r.NameExistsAsync("Fiction", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        // Act
        var result = await _service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Fiction");
        result.Description.Should().Be("Literary works");

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsConflictExceptionAndDoesNotSave()
    {
        // Arrange
        var dto = new CreateCategoryDto("Fiction", null);

        _repoMock.Setup(r => r.NameExistsAsync("Fiction", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        // Act + Assert
        await _service.Invoking(s => s.CreateAsync(dto, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("*Fiction*");

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Category?)null);

        // Act + Assert
        await _service.Invoking(s => s.GetByIdAsync(999, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingIdAndSameName_UpdatesDescription()
    {
        // Arrange
        var existing = Category.Create("Fiction", "Old description");
        var dto = new UpdateCategoryDto("Fiction", "New description");

        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);

        // Act
        var result = await _service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        result.Description.Should().Be("New description");

        _repoMock.Verify(r => r.NameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.Update(It.IsAny<Category>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_CallsRemoveAndSaves()
    {
        // Arrange
        var existing = Category.Create("Fiction", null);

        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);

        // Act
        await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.Remove(existing), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}