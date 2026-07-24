using DocFlow.Application.Admin.Configuracion.DTOs;
using DocFlow.Application.Admin.Configuracion.Queries.ListConfiguracion;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Configuracion.Queries.ListConfiguracion;

public class ListConfiguracionQueryHandlerTests
{
    private readonly Mock<IConfiguracionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ListConfiguracionQueryHandler _handler;

    public ListConfiguracionQueryHandlerTests()
    {
        _handler = new ListConfiguracionQueryHandler(_repoMock.Object);
    }

    private static ConfiguracionSistema CreateConfig(string clave, string valor, string? descripcion = null)
    {
        return ConfiguracionSistema.Crear(Guid.NewGuid(), clave, valor, descripcion ?? string.Empty);
    }

    [Fact]
    public async Task Should_Return_All_Entries()
    {
        var configs = new List<ConfiguracionSistema>
        {
            CreateConfig("JwtExpirationMinutos", "480"),
            CreateConfig("PasswordMinLength", "8"),
            CreateConfig("RequireMfaAdministradores", "false"),
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

        var result = await _handler.Handle(new ListConfiguracionQuery(), CancellationToken.None);

        result.Should().HaveCount(3);
        result.Should().AllSatisfy(d => d.Clave.Should().NotBeNullOrEmpty());
        result.Should().ContainSingle(d => d.Clave == "JwtExpirationMinutos" && d.MinValue == 15 && d.MaxValue == 1440);
        result.Should().ContainSingle(d => d.Clave == "PasswordMinLength" && d.MinValue == 8 && d.MaxValue == 32);
        result.Should().ContainSingle(d => d.Clave == "RequireMfaAdministradores" && d.Grupo == "seguridad" && d.Tipo == "bool");
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_No_Entries()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ConfiguracionSistema>());

        var result = await _handler.Handle(new ListConfiguracionQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
