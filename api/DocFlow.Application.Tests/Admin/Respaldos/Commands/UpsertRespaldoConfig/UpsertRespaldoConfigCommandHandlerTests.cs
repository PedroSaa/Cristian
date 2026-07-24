using DocFlow.Application.Admin.Respaldos.Commands.UpsertRespaldoConfig;
using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Respaldos.Commands.UpsertRespaldoConfig;

public class UpsertRespaldoConfigCommandHandlerTests
{
    private readonly Mock<IRespaldoRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly UpsertRespaldoConfigCommandHandler _handler;
    private readonly Guid _adminId = Guid.NewGuid();

    public UpsertRespaldoConfigCommandHandlerTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        _currentUserMock.Setup(c => c.IpAddress).Returns("1.2.3.4");
        _currentUserMock.Setup(c => c.UserAgent).Returns("test-agent");
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
        _handler = new UpsertRespaldoConfigCommandHandler(
            _repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Upsert_And_Return_Dto()
    {
        RespaldoConfig? savedConfig = null;
        _repoMock
            .Setup(r => r.GetRespaldoConfigAsync())
            .ReturnsAsync((RespaldoConfig?)null);
        _repoMock
            .Setup(r => r.UpsertRespaldoConfigAsync(It.IsAny<RespaldoConfig>()))
            .Callback<RespaldoConfig>(c => savedConfig = c)
            .Returns(Task.CompletedTask);
        _repoMock
            .Setup(r => r.GetRespaldoConfigAsync())
            .ReturnsAsync(() => savedConfig);

        var cmd = new UpsertRespaldoConfigCommand(
            IntervaloMinutos: 60,
            Habilitado: true,
            MaxBackupCount: 10,
            RetentionDays: 30,
            OutputPath: "./Respaldos",
            TimeoutMinutos: 30);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.IntervaloMinutos.Should().Be(60);
        result.Habilitado.Should().BeTrue();
        result.MaxBackupCount.Should().Be(10);
        result.RetentionDays.Should().Be(30);
        result.OutputPath.Should().Be("./Respaldos");
        result.TimeoutMinutos.Should().Be(30);
        result.Id.Should().NotBe(Guid.Empty);
        result.ActualizadoEn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(reg =>
            reg.UsuarioId == _adminId
            && reg.Accion == "ActualizarConfigRespaldo"
            && reg.Entidad == "RespaldoConfig"
            && reg.DireccionIp == "1.2.3.4"
            && reg.UserAgent == "test-agent")), Times.Once);
    }
}
