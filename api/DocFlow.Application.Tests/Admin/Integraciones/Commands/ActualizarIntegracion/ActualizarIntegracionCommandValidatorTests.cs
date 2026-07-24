using DocFlow.Application.Admin.Integraciones.Commands.ActualizarIntegracion;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Integraciones.Commands.ActualizarIntegracion;

public class ActualizarIntegracionCommandValidatorTests
{
    private readonly ActualizarIntegracionCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Command()
    {
        var cmd = new ActualizarIntegracionCommand(Guid.NewGuid(), "https://api.test.com", "key", true);
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_BaseUrl_Empty()
    {
        var cmd = new ActualizarIntegracionCommand(Guid.NewGuid(), "", "key", true);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.BaseUrl);
    }

    [Fact]
    public void Should_Pass_When_Settings_Null()
    {
        var cmd = new ActualizarIntegracionCommand(Guid.NewGuid(), "https://api.test.com", "key", true, null);
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Valid_Settings()
    {
        var cmd = new ActualizarIntegracionCommand(Guid.NewGuid(), "https://api.test.com", "key", true,
            new Dictionary<string, string>
            {
                ["SystemUserEmail"] = "sistema@docflow.cl",
                ["PollingIntervalMinutes"] = "15",
            });
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Invalid_SystemUserEmail()
    {
        var cmd = new ActualizarIntegracionCommand(Guid.NewGuid(), "https://api.test.com", "key", true,
            new Dictionary<string, string> { ["SystemUserEmail"] = "no-es-email" });
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("SystemUserEmail");
    }

    [Fact]
    public void Should_Reject_NonNumeric_PollingInterval()
    {
        var cmd = new ActualizarIntegracionCommand(Guid.NewGuid(), "https://api.test.com", "key", true,
            new Dictionary<string, string> { ["PollingIntervalMinutes"] = "abc" });
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("PollingIntervalMinutes");
    }

    [Fact]
    public void Should_Reject_PollingInterval_Out_Of_Range()
    {
        var cmd = new ActualizarIntegracionCommand(Guid.NewGuid(), "https://api.test.com", "key", true,
            new Dictionary<string, string> { ["PollingIntervalMinutes"] = "5000" });
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("PollingIntervalMinutes");
    }
}
