using ChronicleHub.Application.DTOs.Auth;
using ChronicleHub.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ChronicleHub.Application.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "user@example.com",
            Password: "SecurePass123",
            FirstName: "John",
            LastName: "Doe",
            TenantName: "Acme Corp"
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
        var request = new RegisterRequest(
            Email: email!,
            Password: "SecurePass123",
            FirstName: "John",
            LastName: "Doe",
            TenantName: "Acme Corp"
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
        var request = new RegisterRequest(
            Email: email,
            Password: "SecurePass123",
            FirstName: "John",
            LastName: "Doe",
            TenantName: "Acme Corp"
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
        var request = new RegisterRequest(
            Email: "user@example.com",
            Password: password!,
            FirstName: "John",
            LastName: "Doe",
            TenantName: "Acme Corp"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Password)
            .WithErrorMessage("Password is required.");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public void Validate_WithShortPassword_ShouldHaveValidationError(string password)
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "user@example.com",
            Password: password,
            FirstName: "John",
            LastName: "Doe",
            TenantName: "Acme Corp"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Password)
            .WithErrorMessage("Password must be at least 8 characters long.");
    }

    [Theory]
    [InlineData("alllowercase123")]
    [InlineData("lowercase123!")]
    public void Validate_WithPasswordMissingUppercase_ShouldHaveValidationError(string password)
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "user@example.com",
            Password: password,
            FirstName: "John",
            LastName: "Doe",
            TenantName: "Acme Corp"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Password)
            .WithErrorMessage("Password must contain at least one uppercase letter.");
    }

    [Theory]
    [InlineData("ALLUPPERCASE123")]
    [InlineData("UPPERCASE123!")]
    public void Validate_WithPasswordMissingLowercase_ShouldHaveValidationError(string password)
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "user@example.com",
            Password: password,
            FirstName: "John",
            LastName: "Doe",
            TenantName: "Acme Corp"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Password)
            .WithErrorMessage("Password must contain at least one lowercase letter.");
    }

    [Theory]
    [InlineData("NoDigitsHere")]
    [InlineData("OnlyLetters!")]
    public void Validate_WithPasswordMissingDigit_ShouldHaveValidationError(string password)
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "user@example.com",
            Password: password,
            FirstName: "John",
            LastName: "Doe",
            TenantName: "Acme Corp"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Password)
            .WithErrorMessage("Password must contain at least one digit.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyFirstName_ShouldHaveValidationError(string? firstName)
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "user@example.com",
            Password: "SecurePass123",
            FirstName: firstName!,
            LastName: "Doe",
            TenantName: "Acme Corp"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.FirstName)
            .WithErrorMessage("First name is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyLastName_ShouldHaveValidationError(string? lastName)
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "user@example.com",
            Password: "SecurePass123",
            FirstName: "John",
            LastName: lastName!,
            TenantName: "Acme Corp"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.LastName)
            .WithErrorMessage("Last name is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyTenantName_ShouldHaveValidationError(string? tenantName)
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "user@example.com",
            Password: "SecurePass123",
            FirstName: "John",
            LastName: "Doe",
            TenantName: tenantName!
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.TenantName)
            .WithErrorMessage("Tenant name is required.");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldHaveAllValidationErrors()
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "invalid-email",
            Password: "weak",
            FirstName: "",
            LastName: "",
            TenantName: ""
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Email);
        result.ShouldHaveValidationErrorFor(r => r.Password);
        result.ShouldHaveValidationErrorFor(r => r.FirstName);
        result.ShouldHaveValidationErrorFor(r => r.LastName);
        result.ShouldHaveValidationErrorFor(r => r.TenantName);
    }

    [Theory]
    [InlineData("ValidPass1")]
    [InlineData("SecurePassword123")]
    [InlineData("MyP@ssw0rd")]
    public void Validate_WithValidPassword_ShouldNotHavePasswordValidationError(string password)
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "user@example.com",
            Password: password,
            FirstName: "John",
            LastName: "Doe",
            TenantName: "Acme Corp"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(r => r.Password);
    }
}
