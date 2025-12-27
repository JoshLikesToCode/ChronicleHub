using ChronicleHub.Application.DTOs.Auth;
using ChronicleHub.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ChronicleHub.Application.Tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new LoginRequest(
            Email: "user@example.com",
            Password: "SecurePass123",
            TenantId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidRequestAndNullTenantId_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new LoginRequest(
            Email: "user@example.com",
            Password: "SecurePass123",
            TenantId: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyEmail_ShouldHaveValidationError(string? email)
    {
        // Arrange
        var request = new LoginRequest(
            Email: email!,
            Password: "SecurePass123",
            TenantId: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Email)
            .WithErrorMessage("Email is required.");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user")]
    public void Validate_WithInvalidEmailFormat_ShouldHaveValidationError(string email)
    {
        // Arrange
        var request = new LoginRequest(
            Email: email,
            Password: "SecurePass123",
            TenantId: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Email)
            .WithErrorMessage("Invalid email format.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyPassword_ShouldHaveValidationError(string? password)
    {
        // Arrange
        var request = new LoginRequest(
            Email: "user@example.com",
            Password: password!,
            TenantId: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Password)
            .WithErrorMessage("Password is required.");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldHaveAllValidationErrors()
    {
        // Arrange
        var request = new LoginRequest(
            Email: "invalid-email",
            Password: "",
            TenantId: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Email);
        result.ShouldHaveValidationErrorFor(r => r.Password);
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user+tag@domain.co.uk")]
    [InlineData("name.surname@company.com")]
    public void Validate_WithValidEmail_ShouldNotHaveEmailValidationError(string email)
    {
        // Arrange
        var request = new LoginRequest(
            Email: email,
            Password: "password",
            TenantId: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(r => r.Email);
    }

    [Fact]
    public void Validate_WithEmptyGuidTenantId_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new LoginRequest(
            Email: "user@example.com",
            Password: "password",
            TenantId: Guid.Empty
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(r => r.TenantId);
    }
}
