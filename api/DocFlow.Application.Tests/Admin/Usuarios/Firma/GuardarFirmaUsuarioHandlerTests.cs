using DocFlow.Application.Admin.Usuarios.Firma.Commands.GuardarFirmaUsuario;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Usuarios.Firma;

public class GuardarFirmaUsuarioHandlerTests
{
    private readonly Mock<IFirmaUsuarioRepository> _repoMock = new();
    private readonly Mock<IFirmaClaveProtector> _protectorMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GuardarFirmaUsuarioHandler _handler;

    private static readonly Guid AuditorId = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static byte[] PngBytes() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    public GuardarFirmaUsuarioHandlerTests()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns(AuditorId);
        // The protector is deterministic in tests so we can assert what gets persisted.
        _protectorMock.Setup(p => p.Protect(It.IsAny<string>())).Returns((string s) => $"ENC({s})");
        _handler = new GuardarFirmaUsuarioHandler(
            _repoMock.Object, _protectorMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Should_Create_When_No_Signature_Exists()
    {
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FirmaUsuario?)null);

        var cmd = new GuardarFirmaUsuarioCommand(UsuarioId, PngBytes(), "image/png", null, "JJP");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.TieneFirma.Should().BeTrue();
        result.Sigla.Should().Be("JJP");
        result.ContentType.Should().Be("image/png");
        _repoMock.Verify(r => r.UpsertAsync(
            It.Is<FirmaUsuario>(f => f.UsuarioId == UsuarioId), It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(
            It.Is<RegistroAuditoria>(r => r.Accion == "FirmaUsuarioCreada")), Times.Once);
    }

    [Fact]
    public async Task Should_Replace_When_Signature_Exists()
    {
        var existente = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, PngBytes(), "image/png", null, "OLD");
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        var nueva = new byte[] { 0xFF, 0xD8, 0xFF, 9, 9, 9 };
        var cmd = new GuardarFirmaUsuarioCommand(UsuarioId, nueva, "image/jpeg", null, "NEW");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Sigla.Should().Be("NEW");
        result.ContentType.Should().Be("image/jpeg");
        // Same aggregate is updated, not a second one created.
        _repoMock.Verify(r => r.UpsertAsync(existente, It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(
            It.Is<RegistroAuditoria>(r => r.Accion == "FirmaUsuarioActualizada")), Times.Once);
    }

