using DocFlow.Application.Common;
using FluentAssertions;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class PasswordPolicyValidatorTests
{
    [Theory]
    [InlineData("Secure@123", true)]    // Meets all requirements
    [InlineData("Abcd1234!", true)]     // Meets all requirements
    [InlineData("Str0ng#Pass", true)]   // Meets all requirements
    [InlineData("short", false)]        // Too short
    [InlineData("nouppercase1@", false)] // No uppercase
    [InlineData("NOLOWERCASE1@", false)] // No lowercase
    [InlineData("NoDigits!@", false)]   // No digit
    [InlineData("NoSpecial1", false)]   // No special char (need at least one of !@#$%^&*)
    [InlineData("", false)]             // Empty
    [InlineData("12345678", false)]     // Only digits
    [InlineData("abcdefgh", false)]     // Only lowercase
    [InlineData("ABCDEFGH", false)]     // Only uppercase
    [InlineData("Secure@", false)]      // No digit
    public void ValidatePassword_WithVariousInputs_ReturnsExpectedResult(string password, bool expectedValid)
    {
        var result = PasswordPolicyValidator.Validate(password);
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void ValidatePassword_WithWeakPassword_ReturnsErrorMessages()
    {
        var result = PasswordPolicyValidator.Validate("weak");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidatePassword_WithStrongPassword_ReturnsEmptyErrors()
    {
        var result = PasswordPolicyValidator.Validate("Secure@123");

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // --- Config-driven overload tests (REQ-11) ---

    [Theory]
    [InlineData("abcdef123456", 12, true, true, false)]  // 12 chars ok, but no uppercase
    [InlineData("abcdef123456", 12, false, false, true)] // 12 chars, no upper/special required → passes
    [InlineData("Abcdef123456!", 6, true, true, true)]   // 6+ chars meets all
    [InlineData("abc123!", 8, true, true, false)]        // Too short for minLength=8
    [InlineData("abcdefgh123!", 10, true, false, false)] // Min 10, requires upper → fails
    [InlineData("Abcdefgh123", 8, true, false, true)]    // Meets min 8, has upper, has digit, no special needed → passes
    public void ValidatePassword_WithConfigDrivenRules_ReturnsExpectedResult(
        string password, int minLength, bool requireUpper, bool requireSpecial, bool expectedValid)
    {
        var result = PasswordPolicyValidator.Validate(password, minLength, requireUpper, requireSpecial);
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void ValidatePassword_WithConfigDrivenMinLength12_RejectsShortWithCorrectMessage()
    {
        var result = PasswordPolicyValidator.Validate("short1A!", minLength: 12, requireUpper: true, requireSpecial: true);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("12"));
    }

    [Fact]
    public void ValidatePassword_WithConfigDrivenRelaxedRules_NoUppercaseError()
    {
        var result = PasswordPolicyValidator.Validate("abcdef123456", minLength: 12, requireUpper: false, requireSpecial: false);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidatePassword_WithConfigDrivenRequireSpecialTrue_RejectsNoSpecial()
    {
        var result = PasswordPolicyValidator.Validate("Abcdefgh12345", minLength: 6, requireUpper: true, requireSpecial: true);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("especial"));
    }
}
