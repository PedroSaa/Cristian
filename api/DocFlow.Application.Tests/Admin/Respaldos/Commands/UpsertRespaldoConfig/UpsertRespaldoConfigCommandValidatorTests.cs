using DocFlow.Application.Admin.Respaldos.Commands.UpsertRespaldoConfig;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Respaldos.Commands.UpsertRespaldoConfig;

public class UpsertRespaldoConfigCommandValidatorTests
{
    private readonly UpsertRespaldoConfigCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_IntervaloMinutos_Is_Zero()
    {
        var cmd = new UpsertRespaldoConfigCommand(
            IntervaloMinutos: 0,
            Habilitado: true,
            MaxBackupCount: 10,
            RetentionDays: 30,
            OutputPath: "./Respaldos",
            TimeoutMinutos: 30);

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.IntervaloMinutos);
    }

    [Fact]
    public void Should_Have_Error_When_MaxBackupCount_Is_Negative()
    {
        var cmd = new UpsertRespaldoConfigCommand(
            IntervaloMinutos: 60,
            Habilitado: true,
            MaxBackupCount: -1,
            RetentionDays: 30,
            OutputPath: "./Respaldos",
            TimeoutMinutos: 30);

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MaxBackupCount);
    }

    [Fact]
    public void Should_Have_Error_When_RetentionDays_Is_Negative()
    {
        var cmd = new UpsertRespaldoConfigCommand(
            IntervaloMinutos: 60,
            Habilitado: true,
            MaxBackupCount: 10,
            RetentionDays: -1,
            OutputPath: "./Respaldos",
            TimeoutMinutos: 30);

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.RetentionDays);
    }

    [Fact]
    public void Should_Have_Error_When_TimeoutMinutos_Is_Zero()
    {
        var cmd = new UpsertRespaldoConfigCommand(
            IntervaloMinutos: 60,
            Habilitado: true,
            MaxBackupCount: 10,
            RetentionDays: 30,
            OutputPath: "./Respaldos",
            TimeoutMinutos: 0);

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.TimeoutMinutos);
    }

    [Fact]
    public void Should_Have_Error_When_OutputPath_Is_Empty()
    {
        var cmd = new UpsertRespaldoConfigCommand(
            IntervaloMinutos: 60,
            Habilitado: true,
            MaxBackupCount: 10,
            RetentionDays: 30,
            OutputPath: "",
            TimeoutMinutos: 30);

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.OutputPath);
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Valid()
    {
        var cmd = new UpsertRespaldoConfigCommand(
            IntervaloMinutos: 60,
            Habilitado: true,
            MaxBackupCount: 10,
            RetentionDays: 30,
            OutputPath: "./Respaldos",
            TimeoutMinutos: 30);

        var result = _validator.TestValidate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
