using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Application.Admin.Respaldos.Queries.GetRespaldoConfig;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Respaldos.Queries.GetRespaldoConfig;

public class GetRespaldoConfigQueryHandlerTests
{
    private readonly Mock<IRespaldoRepository> _repoMock = new(MockBehavior.Strict);
    private readonly GetRespaldoConfigQueryHandler _handler;

    public GetRespaldoConfigQueryHandlerTests()
    {
        _handler = new GetRespaldoConfigQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Dto_When_Config_Exists()
    {
        var id = Guid.NewGuid();
        var config = RespaldoConfig.Crear(
            id, intervalo: 60, habilitado: true,
            maxBackupCount: 10, retentionDays: 30,
            outputPath: "./Respaldos", timeoutMinutos: 30);

        _repoMock
            .Setup(r => r.GetRespaldoConfigAsync())
            .ReturnsAsync(config);

        var result = await _handler.Handle(new GetRespaldoConfigQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(id);
        result.IntervaloMinutos.Should().Be(60);
        result.Habilitado.Should().BeTrue();
        result.MaxBackupCount.Should().Be(10);
        result.RetentionDays.Should().Be(30);
        result.OutputPath.Should().Be("./Respaldos");
        result.TimeoutMinutos.Should().Be(30);
    }

    [Fact]
    public async Task Handle_Should_Return_Default_When_No_Config_Exists()
    {
        _repoMock
            .Setup(r => r.GetRespaldoConfigAsync())
            .ReturnsAsync((RespaldoConfig?)null);

        var result = await _handler.Handle(new GetRespaldoConfigQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.IntervaloMinutos.Should().Be(60);
        result.Habilitado.Should().BeFalse();
        result.MaxBackupCount.Should().Be(10);
        result.RetentionDays.Should().Be(30);
        result.OutputPath.Should().Be("./Respaldos");
        result.TimeoutMinutos.Should().Be(5);
    }
}
