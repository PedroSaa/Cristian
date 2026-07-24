using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeClaseg;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeFormaEnvio;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeTiptar;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeClaseg;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeFormaEnvio;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeTiptar;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeClaseg;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeFormaEnvio;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeTiptar;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeClaseg;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeFormaEnvio;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeTiptar;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeClaseg;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeFormaEnvio;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeTiptar;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado;

public class SeClasegTests
{
    private readonly Mock<ISeClasegRepository> _repo = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoria = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUser = new(MockBehavior.Strict);
    private readonly Mock<ILogger<CrearSeClasegCommandHandler>> _createLogger = new();
    private readonly Mock<ILogger<ActualizarSeClasegCommandHandler>> _updateLogger = new();
    private readonly Mock<ILogger<EliminarSeClasegCommandHandler>> _deleteLogger = new();

    public SeClasegTests()
    {
        _currentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task Create_Get_List_Update_Delete_Work()
    {
        _repo.Setup(x => x.GetProximoIdAsync()).ReturnsAsync((short)1);
        _repo.Setup(x => x.CreateAsync(It.IsAny<SeClaseg>())).Returns(Task.CompletedTask);
        _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new SeClaseg(1, "AB", "General"));
        _repo.Setup(x => x.UpdateAsync(It.IsAny<SeClaseg>())).Returns(Task.CompletedTask);
        _repo.Setup(x => x.DeleteAsync(It.IsAny<SeClaseg>())).Returns(Task.CompletedTask);
        _repo.Setup(x => x.GetAllAsync()).ReturnsAsync([new SeClaseg(1, "AB", "General")]);
        _auditoria.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var created = await new CrearSeClasegCommandHandler(_repo.Object, _auditoria.Object, _currentUser.Object, _createLogger.Object)
            .Handle(new CrearSeClasegCommand("AB", "General"), CancellationToken.None);
        created.DFDClasif.Should().Be("General");

        var listed = await new ListSeClasegQueryHandler(_repo.Object).Handle(new ListSeClasegQuery(), CancellationToken.None);
        listed.Should().ContainSingle();

        var gotten = await new GetSeClasegQueryHandler(_repo.Object).Handle(new GetSeClasegQuery(1), CancellationToken.None);
        gotten.DFNCLASIF.Should().Be("AB");

        await new ActualizarSeClasegCommandHandler(_repo.Object, _auditoria.Object, _currentUser.Object, _updateLogger.Object)
            .Handle(new ActualizarSeClasegCommand(1, "CD", "Actualizada"), CancellationToken.None);

        await new EliminarSeClasegCommandHandler(_repo.Object, _auditoria.Object, _currentUser.Object, _deleteLogger.Object)
            .Handle(new EliminarSeClasegCommand(1), CancellationToken.None);
    }

    [Fact]
    public async Task Create_AutogeneratesCode_UsingProximoId()
    {
        _repo.Setup(x => x.GetProximoIdAsync()).ReturnsAsync((short)5);
        SeClaseg? persisted = null;
        _repo.Setup(x => x.CreateAsync(It.IsAny<SeClaseg>()))
            .Callback<SeClaseg>(e => persisted = e)
            .Returns(Task.CompletedTask);
        _auditoria.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var created = await new CrearSeClasegCommandHandler(_repo.Object, _auditoria.Object, _currentUser.Object, _createLogger.Object)
            .Handle(new CrearSeClasegCommand("CD", "Interna"), CancellationToken.None);

        created.DFClasif.Should().Be((short)5);
        persisted!.DFClasif.Should().Be((short)5);
        _repo.Verify(x => x.GetProximoIdAsync(), Times.Once);
    }
}

public class SeFormaEnvioTests
{
    private readonly Mock<ISeFormaEnvioRepository> _repo = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoria = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUser = new(MockBehavior.Strict);
    private readonly Mock<ILogger<CrearSeFormaEnvioCommandHandler>> _createLogger = new();
    private readonly Mock<ILogger<ActualizarSeFormaEnvioCommandHandler>> _updateLogger = new();
    private readonly Mock<ILogger<EliminarSeFormaEnvioCommandHandler>> _deleteLogger = new();

