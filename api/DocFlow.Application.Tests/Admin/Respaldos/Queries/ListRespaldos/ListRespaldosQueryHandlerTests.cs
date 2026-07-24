using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Application.Admin.Respaldos.Queries.ListRespaldos;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Respaldos.Queries.ListRespaldos;

public class ListRespaldosQueryHandlerTests
{
    private readonly Mock<IRespaldoRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ListRespaldosQueryHandler _handler;

    public ListRespaldosQueryHandlerTests()
    {
        _handler = new ListRespaldosQueryHandler(_repoMock.Object);
    }

    private static Respaldo CreateRespaldo(string nombre, long tamanioBytes, string ruta = "/respaldos/stub")
    {
        var respaldo = Respaldo.Crear(Guid.NewGuid(), nombre, ruta);
        respaldo.Completar(ruta, tamanioBytes);
        return respaldo;
    }

    [Fact]
    public async Task Should_Return_All_Respaldos_As_Dtos()
    {
        var respaldos = new List<Respaldo>
        {
            CreateRespaldo("Respaldo-001", 1024),
            CreateRespaldo("Respaldo-002", 2048),
        };

        _repoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(respaldos);

        var result = await _handler.Handle(new ListRespaldosQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(d =>
        {
            d.Id.Should().NotBe(Guid.Empty);
            d.Nombre.Should().NotBeNullOrEmpty();
            d.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            d.TamanioBytes.Should().BeGreaterThan(0);
            d.Estado.Should().Be(EstadoRespaldo.Completado);
            d.Ruta.Should().Be("/respaldos/stub");
        });
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_No_Respaldos()
    {
        _repoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Respaldo>());

        var result = await _handler.Handle(new ListRespaldosQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