    [Fact]
    public async Task Should_Encrypt_Clave_And_Never_Store_Plaintext()
    {
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FirmaUsuario?)null);

        FirmaUsuario? persisted = null;
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<FirmaUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<FirmaUsuario, CancellationToken>((f, _) => persisted = f);

        const string clavePlano = "1234-secreto";
        var cmd = new GuardarFirmaUsuarioCommand(UsuarioId, PngBytes(), "image/png", clavePlano, null);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        // The stored value is the protector's output, never the raw plaintext handed to the command.
        _protectorMock.Verify(p => p.Protect(clavePlano), Times.Once);
        persisted.Should().NotBeNull();
        persisted!.ClaveCifrada.Should().Be($"ENC({clavePlano})");
        persisted.ClaveCifrada.Should().NotBe(clavePlano);
        result.TieneClave.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Not_Call_Protector_When_No_Clave()
    {
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FirmaUsuario?)null);

        FirmaUsuario? persisted = null;
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<FirmaUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<FirmaUsuario, CancellationToken>((f, _) => persisted = f);

        var cmd = new GuardarFirmaUsuarioCommand(UsuarioId, PngBytes(), "image/png");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        _protectorMock.Verify(p => p.Protect(It.IsAny<string>()), Times.Never);
        persisted!.ClaveCifrada.Should().BeNull();
        result.TieneClave.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Fail_When_Content_Does_Not_Match_Declared_Type()
    {
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FirmaUsuario?)null);

        // JPEG magic bytes declared as PNG.
        var cmd = new GuardarFirmaUsuarioCommand(UsuarioId, new byte[] { 0xFF, 0xD8, 0xFF }, "image/png");

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*no corresponde*");
        _repoMock.Verify(r => r.UpsertAsync(It.IsAny<FirmaUsuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Fail_When_Creating_Without_Image()
    {
        // Creation (no existing signature) with no image supplied is rejected.
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FirmaUsuario?)null);

        var cmd = new GuardarFirmaUsuarioCommand(UsuarioId, null, null, null, "JJP");

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*obligatoria*");
        _repoMock.Verify(r => r.UpsertAsync(It.IsAny<FirmaUsuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Keep_Existing_Image_When_None_Provided()
    {
        var imagenExistente = PngBytes();
        var existente = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, imagenExistente, "image/png", null, "OLD");
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        FirmaUsuario? persisted = null;
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<FirmaUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<FirmaUsuario, CancellationToken>((f, _) => persisted = f);

        // No image, no content type: only the sigla changes.
        var cmd = new GuardarFirmaUsuarioCommand(UsuarioId, null, null, null, "NEW");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        persisted.Should().NotBeNull();
        persisted!.ImagenFirma.Should().BeEquivalentTo(imagenExistente);
        persisted.ContentType.Should().Be("image/png");
        result.Sigla.Should().Be("NEW");
        result.ContentType.Should().Be("image/png");
    }

    [Fact]
    public async Task Should_Keep_Existing_Clave_When_None_Provided()
    {
        // Existing signature already has an encrypted PIN; the command omits the clave.
        var existente = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, PngBytes(), "image/png", "ENC(old)", "S");
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        FirmaUsuario? persisted = null;
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<FirmaUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<FirmaUsuario, CancellationToken>((f, _) => persisted = f);

        var cmd = new GuardarFirmaUsuarioCommand(UsuarioId, null, null, null, "S");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        // The existing encrypted PIN is preserved, NOT cleared, and the protector is never invoked.
        _protectorMock.Verify(p => p.Protect(It.IsAny<string>()), Times.Never);
        persisted.Should().NotBeNull();
        persisted!.ClaveCifrada.Should().NotBeNull();
        persisted.ClaveCifrada.Should().Be("ENC(old)");
        result.TieneClave.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Replace_Clave_When_New_Provided()
    {
        var existente = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, PngBytes(), "image/png", "ENC(old)", "S");
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        FirmaUsuario? persisted = null;
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<FirmaUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<FirmaUsuario, CancellationToken>((f, _) => persisted = f);

        var cmd = new GuardarFirmaUsuarioCommand(UsuarioId, null, null, "clave-nueva", "S");

        await _handler.Handle(cmd, CancellationToken.None);

        _protectorMock.Verify(p => p.Protect("clave-nueva"), Times.Once);
        persisted.Should().NotBeNull();
        persisted!.ClaveCifrada.Should().Be("ENC(clave-nueva)");
        persisted.ClaveCifrada.Should().NotBe("ENC(old)");
    }

    [Fact]
    public async Task Should_Update_Only_Sigla_And_Keep_Image_And_Clave()
    {
        var imagenExistente = PngBytes();
        var existente = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, imagenExistente, "image/png", "ENC(old)", "OLD");
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        FirmaUsuario? persisted = null;
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<FirmaUsuario>(), It.IsAny<CancellationToken>()))
            .Callback<FirmaUsuario, CancellationToken>((f, _) => persisted = f);

        // Only sigla is sent: image and clave stay intact.
        var cmd = new GuardarFirmaUsuarioCommand(UsuarioId, null, null, null, "NUEVA");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        _protectorMock.Verify(p => p.Protect(It.IsAny<string>()), Times.Never);
        persisted.Should().NotBeNull();
        persisted!.ImagenFirma.Should().BeEquivalentTo(imagenExistente);
        persisted.ContentType.Should().Be("image/png");
        persisted.ClaveCifrada.Should().Be("ENC(old)");
        persisted.Sigla.Should().Be("NUEVA");
        result.Sigla.Should().Be("NUEVA");
        result.TieneClave.Should().BeTrue();
    }
}

public class GuardarFirmaUsuarioValidatorTests
{
    private readonly GuardarFirmaUsuarioValidator _validator = new();
    private static byte[] PngBytes() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2];

    [Fact]
    public void Should_Pass_For_Valid_Png()
    {
        var cmd = new GuardarFirmaUsuarioCommand(Guid.NewGuid(), PngBytes(), "image/png", null, "AB");

        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ContentType_Not_Image()
    {
        var cmd = new GuardarFirmaUsuarioCommand(Guid.NewGuid(), PngBytes(), "application/pdf");

        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_No_Image()
    {
        // Image is optional now (partial update); whether creation requires it is enforced in the handler.
        var cmd = new GuardarFirmaUsuarioCommand(Guid.NewGuid(), null, null, null, "AB");

        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_Image_Present_But_ContentType_Missing()
    {
        var cmd = new GuardarFirmaUsuarioCommand(Guid.NewGuid(), PngBytes(), null);

        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_Image_Exceeds_Max()
    {
        var big = new byte[Application.Admin.Usuarios.Firma.FirmaImagenValidation.MaxImageBytes + 1];
        var cmd = new GuardarFirmaUsuarioCommand(Guid.NewGuid(), big, "image/png");

        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_Sigla_Too_Long()
    {
        var cmd = new GuardarFirmaUsuarioCommand(
            Guid.NewGuid(), PngBytes(), "image/png", null, new string('x', FirmaUsuario.SiglaMaxLength + 1));

        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}
