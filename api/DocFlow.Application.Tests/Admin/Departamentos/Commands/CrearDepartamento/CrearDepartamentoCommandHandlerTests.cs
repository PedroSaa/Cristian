using DocFlow.Application.Admin.Departamentos.Commands.CrearDepartamento;
using DocFlow.Application.Admin.Departamentos.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Departamentos.Commands.CrearDepartamento;

public class CrearDepartamentoCommandHandlerTests
{
    private readonly Mock<IDepartamentoRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<CrearDepartamentoCommandHandler>> _loggerMock = new();
    private readonly CrearDepartamentoCommandHandler _handler;
    private readonly Guid _adminId = Guid.NewGuid();

    public CrearDepartamentoCommandHandlerTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        _handler = new CrearDepartamentoCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Should_Create_And_Return_DepartamentoAdminDto()
    {
        _repoMock.Setup(r => r.ExistsByNombreAsync("Test Departamento")).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByCodigoAsync("TEST-001")).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Departamento>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var cmd = new CrearDepartamentoCommand(Nombre: "Test Departamento", Codigo: "TEST-001");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Nombre.Should().Be("Test Departamento");
        result.Codigo.Should().Be("TEST-001");
        result.Activo.Should().BeTrue();
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _adminId)), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_When_Codigo_Already_Exists()
    {
        _repoMock.Setup(r => r.ExistsByNombreAsync("Test")).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByCodigoAsync("TEST-001")).ReturnsAsync(true);

        var cmd = new CrearDepartamentoCommand(Nombre: "Test", Codigo: "TEST-001");
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_Throw_When_Nombre_Already_Exists()
    {
        _repoMock.Setup(r => r.ExistsByNombreAsync("Test Departamento")).ReturnsAsync(true);

        var cmd = new CrearDepartamentoCommand(Nombre: "Test Departamento", Codigo: "TEST-002");
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_Throw_Unauthorized_And_Not_Write_When_Current_User_Missing()
    {
        _currentUserMock.Setup(c => c.UserId).Returns((Guid?)null);

        var cmd = new CrearDepartamentoCommand(Nombre: "Test", Codigo: "TEST-003");
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Departamento>()), Times.Never);
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }
}
