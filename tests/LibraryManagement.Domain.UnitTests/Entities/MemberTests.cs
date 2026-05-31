using FluentAssertions;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Exceptions;
using Xunit;

namespace LibraryManagement.Domain.UnitTests.Entities;

public class MemberTests
{
    [Fact]
    public void Create_StartsActiveAndUnlinked()
    {
        var member = Member.Create("John", "Doe", "john@example.com");

        member.IsActive.Should().BeTrue();
        member.UserId.Should().BeNull();
        member.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_NormalizesEmailToLowercase()
    {
        var member = Member.Create("John", "Doe", "John.Doe@EXAMPLE.COM");

        member.Email.Should().Be("john.doe@example.com");
    }

    [Fact]
    public void Create_WithInvalidEmail_Throws()
    {
        var act = () => Member.Create("John", "Doe", "not-an-email");

        act.Should().Throw<DomainException>()
            .WithMessage("*email*");
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var member = Member.Create("John", "Doe", "john@example.com");

        member.Deactivate();

        member.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_OnAlreadyActive_IsIdempotent()
    {
        var member = Member.Create("John", "Doe", "john@example.com");

        member.Activate();

        member.IsActive.Should().BeTrue();
    }

    [Fact]
    public void LinkToUser_AssignsUserId()
    {
        var member = Member.Create("John", "Doe", "john@example.com");
        var userId = Guid.NewGuid().ToString();

        member.LinkToUser(userId);

        member.UserId.Should().Be(userId);
    }

    [Fact]
    public void LinkToUser_WithEmptyId_Throws()
    {
        var member = Member.Create("John", "Doe", "john@example.com");

        var act = () => member.LinkToUser("");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UnlinkFromUser_ClearsUserId()
    {
        var member = Member.Create("John", "Doe", "john@example.com");
        member.LinkToUser("some-id");

        member.UnlinkFromUser();

        member.UserId.Should().BeNull();
    }
}