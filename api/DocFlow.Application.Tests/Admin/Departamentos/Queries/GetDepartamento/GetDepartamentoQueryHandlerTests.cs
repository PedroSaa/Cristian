using DocFlow.Application.Admin.Departamentos.DTOs;
using DocFlow.Application.Admin.Departamentos.Queries.GetDepartamento;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Departamentos.Queries.GetDepartamento;

public class GetDepartamentoQueryHandlerTests
{
    private readonly Mock<IDepartamentoRepository> _repoMock = new(MockBehavior.Strict);
    private readonly GetDepartamentoQueryHandler _handler;

    public GetDepartamentoQueryHandlerTests()
    {
        _handler = new GetDepartamentoQueryHandler(_repoMock.Object);
    }

    private static Departamento CreateDepartamento() =>
        Departamento.Crear(Guid.NewGuid(), "Test", "TEST-001");

    [Fact]
    public async Task Should_Return_DepartamentoAdminDto_When_Found()
    {
        var dep = CreateDepartamento();
        _repoMock.Setup(r => r.GetByIdAsync(dep.Id)).ReturnsAsync(dep);

        var query = new GetDepartamentoQuery(dep.Id);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(dep.Id);
        result.Nombre.Should().Be("Test");
        result.Codigo.Should().Be("TEST-001");
        result.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Throw_KeyNotFoundException_When_Not_Found()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Departamento?)null);

        var query = new GetDepartamentoQuery(id);
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
