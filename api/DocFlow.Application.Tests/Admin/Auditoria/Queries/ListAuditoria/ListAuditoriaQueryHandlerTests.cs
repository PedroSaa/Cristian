using DocFlow.Application.Admin.Auditoria.DTOs;
using DocFlow.Application.Admin.Auditoria.Queries.ListAuditoria;
using DocFlow.Application.Common;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Auditoria.Queries.ListAuditoria;

public class ListAuditoriaQueryHandlerTests
{
    private readonly Mock<IAuditoriaRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ListAuditoriaQueryHandler _handler;

    public ListAuditoriaQueryHandlerTests()
    {
        _handler = new ListAuditoriaQueryHandler(_repoMock.Object);
    }

    private static List<AuditoriaQueryResult> ToResults(List<RegistroAuditoria> registros)
        => registros.Select(r => new AuditoriaQueryResult(r, "Test User")).ToList();

    [Fact]
    public async Task Should_Return_Filtered_Results_When_Filters_Applied()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var registros = new List<RegistroAuditoria>
        {
            RegistroAuditoria.Crear(usuarioId, "Login", "Usuario", Guid.NewGuid().ToString(), "Inicio de sesión"),
            RegistroAuditoria.Crear(usuarioId, "Logout", "Usuario", Guid.NewGuid().ToString(), "Cierre de sesión"),
        };
        _repoMock
            .Setup(r => r.GetPaginatedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>()))
            .ReturnsAsync((ToResults(registros), registros.Count));

        var query = new ListAuditoriaQuery(Page: 1, PageSize: 20, UsuarioId: usuarioId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
        result.Page.Should().Be(1);
        result.TotalPaginas.Should().Be(1);
        result.Items.Should().AllSatisfy(dto => dto.UsuarioId.Should().Be(usuarioId));
    }

    [Fact]
    public async Task Should_Return_Empty_Page_When_No_Matches()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetPaginatedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>()))
            .ReturnsAsync((new List<AuditoriaQueryResult>(), 0));

        var query = new ListAuditoriaQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.TotalPaginas.Should().Be(0);
    }
}
