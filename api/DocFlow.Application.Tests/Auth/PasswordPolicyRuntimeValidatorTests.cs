using DocFlow.Application.Admin.Usuarios.Commands.CrearUsuario;
using DocFlow.Application.Admin.Usuarios.Commands.ResetPasswordUsuario;
using DocFlow.Application.Auth.Commands.ChangePassword;
using DocFlow.Application.Common.Interfaces;
using FluentValidation.TestHelper;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class PasswordPolicyRuntimeValidatorTests
{
    private static Mock<ISecurityPolicyService> CreatePolicyMock(
        int minLength = 8,
        bool requireUpper = false,
        bool requireSpecial = false)
    {
        var mock = new Mock<ISecurityPolicyService>();
        mock.Setup(x => x.GetPasswordMinLength()).Returns(minLength);
        mock.Setup(x => x.GetPasswordRequireUpper()).Returns(requireUpper);
        mock.Setup(x => x.GetPasswordRequireSpecial()).Returns(requireSpecial);
        return mock;
    }

    [Fact]
    public void CrearUsuarioValidator_Uses_Runtime_Policy()
    {
        var validator = new CrearUsuarioCommandValidator(CreatePolicyMock().Object);

        var result = validator.TestValidate(new CrearUsuarioCommand(
            "Juan",
            "Pérez",
            "Gómez",
            null,
            null,
            "juan@docflow.cl",
            "Administrador",
            null,
            "abc12345"));

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ResetPasswordValidator_Uses_Runtime_Policy()
    {
        var validator = new ResetPasswordUsuarioCommandValidator(CreatePolicyMock().Object);

        var result = validator.TestValidate(new ResetPasswordUsuarioCommand(Guid.NewGuid(), "abc12345"));

        result.ShouldNotHaveValidationErrorFor(x => x.NuevaPassword);
    }

    [Fact]
    public void ChangePasswordValidator_Uses_Runtime_Policy()
    {
        var validator = new ChangePasswordCommandValidator(CreatePolicyMock().Object);

        var result = validator.TestValidate(new ChangePasswordCommand("old-password", "abc12345"));

        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }
}
