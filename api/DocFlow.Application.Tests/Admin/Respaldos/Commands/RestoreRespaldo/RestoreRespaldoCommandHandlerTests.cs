using System.Threading.Channels;
using DocFlow.Application.Admin.Respaldos.Commands.RestoreRespaldo;
using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Respaldos.Commands.RestoreRespaldo;

public class RestoreRespaldoCommandHandlerTests
{
    private readonly Mock<IRestoreLogRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IRespaldoRepository> _respaldoRepoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Channel<RestoreRequest> _channel = Channel.CreateUnbounded<RestoreRequest>();
    private readonly RestoreRespaldoCommandHandler _handler;
    private readonly Guid _adminId = Guid.NewGuid();

    public RestoreRespaldoCommandHandlerTests()
    {
        var writer = _channel.Writer;
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        _currentUserMock.Setup(c => c.IpAddress).Returns("1.2.3.4");
        _currentUserMock.Setup(c => c.UserAgent).Returns("test-agent");
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
        _handler = new RestoreRespaldoCommandHandler(
            _repoMock.Object,
            _respaldoRepoMock.Object,
            writer,
            _auditoriaMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Create_RestoreLog_Pendiente_And_Enqueue()
    {
        var respaldoId = Guid.NewGuid();
        var respaldo = Respaldo.Crear(respaldoId, "Respaldo-20260516", "/ruta");
        respaldo.Completar("/ruta/backup.sql.gz", 2048);

        RestoreLog? savedLog = null;
        _respaldoRepoMock
            .Setup(r => r.GetByIdAsync(respaldoId))
            .ReturnsAsync(respaldo);
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<RestoreLog>()))
            .Callback<RestoreLog>(l => savedLog = l)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new RestoreRespaldoCommand(respaldoId),
            CancellationToken.None);

        // Assert handler result
        result.Should().NotBeNull();
        result.RespaldoId.Should().Be(respaldoId);
        result.Estado.Should().Be(EstadoRestore.Pendiente);
        result.FechaFin.Should().BeNull();
        result.MensajeError.Should().BeNull();

        // Assert the entity was saved as Pendiente
        savedLog.Should().NotBeNull();
        savedLog!.Estado.Should().Be(EstadoRestore.Pendiente);
        savedLog.RespaldoId.Should().Be(respaldoId);

        // Assert the request was enqueued
        var enqueued = await _channel.Reader.ReadAsync(CancellationToken.None);
        enqueued.RespaldoId.Should().Be(respaldoId);
        enqueued.RestoreLogId.Should().Be(result.Id);

        _respaldoRepoMock.VerifyAll();
        _repoMock.VerifyAll();
    }

    [Fact]
    public async Task Handle_Should_Throw_KeyNotFoundException_When_Respaldo_Not_Found()
    {
        var respaldoId = Guid.NewGuid();
        _respaldoRepoMock
            .Setup(r => r.GetByIdAsync(respaldoId))
            .ReturnsAsync((Respaldo?)null);

        var act = () => _handler.Handle(
            new RestoreRespaldoCommand(respaldoId),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{respaldoId}*");

        _respaldoRepoMock.VerifyAll();
    }

    [Fact]
    public async Task Handle_Should_Throw_InvalidOperationException_When_Not_Completed()
    {
        var respaldoId = Guid.NewGuid();
        var respaldo = Respaldo.Crear(respaldoId, "Respaldo-20260516", "/ruta");
        // Not completed — still Pendiente

        _respaldoRepoMock
            .Setup(r => r.GetByIdAsync(respaldoId))
            .ReturnsAsync(respaldo);

        var act = () => _handler.Handle(
            new RestoreRespaldoCommand(respaldoId),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Completado*");

        _respaldoRepoMock.VerifyAll();
    }

    [Fact]
    public async Task Handle_Should_Return_Dto_With_All_Fields()
    {
        var respaldoId = Guid.NewGuid();
        var respaldo = Respaldo.Crear(respaldoId, "Respaldo-20260516", "/ruta");
        respaldo.Completar("/ruta/backup.sql.gz", 2048);

        _respaldoRepoMock
            .Setup(r => r.GetByIdAsync(respaldoId))
            .ReturnsAsync(respaldo);
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<RestoreLog>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new RestoreRespaldoCommand(respaldoId),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.RespaldoId.Should().Be(respaldoId);
        result.FechaInicio.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.FechaFin.Should().BeNull();
        result.Estado.Should().Be(EstadoRestore.Pendiente);
        result.MensajeError.Should().BeNull();

        _respaldoRepoMock.VerifyAll();
        _repoMock.VerifyAll();
    }

    [Fact]
    public async Task Handle_Should_Audit_Restore_With_Ip_And_UserAgent()
    {
        var respaldoId = Guid.NewGuid();
        var respaldo = Respaldo.Crear(respaldoId, "Respaldo-20260516", "/ruta");
        respaldo.Completar("/ruta/backup.sql.gz", 2048);
        _respaldoRepoMock.Setup(r => r.GetByIdAsync(respaldoId)).ReturnsAsync(respaldo);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<RestoreLog>())).Returns(Task.CompletedTask);

        await _handler.Handle(new RestoreRespaldoCommand(respaldoId), CancellationToken.None);

        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(reg =>
            reg.UsuarioId == _adminId
            && reg.Accion == "RestaurarRespaldo"
            && reg.Entidad == "Respaldo"
            && reg.EntidadId == respaldoId.ToString()
            && reg.DireccionIp == "1.2.3.4"
            && reg.UserAgent == "test-agent")), Times.Once);
    }
}
