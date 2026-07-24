using DocFlow.Application.Admin.Auditoria.DTOs;
using DocFlow.Application.Admin.Auditoria.Queries.GetAuditoria;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Auditoria.Queries.GetAuditoria;

public class GetAuditoriaQueryHandlerTests
{
    private readonly Mock<IAuditoriaRepository> _repoMock = new(MockBehavior.Strict);
    private readonly GetAuditoriaQueryHandler _handler;

    public GetAuditoriaQueryHandlerTests()
    {
        _handler = new GetAuditoriaQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Should_Return_RegistroAuditoriaDto_When_Found()
    {
        // Arrange
        var registro = RegistroAuditoria.Crear(
            Guid.NewGuid(), "Login", "Usuario", Guid.NewGuid().ToString(), "Inicio de sesión");
        var queryResult = new AuditoriaQueryResult(registro, "Test User");
        _repoMock.Setup(r => r.GetByIdWithUserAsync(registro.Id)).ReturnsAsync(queryResult);

        var query = new GetAuditoriaQuery(registro.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(registro.Id);
        result.UsuarioId.Should().Be(registro.UsuarioId);
        result.UsuarioNombre.Should().Be("Test User");
        result.Accion.Should().Be("Login");
        result.Entidad.Should().Be("Usuario");
        result.EntidadId.Should().Be(registro.EntidadId);
        result.Detalle.Should().Be("Inicio de sesión");
        result.DireccionIp.Should().BeNull();
        result.UserAgent.Should().BeNull();
        result.CreadoEn.Should().Be(registro.CreadoEn);
    }

    [Fact]
    public async Task Should_Throw_KeyNotFoundException_When_Not_Found()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdWithUserAsync(id)).ReturnsAsync((AuditoriaQueryResult?)null);

        var query = new GetAuditoriaQuery(id);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Registro de auditoría {id} no encontrado.");
    }
}
