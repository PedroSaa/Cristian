using DocFlow.Application.Admin.Roles.Commands.EliminarRol;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Roles.Commands.EliminarRol;

public class EliminarRolCommandValidatorTests
{
    private readonly EliminarRolCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Id()
    {
        var cmd = new EliminarRolCommand(Guid.NewGuid());
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Id_Empty()
    {
        var cmd = new EliminarRolCommand(Guid.Empty);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}