    public SeFormaEnvioTests()
    {
        _currentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task Create_Get_List_Update_Delete_Work()
    {
        _repo.Setup(x => x.GetProximoIdAsync()).ReturnsAsync((short)1);
        _repo.Setup(x => x.CreateAsync(It.IsAny<SeFormaEnvio>())).Returns(Task.CompletedTask);
        _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new SeFormaEnvio(1, "Correo"));
        _repo.Setup(x => x.UpdateAsync(It.IsAny<SeFormaEnvio>())).Returns(Task.CompletedTask);
        _repo.Setup(x => x.DeleteAsync(It.IsAny<SeFormaEnvio>())).Returns(Task.CompletedTask);
        _repo.Setup(x => x.GetAllAsync()).ReturnsAsync([new SeFormaEnvio(1, "Correo")]);
        _auditoria.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var created = await new CrearSeFormaEnvioCommandHandler(_repo.Object, _auditoria.Object, _currentUser.Object, _createLogger.Object)
            .Handle(new CrearSeFormaEnvioCommand("Correo"), CancellationToken.None);
        created.FormaEnvio.Should().Be("Correo");

        var listed = await new ListSeFormaEnvioQueryHandler(_repo.Object).Handle(new ListSeFormaEnvioQuery(), CancellationToken.None);
        listed.Should().ContainSingle();

        var gotten = await new GetSeFormaEnvioQueryHandler(_repo.Object).Handle(new GetSeFormaEnvioQuery(1), CancellationToken.None);
        gotten.FormaEnvio.Should().Be("Correo");

        await new ActualizarSeFormaEnvioCommandHandler(_repo.Object, _auditoria.Object, _currentUser.Object, _updateLogger.Object)
            .Handle(new ActualizarSeFormaEnvioCommand(1, "Actualizada"), CancellationToken.None);

        await new EliminarSeFormaEnvioCommandHandler(_repo.Object, _auditoria.Object, _currentUser.Object, _deleteLogger.Object)
            .Handle(new EliminarSeFormaEnvioCommand(1), CancellationToken.None);
    }

    [Fact]
    public async Task Create_AutogeneratesCode_UsingProximoId()
    {
        _repo.Setup(x => x.GetProximoIdAsync()).ReturnsAsync((short)8);
        SeFormaEnvio? persisted = null;
        _repo.Setup(x => x.CreateAsync(It.IsAny<SeFormaEnvio>()))
            .Callback<SeFormaEnvio>(e => persisted = e)
            .Returns(Task.CompletedTask);
        _auditoria.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var created = await new CrearSeFormaEnvioCommandHandler(_repo.Object, _auditoria.Object, _currentUser.Object, _createLogger.Object)
            .Handle(new CrearSeFormaEnvioCommand("Courier"), CancellationToken.None);

        created.IdFormaEnvio.Should().Be((short)8);
        persisted!.IdFormaEnvio.Should().Be((short)8);
        _repo.Verify(x => x.GetProximoIdAsync(), Times.Once);
    }
}

public class SeTiptarTests
{
    private readonly Mock<ISeTiptarRepository> _repo = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoria = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUser = new(MockBehavior.Strict);
    private readonly Mock<ILogger<CrearSeTiptarCommandHandler>> _createLogger = new();
    private readonly Mock<ILogger<ActualizarSeTiptarCommandHandler>> _updateLogger = new();
    private readonly Mock<ILogger<EliminarSeTiptarCommandHandler>> _deleteLogger = new();

    public SeTiptarTests()
    {
        _currentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task Create_Get_List_Update_Delete_Work()
    {
        _repo.Setup(x => x.ExistsAsync("A01")).ReturnsAsync(false);
        _repo.Setup(x => x.CreateAsync(It.IsAny<SeTiptar>())).Returns(Task.CompletedTask);
        _repo.Setup(x => x.GetByIdAsync("A01")).ReturnsAsync(new SeTiptar("A01", "Obs", "Desc"));
        _repo.Setup(x => x.UpdateAsync(It.IsAny<SeTiptar>())).Returns(Task.CompletedTask);
        _repo.Setup(x => x.DeleteAsync(It.IsAny<SeTiptar>())).Returns(Task.CompletedTask);
        _repo.Setup(x => x.GetAllAsync()).ReturnsAsync([new SeTiptar("A01", "Obs", "Desc")]);
        _auditoria.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var created = await new CrearSeTiptarCommandHandler(_repo.Object, _auditoria.Object, _currentUser.Object, _createLogger.Object)
            .Handle(new CrearSeTiptarCommand("A01", "Obs", "Desc"), CancellationToken.None);
        created.DFTACCION.Should().Be("A01");

        var listed = await new ListSeTiptarQueryHandler(_repo.Object).Handle(new ListSeTiptarQuery(), CancellationToken.None);
        listed.Should().ContainSingle();

        var gotten = await new GetSeTiptarQueryHandler(_repo.Object).Handle(new GetSeTiptarQuery("A01"), CancellationToken.None);
        gotten.DFTACCION.Should().Be("A01");

        await new ActualizarSeTiptarCommandHandler(_repo.Object, _auditoria.Object, _currentUser.Object, _updateLogger.Object)
            .Handle(new ActualizarSeTiptarCommand("A01", "Nueva obs", "Nueva desc"), CancellationToken.None);

        await new EliminarSeTiptarCommandHandler(_repo.Object, _auditoria.Object, _currentUser.Object, _deleteLogger.Object)
            .Handle(new EliminarSeTiptarCommand("A01"), CancellationToken.None);
    }
}
