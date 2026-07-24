using DocFlow.Application.Admin.Integraciones.DTOs;
using DocFlow.Application.Admin.Integraciones.Queries.GetIntegracion;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Integraciones.Queries.GetIntegracion;

public class GetIntegracionQueryHandlerTests
{
    private readonly Mock<IIntegracionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly GetIntegracionQueryHandler _handler;

    public GetIntegracionQueryHandlerTests()
    {
        _handler = new GetIntegracionQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Should_Return_Dto_When_Found()
    {
        var id = Guid.NewGuid();
        var integracion = ConfiguracionIntegracion.Crear(id, "DocDigital", TipoIntegracion.DocDigital,
            "https://api.docdigital.cl", "sk-1234567890abcdef", true);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(integracion);

        var result = await _handler.Handle(new GetIntegracionQuery(id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(id);
        result.Nombre.Should().Be("DocDigital");
        result.ApiKeyMasked.Should().Be("****cdef");
    }

    [Fact]
    public async Task Should_Throw_KeyNotFoundException_When_Missing()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ConfiguracionIntegracion?)null);

        var act = async () => await _handler.Handle(new GetIntegracionQuery(id), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
