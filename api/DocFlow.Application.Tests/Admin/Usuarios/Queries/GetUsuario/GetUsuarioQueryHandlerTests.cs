using DocFlow.Application.Admin.Usuarios.Queries.GetUsuario;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Usuarios.Queries.GetUsuario;

public class GetUsuarioQueryHandlerTests
{
    private readonly Mock<IUsuarioAdminRepository> _repoMock = new();

    [Fact]
    public async Task Handle_RetornaDto_CuandoUsuarioExiste()
    {
        var usuario = CreateUser();
        _repoMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sut = new GetUsuarioQueryHandler(_repoMock.Object);

        var result = await sut.Handle(new GetUsuarioQuery(usuario.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(usuario.Id);
    }

    [Fact]
    public async Task Handle_LanzaKeyNotFound_CuandoUsuarioNoExiste()
    {
        _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeUsuari?)null);

        var sut = new GetUsuarioQueryHandler(_repoMock.Object);

        var act = async () => await sut.Handle(new GetUsuarioQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static SeUsuari CreateUser()
    {
        var personal = SePersonal.Crear("testuser", "Test User", correo: "test@docflow.cl");
        var usuario = SeUsuari.Crear(Guid.NewGuid(), "testuser", "hash", null, null, estadoCuenta: true);
        usuario.VincularPersonal(personal);
        return usuario;
    }
}
