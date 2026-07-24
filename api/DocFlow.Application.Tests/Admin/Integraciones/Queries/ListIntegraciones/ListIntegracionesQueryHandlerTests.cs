using DocFlow.Application.Admin.Integraciones.DTOs;
using DocFlow.Application.Admin.Integraciones.Queries.ListIntegraciones;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Integraciones.Queries.ListIntegraciones;

public class ListIntegracionesQueryHandlerTests
{
    private readonly Mock<IIntegracionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ListIntegracionesQueryHandler _handler;

    public ListIntegracionesQueryHandlerTests()
    {
        _handler = new ListIntegracionesQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Should_Return_All_Integraciones_With_Masked_ApiKey()
    {
        var items = new List<ConfiguracionIntegracion>
        {
            ConfiguracionIntegracion.Crear(Guid.NewGuid(), "DocDigital", TipoIntegracion.DocDigital,
                "https://api.docdigital.cl", "sk-1234567890abcdef", true),
            ConfiguracionIntegracion.Crear(Guid.NewGuid(), "FirmaGob", TipoIntegracion.FirmaGob,
                "https://api.firma.cl", "abc", true),
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        var result = await _handler.Handle(new ListIntegracionesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].ApiKeyMasked.Should().Be("****cdef");
        result[1].ApiKeyMasked.Should().Be("****");
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_None()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ConfiguracionIntegracion>());

        var result = await _handler.Handle(new ListIntegracionesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
