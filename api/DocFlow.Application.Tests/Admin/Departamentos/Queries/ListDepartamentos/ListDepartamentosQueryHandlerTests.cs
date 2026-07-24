using DocFlow.Application.Admin.Departamentos.DTOs;
using DocFlow.Application.Admin.Departamentos.Queries.ListDepartamentos;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Departamentos.Queries.ListDepartamentos;

public class ListDepartamentosQueryHandlerTests
{
    private readonly Mock<IDepartamentoRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ListDepartamentosQueryHandler _handler;

    public ListDepartamentosQueryHandlerTests()
    {
        _handler = new ListDepartamentosQueryHandler(_repoMock.Object);
    }

    private static Departamento CreateDepartamento(string nombre, string codigo, bool activo)
    {
        var dep = Departamento.Crear(Guid.NewGuid(), nombre, codigo);
        if (!activo) dep.Desactivar();
        return dep;
    }

    [Fact]
    public async Task Should_Return_Filtered_List_When_Activo_True()
    {
        var departamentos = new List<Departamento>
        {
            CreateDepartamento("Depto A", "A-001", true),
            CreateDepartamento("Depto B", "B-001", true),
        };
        _repoMock.Setup(r => r.GetAllAsync(true)).ReturnsAsync(departamentos);

        var query = new ListDepartamentosQuery(Activo: true);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(d => d.Activo.Should().BeTrue());
    }

    [Fact]
    public async Task Should_Return_Filtered_List_When_Activo_False()
    {
        var departamentos = new List<Departamento>
        {
            CreateDepartamento("Depto C", "C-001", false),
        };
        _repoMock.Setup(r => r.GetAllAsync(false)).ReturnsAsync(departamentos);

        var query = new ListDepartamentosQuery(Activo: false);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result.All(d => d.Activo).Should().BeFalse();
    }

    [Fact]
    public async Task Should_Return_All_When_Activo_Null()
    {
        var departamentos = new List<Departamento>
        {
            CreateDepartamento("Depto A", "A-001", true),
            CreateDepartamento("Depto B", "B-001", false),
        };
        _repoMock.Setup(r => r.GetAllAsync(null)).ReturnsAsync(departamentos);

        var query = new ListDepartamentosQuery(Activo: null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
    }
}
