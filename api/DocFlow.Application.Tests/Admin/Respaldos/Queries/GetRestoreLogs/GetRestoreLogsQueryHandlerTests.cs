using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Application.Admin.Respaldos.Queries.GetRestoreLogs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Respaldos.Queries.GetRestoreLogs;

public class GetRestoreLogsQueryHandlerTests
{
    private readonly Mock<IRestoreLogRepository> _repoMock = new(MockBehavior.Strict);
    private readonly GetRestoreLogsQueryHandler _handler;

    public GetRestoreLogsQueryHandlerTests()
    {
        _handler = new GetRestoreLogsQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_DTOs_For_Respaldo()
    {
        var respaldoId = Guid.NewGuid();
        var logs = new List<RestoreLog>
        {
            CreateLog(respaldoId, EstadoRestore.Completado, "Ok"),
            CreateLog(respaldoId, EstadoRestore.Fallido, "Error DB"),
        };

        _repoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync((IReadOnlyList<RestoreLog>)logs);

        var result = await _handler.Handle(
            new GetRestoreLogsQuery(respaldoId),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(dto => dto.RespaldoId == respaldoId).Should().BeTrue();
        result.Select(dto => dto.Estado).Should().BeEquivalentTo(
            new[] { EstadoRestore.Completado, EstadoRestore.Fallido });

        _repoMock.VerifyAll();
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_When_No_Logs()
    {
        var respaldoId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync((IReadOnlyList<RestoreLog>)new List<RestoreLog>());

        var result = await _handler.Handle(
            new GetRestoreLogsQuery(respaldoId),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _repoMock.VerifyAll();
    }

    [Fact]
    public async Task Handle_Should_Filter_By_RespaldoId()
    {
        var respaldoId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var logs = new List<RestoreLog>
        {
            CreateLog(respaldoId, EstadoRestore.Completado, null),
            CreateLog(otherId, EstadoRestore.Completado, null),
            CreateLog(respaldoId, EstadoRestore.Fallido, "err"),
        };

        _repoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync((IReadOnlyList<RestoreLog>)logs);

        var result = await _handler.Handle(
            new GetRestoreLogsQuery(respaldoId),
            CancellationToken.None);

        result.Should().HaveCount(2);
        result.All(dto => dto.RespaldoId == respaldoId).Should().BeTrue();

        _repoMock.VerifyAll();
    }

    [Fact]
    public async Task Handle_Should_Map_All_DTO_Fields()
    {
        var respaldoId = Guid.NewGuid();
        var log = CreateLog(respaldoId, EstadoRestore.Completado, null);

        _repoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync((IReadOnlyList<RestoreLog>)new[] { log });

        var result = await _handler.Handle(
            new GetRestoreLogsQuery(respaldoId),
            CancellationToken.None);

        var dto = result.Single();
        dto.Id.Should().Be(log.Id);
        dto.RespaldoId.Should().Be(log.RespaldoId);
        dto.FechaInicio.Should().Be(log.FechaInicio);
        dto.FechaFin.Should().Be(log.FechaFin);
        dto.Estado.Should().Be(log.Estado);
        dto.MensajeError.Should().Be(log.MensajeError);

        _repoMock.VerifyAll();
    }

    private static RestoreLog CreateLog(Guid respaldoId, EstadoRestore estado, string? error)
    {
        var log = RestoreLog.Crear(respaldoId);
        if (estado == EstadoRestore.EnProceso) log.MarcarEnProceso();
        else if (estado == EstadoRestore.Completado) { log.MarcarEnProceso(); log.Completar(); }
        else if (estado == EstadoRestore.Fallido) { log.MarcarEnProceso(); log.Fallar(error); }
        return log;
    }
}
