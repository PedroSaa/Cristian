using System.Threading.Channels;
using DocFlow.Application.Admin.Respaldos.Commands.TriggerRespaldo;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Respaldos.Commands.TriggerRespaldo;

public class TriggerRespaldoCommandHandlerTests
{
    private readonly Mock<IRespaldoRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();
    private readonly TriggerRespaldoCommandHandler _handler;
    private readonly Guid _adminId = Guid.NewGuid();

    public TriggerRespaldoCommandHandlerTests()
    {
        var writer = _channel.Writer;
        _repoMock
            .Setup(r => r.GetRespaldoConfigAsync())
            .ReturnsAsync((RespaldoConfig?)null);
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        _currentUserMock.Setup(c => c.IpAddress).Returns("1.2.3.4");
        _currentUserMock.Setup(c => c.UserAgent).Returns("test-agent");
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
        _handler = new TriggerRespaldoCommandHandler(
            _repoMock.Object, writer, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Should_Create_Pendiente_Respaldo_And_Enqueue()
    {
        Respaldo? savedRespaldo = null;
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<Respaldo>()))
            .Callback<Respaldo>(r => savedRespaldo = r)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new TriggerRespaldoCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Estado.Should().Be(EstadoRespaldo.Pendiente);
        result.Nombre.Should().StartWith("Respaldo-");
        result.TamanioBytes.Should().Be(0);

        savedRespaldo.Should().NotBeNull();
        savedRespaldo!.Estado.Should().Be(EstadoRespaldo.Pendiente);

        var enqueuedId = await _channel.Reader.ReadAsync(CancellationToken.None);
        enqueuedId.Should().Be(result.Id);
    }

    [Fact]
    public async Task Should_Return_Dto_With_All_Fields()
    {
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<Respaldo>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new TriggerRespaldoCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Nombre.Should().NotBeNullOrEmpty();
        result.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.TamanioBytes.Should().Be(0);
        result.Estado.Should().Be(EstadoRespaldo.Pendiente);
        result.Ruta.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Audit_Backup_Generation_With_Ip_And_UserAgent()
    {
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<Respaldo>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new TriggerRespaldoCommand(), CancellationToken.None);

        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(reg =>
            reg.UsuarioId == _adminId
            && reg.Accion == "GenerarRespaldo"
            && reg.Entidad == "Respaldo"
            && reg.EntidadId == result.Id.ToString()
            && reg.DireccionIp == "1.2.3.4"
            && reg.UserAgent == "test-agent")), Times.Once);
    }
}
