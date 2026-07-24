using DocFlow.Application.Auth.Commands.Mfa;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class EnableMfaCommandHandlerTests
{
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();

    private EnableMfaCommandHandler CreateSut() => new(_usuarioRepositoryMock.Object, _totpServiceMock.Object, _currentUserMock.Object, _auditoriaMock.Object, AuthUserFactory.PassthroughMfaProtector());

    [Fact]
    public async Task Handle_WithAuthenticatedUser_ReturnsProvisioningUri()
    {
        var userId = Guid.NewGuid();
        var usuario = AuthUserFactory.CreateUser("Test User", "user@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions());
        var secret = "JBSWY3DPEHPK3PXP";
        var provisioningUri = $"otpauth://totp/DocFlow:user@docflow.cl?secret={secret}&issuer=DocFlow";

        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _totpServiceMock.Setup(x => x.GenerateSecret()).Returns(secret);
        _totpServiceMock.Setup(x => x.GenerateProvisioningUri(secret, "user@docflow.cl")).Returns(provisioningUri);

        var sut = CreateSut();

        var result = await sut.Handle(new EnableMfaCommand(), CancellationToken.None);

        result.ProvisioningUri.Should().Be(provisioningUri);
        result.SecretKey.Should().Be(secret);
        usuario.MfaSecretKey.Should().Be(secret);
        usuario.MfaEnabled.Should().BeTrue();
        _usuarioRepositoryMock.Verify(x => x.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.Accion == "MFASetupIniciado" && r.UsuarioId == usuario.UsuarioId)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnauthenticatedUser_ThrowsUnauthorized()
    {
        _currentUserMock.Setup(x => x.UserId).Returns((Guid?)null);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(false);

        var sut = CreateSut();

        var act = () => sut.Handle(new EnableMfaCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_PersistsEncryptedSecret_ButReturnsPlaintextForQr()
    {
        var userId = Guid.NewGuid();
        var usuario = AuthUserFactory.CreateUser("Test User", "user@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions());
        var secret = "JBSWY3DPEHPK3PXP";

        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _totpServiceMock.Setup(x => x.GenerateSecret()).Returns(secret);
        _totpServiceMock.Setup(x => x.GenerateProvisioningUri(secret, "user@docflow.cl")).Returns("otpauth://totp/x");

        var protectorMock = new Mock<IMfaSecretProtector>();
        protectorMock.Setup(p => p.Protect(secret)).Returns("ENC:" + secret);
        var sut = new EnableMfaCommandHandler(_usuarioRepositoryMock.Object, _totpServiceMock.Object, _currentUserMock.Object, _auditoriaMock.Object, protectorMock.Object);

        var result = await sut.Handle(new EnableMfaCommand(), CancellationToken.None);

        usuario.MfaSecretKey.Should().Be("ENC:" + secret);   // se persiste cifrado
        result.SecretKey.Should().Be(secret);                // el QR/provisioning recibe el plano
        protectorMock.Verify(p => p.Protect(secret), Times.Once);
    }
}

public class VerifyMfaCommandHandlerTests
{
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();

    private VerifyMfaCommandHandler CreateSut() => new(_usuarioRepositoryMock.Object, _totpServiceMock.Object, _currentUserMock.Object, _mediatorMock.Object, AuthUserFactory.PassthroughMfaProtector());

    [Fact]
    public async Task Handle_WithValidCode_EnablesMfaAndReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var secret = "JBSWY3DPEHPK3PXP";
        var usuario = AuthUserFactory.CreateUser("Test User", "user@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions());
        usuario.EstablecerMfa(secret);

        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _totpServiceMock.Setup(x => x.ValidateCode(secret, "123456")).Returns(true);

        var sut = CreateSut();

        var result = await sut.Handle(new VerifyMfaCommand("123456"), CancellationToken.None);

        result.Success.Should().BeTrue();
        usuario.MfaEnabled.Should().BeTrue();
        _usuarioRepositoryMock.Verify(x => x.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidCode_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var secret = "JBSWY3DPEHPK3PXP";
        var usuario = AuthUserFactory.CreateUser("Test User", "user@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions());
        usuario.EstablecerMfa(secret);

        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _totpServiceMock.Setup(x => x.ValidateCode(secret, "000000")).Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(new VerifyMfaCommand("000000"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Código de verificación inválido.");
    }

    [Fact]
    public async Task Handle_DecryptsStoredSecretBeforeValidating()
    {
        var userId = Guid.NewGuid();
        var usuario = AuthUserFactory.CreateUser("Test User", "user@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions());
        usuario.EstablecerMfa("ENC:JBSWY3DPEHPK3PXP"); // almacenado cifrado

        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var protectorMock = new Mock<IMfaSecretProtector>();
        protectorMock.Setup(p => p.Unprotect("ENC:JBSWY3DPEHPK3PXP")).Returns("JBSWY3DPEHPK3PXP");
        // El validador TOTP debe recibir el secreto EN CLARO, no el cifrado.
        _totpServiceMock.Setup(x => x.ValidateCode("JBSWY3DPEHPK3PXP", "123456")).Returns(true);
        var sut = new VerifyMfaCommandHandler(_usuarioRepositoryMock.Object, _totpServiceMock.Object, _currentUserMock.Object, _mediatorMock.Object, protectorMock.Object);

        var result = await sut.Handle(new VerifyMfaCommand("123456"), CancellationToken.None);

        result.Success.Should().BeTrue();
        _totpServiceMock.Verify(x => x.ValidateCode("JBSWY3DPEHPK3PXP", "123456"), Times.Once);
        protectorMock.Verify(p => p.Unprotect("ENC:JBSWY3DPEHPK3PXP"), Times.Once);
    }
}

public class DisableMfaCommandHandlerTests
{
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = MfaTestHelpers.CreateValidHasher();
    private readonly Mock<IMediator> _mediatorMock = new();

    private DisableMfaCommandHandler CreateSut() => new(_usuarioRepositoryMock.Object, _currentUserMock.Object, _passwordHasherMock.Object, _mediatorMock.Object);

    [Fact]
    public async Task Handle_WithAuthenticatedUser_DisablesMfa()
    {
        var userId = Guid.NewGuid();
        var usuario = AuthUserFactory.CreateUser("Test User", "user@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), mfaEnabled: true, passwordHash: "CurrentPass1!");

        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sut = CreateSut();

        await sut.Handle(new DisableMfaCommand(MfaTestHelpers.ValidPassword), CancellationToken.None);

        usuario.MfaEnabled.Should().BeFalse();
        usuario.MfaSecretKey.Should().BeNull();
        _usuarioRepositoryMock.Verify(x => x.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsInvalidOperation()
    {
        var userId = Guid.NewGuid();
        var usuario = AuthUserFactory.CreateUser("Test User", "user@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), mfaEnabled: true, passwordHash: "CurrentPass1!");

        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sut = CreateSut();

        var act = () => sut.Handle(new DisableMfaCommand(MfaTestHelpers.WrongPassword), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Contraseña*");
    }
}

internal static class MfaTestHelpers
{
    public const string ValidPassword = "CurrentPass1!";
    public const string WrongPassword = "WrongPass1!";

    public static Mock<IPasswordHasher> CreateValidHasher()
    {
        var mock = new Mock<IPasswordHasher>();
        mock.Setup(x => x.Verify(ValidPassword, It.IsAny<string>())).Returns(true);
        mock.Setup(x => x.Verify(It.IsNotIn(ValidPassword), It.IsAny<string>())).Returns(false);
        return mock;
    }
}
