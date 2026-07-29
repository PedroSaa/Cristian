using DocFlow.Application.Admin.Usuarios.Firma.Queries.GetFirmaImagen;
using DocFlow.Application.Admin.Usuarios.Firma.Queries.GetFirmaUsuario;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Usuarios.Firma;

public class GetFirmaUsuarioHandlerTests
{
    private readonly Mock<IFirmaUsuarioRepository> _repoMock = new();
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static byte[] PngBytes() => [0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4];

    [Fact]
    public async Task Metadata_Should_Report_TieneFirma_False_When_None()
    {
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FirmaUsuario?)null);
        var handler = new GetFirmaUsuarioHandler(_repoMock.Object);

        var result = await handler.Handle(new GetFirmaUsuarioQuery(UsuarioId), CancellationToken.None);

        result.TieneFirma.Should().BeFalse();
        result.TieneClave.Should().BeFalse();
        result.Tamano.Should().Be(0);
        result.CreadoEn.Should().BeNull();
    }

    [Fact]
    public async Task Metadata_Should_Report_Details_When_Exists()
    {
        var firma = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, PngBytes(), "image/png", "cifrada", "JJP");
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(firma);
        var handler = new GetFirmaUsuarioHandler(_repoMock.Object);

        var result = await handler.Handle(new GetFirmaUsuarioQuery(UsuarioId), CancellationToken.None);

        result.TieneFirma.Should().BeTrue();
        result.TieneClave.Should().BeTrue();
        result.Sigla.Should().Be("JJP");
        result.ContentType.Should().Be("image/png");
        result.Tamano.Should().Be(PngBytes().LongLength);
    }
}

public class GetFirmaImagenHandlerTests
{
    private readonly Mock<IFirmaUsuarioRepository> _repoMock = new();
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static byte[] PngBytes() => [0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4];

    [Fact]
    public async Task Should_Return_Bytes_And_ContentType()
    {
        var firma = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, PngBytes(), "image/png");
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(firma);
        var handler = new GetFirmaImagenHandler(_repoMock.Object);

        var result = await handler.Handle(new GetFirmaImagenQuery(UsuarioId), CancellationToken.None);

        result.Contenido.Should().BeEquivalentTo(PngBytes());
        result.ContentType.Should().Be("image/png");
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_No_Signature()
    {
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FirmaUsuario?)null);
        var handler = new GetFirmaImagenHandler(_repoMock.Object);

        var act = async () => await handler.Handle(new GetFirmaImagenQuery(UsuarioId), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class EliminarFirmaUsuarioHandlerTests
{
    private readonly Mock<IFirmaUsuarioRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<DocFlow.Application.Common.Interfaces.ICurrentUser> _currentUserMock = new();
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static byte[] PngBytes() => [0x89, 0x50, 0x4E, 0x47, 1, 2];

    private DocFlow.Application.Admin.Usuarios.Firma.Commands.EliminarFirmaUsuario.EliminarFirmaUsuarioHandler Build()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns(Guid.NewGuid());
        return new DocFlow.Application.Admin.Usuarios.Firma.Commands.EliminarFirmaUsuario.EliminarFirmaUsuarioHandler(
            _repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Should_Delete_And_Audit()
    {
        var firma = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, PngBytes(), "image/png");
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(firma);
        var handler = Build();

        await handler.Handle(
            new DocFlow.Application.Admin.Usuarios.Firma.Commands.EliminarFirmaUsuario.EliminarFirmaUsuarioCommand(UsuarioId),
            CancellationToken.None);

        _repoMock.Verify(r => r.DeleteAsync(UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(
            It.Is<RegistroAuditoria>(r => r.Accion == "FirmaUsuarioEliminada")), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_None()
    {
        _repoMock.Setup(r => r.GetByUsuarioAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FirmaUsuario?)null);
        var handler = Build();

        var act = async () => await handler.Handle(
            new DocFlow.Application.Admin.Usuarios.Firma.Commands.EliminarFirmaUsuario.EliminarFirmaUsuarioCommand(UsuarioId),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
