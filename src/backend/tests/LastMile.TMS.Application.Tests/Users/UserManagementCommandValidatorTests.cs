using FluentAssertions;
using LastMile.TMS.Application.Users.Commands;
using LastMile.TMS.Application.Users.Queries;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Tests.Users;

public class UserManagementCommandValidatorTests
{
    [Fact]
    public void CreateUserCommandValidator_ShouldRejectMissingRequiredFields()
    {
        var validator = new CreateUserCommandValidator();
        var command = new CreateUserCommand(
            "",
            "",
            "not-an-email",
            new string('1', 25),
            PredefinedRole.Dispatcher,
            null,
            null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateUserCommand.FirstName));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateUserCommand.LastName));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateUserCommand.Email));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateUserCommand.Phone));
    }

    [Fact]
    public void UpdateUserCommandValidator_ShouldRequireUserId()
    {
        var validator = new UpdateUserCommandValidator();
        var command = new UpdateUserCommand(
            Guid.Empty,
            "Taylor",
            "Updater",
            "taylor@example.com",
            "+10000000003",
            PredefinedRole.OperationsManager,
            null,
            null,
            true);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateUserCommand.Id));
    }

    [Fact]
    public void DeactivateUserCommandValidator_ShouldRequireUserId()
    {
        var validator = new DeactivateUserCommandValidator();
        var command = new DeactivateUserCommand(Guid.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(DeactivateUserCommand.UserId));
    }

    [Fact]
    public void RequestPasswordResetCommandValidator_ShouldRequireValidEmail()
    {
        var validator = new RequestPasswordResetCommandValidator();
        var command = new RequestPasswordResetCommand("not-an-email");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(RequestPasswordResetCommand.Email));
    }

    [Fact]
    public void GetUsersQueryValidator_ShouldRejectInvalidPaging()
    {
        var validator = new GetUsersQueryValidator();
        var query = new GetUsersQuery(
            Search: "dispatch",
            Role: PredefinedRole.Dispatcher,
            IsActive: true,
            DepotId: null,
            ZoneId: null,
            Skip: -1,
            Take: 101);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(GetUsersQuery.Skip));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(GetUsersQuery.Take));
    }

    [Fact]
    public void GetUsersQueryValidator_ShouldAcceptValidPaging()
    {
        var validator = new GetUsersQueryValidator();
        var query = new GetUsersQuery(
            Search: "alex",
            Role: null,
            IsActive: null,
            DepotId: null,
            ZoneId: null,
            Skip: 0,
            Take: 25);

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CompletePasswordResetCommandValidator_ShouldRejectWeakPasswords()
    {
        var validator = new CompletePasswordResetCommandValidator();
        var command = new CompletePasswordResetCommand(
            "person@example.com",
            "token-value",
            "weakpass");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CompletePasswordResetCommand.NewPassword));
    }

    [Fact]
    public void CompletePasswordResetCommandValidator_ShouldAcceptValidPasswords()
    {
        var validator = new CompletePasswordResetCommandValidator();
        var command = new CompletePasswordResetCommand(
            "person@example.com",
            "token-value",
            "ValidPass1");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
