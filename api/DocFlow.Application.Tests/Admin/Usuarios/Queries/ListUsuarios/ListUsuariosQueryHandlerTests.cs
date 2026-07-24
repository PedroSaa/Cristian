using System.Reflection;
using DocFlow.Application.Admin.Usuarios.Queries.ListUsuarios;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Usuarios.Queries.ListUsuarios;

public class ListUsuariosQueryHandlerTests
{
    private readonly Mock<IUsuarioAdminRepository> _repoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ListUsuariosQueryHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public ListUsuariosQueryHandlerTests()
    {
        _currentUserMock.SetupGet(c => c.UserId).Returns(_currentUserId);
        _repoMock.Setup(x => x.CountActiveAdministratorsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _handler = new ListUsuariosQueryHandler(_repoMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_Maps_Rut_To_Dto()
    {
        var usuario = CreateTestUser();
        var items = new List<SeUsuari> { usuario };

        _repoMock
            .Setup(x => x.GetPaginatedAsync(1, 20, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        var result = await _handler.Handle(new ListUsuariosQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Rut.Should().Be("12.345.678-9");
    }

    [Fact]
    public async Task Handle_Maps_All_Fields_Correctly()
    {
        var id = Guid.NewGuid();
        var deptoId = Guid.NewGuid();
        var usuario = CreateTestUser(id, deptoId, rolId: Guid.Parse("30000000-0000-0000-0000-000000000003"));
        var items = new List<SeUsuari> { usuario };

        _repoMock
            .Setup(x => x.GetPaginatedAsync(1, 20, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        var result = await _handler.Handle(new ListUsuariosQuery(), CancellationToken.None);

        var dto = result.Items[0];
        dto.Id.Should().Be(id);
        dto.NombreCompleto.Should().Be("Test User");
        dto.Email.Should().Be("test@docflow.cl");
        dto.Rol.Should().Be("Operador");
        dto.DepartamentoId.Should().Be(deptoId);
        dto.Activo.Should().BeTrue();
        dto.Rut.Should().Be("12.345.678-9");
    }

    [Fact]
    public async Task Handle_Maps_Self_And_LastAdmin_Flags()
    {
        var selfId = _currentUserId;
        var usuario = CreateTestUser(selfId, rolId: Guid.NewGuid());
        usuario.Activar();
        SetRole(usuario, new Rol(Guid.NewGuid(), "Administrador", "Administrador del sistema"));
        var items = new List<SeUsuari> { usuario };

        _repoMock.Setup(x => x.GetPaginatedAsync(1, 20, null, null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync((items, 1));
        _repoMock.Setup(x => x.CountActiveAdministratorsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(new ListUsuariosQuery(), CancellationToken.None);

        var dto = result.Items[0];
        dto.EsCuentaPropia.Should().BeTrue();
        dto.EsUltimoAdminActivo.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Maps_RolId_From_Usuario()
    {
        var rolId = Guid.NewGuid();
        var usuario = CreateTestUser(rolId: rolId);
        var items = new List<SeUsuari> { usuario };

        _repoMock
            .Setup(x => x.GetPaginatedAsync(1, 20, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        var result = await _handler.Handle(new ListUsuariosQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].RolId.Should().Be(rolId.ToString());
    }

    [Fact]
    public async Task Handle_When_RolId_Is_Null_Maps_Null()
    {
        var usuario = CreateTestUser(rolId: null);
        var items = new List<SeUsuari> { usuario };

        _repoMock
            .Setup(x => x.GetPaginatedAsync(1, 20, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        var result = await _handler.Handle(new ListUsuariosQuery(), CancellationToken.None);

        result.Items[0].RolId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_When_Rut_Is_Null_Maps_Null()
    {
        var usuario = CreateTestUser(rut: null);
        var items = new List<SeUsuari> { usuario };

        _repoMock
            .Setup(x => x.GetPaginatedAsync(1, 20, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        var result = await _handler.Handle(new ListUsuariosQuery(), CancellationToken.None);

        result.Items[0].Rut.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Passes_Search_To_Repository()
    {
        var items = new List<SeUsuari>();

        _repoMock
            .Setup(x => x.GetPaginatedAsync(1, 20, null, null, null, "ada", It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 0));

        await _handler.Handle(new ListUsuariosQuery(Search: "ada"), CancellationToken.None);

        _repoMock.Verify(x => x.GetPaginatedAsync(1, 20, null, null, null, "ada", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SeUsuari CreateTestUser(Guid? id = null, Guid? deptoId = null, string? rut = "12.345.678-9", Guid? rolId = null)
    {
        var usucod = "testuser";
        var personal = SePersonal.Crear(usucod, "Test User", rut: rut, correo: "test@docflow.cl", estado: true);
        var usuario = SeUsuari.Crear(
            id ?? Guid.NewGuid(),
            usucod,
            "hashed-password",
            rolId,
            deptoId,
            estadoCuenta: true);

        usuario.VincularPersonal(personal);
        if (rolId is not null)
            SetRole(usuario, new Rol(rolId.Value, rolId == Guid.Parse("30000000-0000-0000-0000-000000000003") ? "Operador" : "Rol", "Rol del sistema"));

        return usuario;
    }

    private static void SetRole(SeUsuari usuario, Rol role)
        => typeof(SeUsuari).GetProperty(nameof(SeUsuari.Rol), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(usuario, role);
}
