using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using LibraryManagement.Application.Common.Exceptions;
using LibraryManagement.Application.Dtos.Authors;
using LibraryManagement.Application.Services;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using LibraryManagement.Domain.Interfaces;
using Moq;
using Xunit;

namespace LibraryManagement.Application.UnitTests.Services;

public class AuthorServiceTest{

    private readonly Mock<IAuthorRepository> _authorRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IValidator<CreateAuthorDto>> _createValidator;
    private readonly Mock<IValidator<UpdateAuthorDto>> _updateValidator;
    private readonly AuthorService _service;

    public AuthorServiceTest()
    {
        _authorRepo = new Mock<IAuthorRepository>();
        _uow = new Mock<IUnitOfWork>();
        _createValidator = new Mock<IValidator<CreateAuthorDto>>();
        _updateValidator = new Mock<IValidator<UpdateAuthorDto>>();

        // Default: validators pass (no errors)
        _createValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateAuthorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateAuthorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _service = new AuthorService(
            _authorRepo.Object,
            _uow.Object,
            _createValidator.Object,
            _updateValidator.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsAuthorAndSavesChanges()
    {
        //Arrange
        var dto = new CreateAuthorDto("John", "Doe", new DateOnly(1980, 1, 1), "Biography");

        //Act
        var result = await _service.CreateAsync(dto, CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        
        //AddAsync,SaveChangesAsync,AuthorToDto
        _authorRepo.Verify(r => r.AddAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

    }
}