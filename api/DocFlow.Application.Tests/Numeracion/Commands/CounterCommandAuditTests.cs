using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Numeracion.Commands.CreateCounter;
using DocFlow.Application.Numeracion.Commands.DeactivateCounter;
using DocFlow.Application.Numeracion.Commands.ReactivateCounter;
using DocFlow.Application.Numeracion.Commands.SetCounterValue;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.NumeracionesDocumento;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Numeracion.Commands;

public class CounterCommandAuditTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Mock<ICounterService> _counterService = new();
    private readonly Mock<IAuditoriaRepository> _auditoria = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<ISeFordocRepository> _formatos = new();

    public CounterCommandAuditTests()
    {
        _currentUser.Setup(x => x.UserId).Returns(_actorId);
        _auditoria.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CreateCounter_Writes_Audit_With_Actor_And_Created_Values()
    {
        var created = Counter(Guid.NewGuid(), ultimoValor: 7);
        _counterService
            .Setup(x => x.CreateCounterAsync(It.IsAny<CounterKey>(), 7, "ANUAL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var handler = new CreateCounterHandler(_counterService.Object, _auditoria.Object, _currentUser.Object, _formatos.Object);
        await handler.Handle(new CreateCounterCommand("DOC", "ORG", Periodicidad: "ANUAL", ValorInicial: 7), CancellationToken.None);

        VerifyAudit("CrearContadorNumeracion", created.Id.ToString(), "ultimoValor=7", "codigo=DOC");
    }

    [Fact]
    public async Task CreateCounter_Missing_Actor_Fails_Before_Mutation()
    {
        _currentUser.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = new CreateCounterHandler(_counterService.Object, _auditoria.Object, _currentUser.Object, _formatos.Object);

        var act = async () => await handler.Handle(new CreateCounterCommand("DOC", "ORG"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _counterService.Verify(x => x.CreateCounterAsync(It.IsAny<CounterKey>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditoria.Verify(x => x.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task CreateCounter_WithNonExistentTipoCod_Throws_And_DoesNotCreate()
    {
        _formatos.Setup(x => x.ExistsAsync((short)999)).ReturnsAsync(false);
        var handler = new CreateCounterHandler(_counterService.Object, _auditoria.Object, _currentUser.Object, _formatos.Object);

        var act = async () => await handler.Handle(new CreateCounterCommand("DOC", "ORG", TipoCod: 999), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*999*");
        _counterService.Verify(x => x.CreateCounterAsync(It.IsAny<CounterKey>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditoria.Verify(x => x.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task CreateCounter_WithExistingTipoCod_Creates()
    {
        var created = Counter(Guid.NewGuid(), ultimoValor: 0);
        _formatos.Setup(x => x.ExistsAsync((short)5)).ReturnsAsync(true);
        _counterService
            .Setup(x => x.CreateCounterAsync(It.IsAny<CounterKey>(), 0, "ANUAL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var handler = new CreateCounterHandler(_counterService.Object, _auditoria.Object, _currentUser.Object, _formatos.Object);
        await handler.Handle(new CreateCounterCommand("DOC", "ORG", TipoCod: 5, Periodicidad: "ANUAL"), CancellationToken.None);

        _counterService.Verify(x => x.CreateCounterAsync(It.IsAny<CounterKey>(), 0, "ANUAL", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCounterValue_Writes_Audit_With_Before_And_After_Values()
    {
        var id = Guid.NewGuid();
        var before = Counter(id, ultimoValor: 4);
        var after = Counter(id, ultimoValor: 10);
        _counterService.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(before);
        _counterService.Setup(x => x.SetCounterValueAsync(id, 10, It.IsAny<CancellationToken>())).ReturnsAsync(after);

        var handler = new SetCounterValueHandler(_counterService.Object, _auditoria.Object, _currentUser.Object);
        await handler.Handle(new SetCounterValueCommand(id, 10), CancellationToken.None);

        VerifyAudit("ActualizarValorContadorNumeracion", id.ToString(), "ultimoValorAntes=4", "ultimoValorDespues=10");
    }

    [Fact]
    public async Task SetCounterValue_Missing_Actor_Fails_Before_Mutation()
    {
        _currentUser.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = new SetCounterValueHandler(_counterService.Object, _auditoria.Object, _currentUser.Object);

        var act = async () => await handler.Handle(new SetCounterValueCommand(Guid.NewGuid(), 10), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _counterService.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _counterService.Verify(x => x.SetCounterValueAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateCounter_Writes_Audit_With_Previous_And_New_Active_State()
    {
        var id = Guid.NewGuid();
        var before = Counter(id, ultimoValor: 1);
        _counterService.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(before);
        _counterService.Setup(x => x.DeactivateCounterAsync(id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new DeactivateCounterHandler(_counterService.Object, _auditoria.Object, _currentUser.Object);
        await handler.Handle(new DeactivateCounterCommand(id), CancellationToken.None);

        VerifyAudit("DesactivarContadorNumeracion", id.ToString(), "activoAntes=True", "activoDespues=False");
    }

    [Fact]
    public async Task DeactivateCounter_Missing_Actor_Fails_Before_Mutation()
    {
        _currentUser.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = new DeactivateCounterHandler(_counterService.Object, _auditoria.Object, _currentUser.Object);

        var act = async () => await handler.Handle(new DeactivateCounterCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _counterService.Verify(x => x.DeactivateCounterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReactivateCounter_Writes_Audit_With_Previous_And_New_Active_State()
    {
        var id = Guid.NewGuid();
        var before = Counter(id, ultimoValor: 1);
        before.Desactivar();
        var after = Counter(id, ultimoValor: 1);
        _counterService.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(before);
        _counterService.Setup(x => x.ReactivateCounterAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(after);

        var handler = new ReactivateCounterHandler(_counterService.Object, _auditoria.Object, _currentUser.Object);
        await handler.Handle(new ReactivateCounterCommand(id), CancellationToken.None);

        VerifyAudit("ReactivarContadorNumeracion", id.ToString(), "activoAntes=False", "activoDespues=True");
    }

    [Fact]
    public async Task ReactivateCounter_Missing_Actor_Fails_Before_Mutation()
    {
        _currentUser.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = new ReactivateCounterHandler(_counterService.Object, _auditoria.Object, _currentUser.Object);

        var act = async () => await handler.Handle(new ReactivateCounterCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _counterService.Verify(x => x.ReactivateCounterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyAudit(string accion, string entidadId, params string[] detailParts)
    {
        _auditoria.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _actorId &&
            r.Accion == accion &&
            r.Entidad == "ContadorNumeracion" &&
            r.EntidadId == entidadId &&
            detailParts.All(part => r.Detalle.Contains(part)))), Times.Once);
    }

    private static ContadorNumeracion Counter(Guid id, long ultimoValor) =>
        new(id, "DOC", "ORG", string.Empty, 0, string.Empty, "CONTINUO", string.Empty, ultimoValor);
}
