using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeForplaMedidas;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeForpla;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeForplaMedidas;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado;

public class SeForplaMedidasTests
{
    private const string CodForm = "{\"tipo\":\"T\",\"nt\":5,\"nc\":0,\"ns\":0}";

    private static readonly (short Id, string Objeto, short X, short Y, short Ancho, short Alto)[] LegacyDefaults =
    [
        (1, "AUTORIZACION", 100, 170, 1, 1),
        (2, "NUMERO", 100, 130, 0, 0),
        (3, "FIRMA", 50, 20, 450, 50),
        (4, "QR", 0, 0, 0, 0),
        (5, "FOTOFIRMA", 50, 50, 0, 0),
        (6, "QRFIRMA", 1, 1, 0, 0),
        (7, "FIRMAGOBx", 0, 0, 0, 0),
    ];

    private sealed class CrearSeForplaFixture
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Mock<ISeForplaRepository> Repo { get; } = new();
        public Mock<ISeForplaMedidaRepository> Medidas { get; } = new();
        public Mock<IAuditoriaRepository> Auditoria { get; } = new();
        public Mock<ICurrentUser> CurrentUser { get; } = new();
        public Mock<ISeFordocRepository> Formatos { get; } = new();
        public Mock<ICatalogoCategoriaRepository> Categorias { get; } = new();
        public Mock<ICatalogoSubcategoriaRepository> Subcategorias { get; } = new();
        public Mock<ISeUsuariRepository> Usuarios { get; } = new();
        public List<SeForplaMedida> MedidasSembradas { get; } = [];

        public CrearSeForplaFixture()
        {
            Repo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            Repo.Setup(r => r.CreateAsync(It.IsAny<SeForpla>())).Returns(Task.CompletedTask);
            Medidas.Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<SeForplaMedida>>()))
                .Callback<IEnumerable<SeForplaMedida>>(items => MedidasSembradas.AddRange(items))
                .Returns(Task.CompletedTask);
            Auditoria.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
            CurrentUser.SetupGet(x => x.UserId).Returns(UserId);
            Usuarios.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(SeUsuari.Crear(UserId, "USR01", "hash"));
            Formatos.Setup(r => r.ExistsAsync(It.IsAny<short>())).ReturnsAsync(true);
        }

        public CrearSeForplaCommandHandler BuildHandler() => new(
            Repo.Object,
            Auditoria.Object,
            CurrentUser.Object,
            Mock.Of<ILogger<CrearSeForplaCommandHandler>>(),
            Formatos.Object,
            Categorias.Object,
            Subcategorias.Object,
            Usuarios.Object,
            Medidas.Object);
    }

    [Fact]
    public async Task CrearSeForpla_Should_Seed_The_Seven_Legacy_Medidas()
    {
        var fx = new CrearSeForplaFixture();
        var handler = fx.BuildHandler();

        var result = await handler.Handle(
            new CrearSeForplaCommand("T", 5, null, null, "oficio.docx", Convert.ToBase64String(new byte[] { 1 }), null),
            CancellationToken.None);

        fx.Medidas.Verify(r => r.CreateRangeAsync(It.IsAny<IEnumerable<SeForplaMedida>>()), Times.Once);
        fx.MedidasSembradas.Should().HaveCount(7);
        fx.MedidasSembradas.Select(m => (m.IdForplaMed, m.Objeto, m.X, m.Y, m.Ancho, m.Alto))
            .Should().ContainInOrder(LegacyDefaults.Select(d => (d.Id, d.Objeto, d.X, d.Y, d.Ancho, d.Alto)));
        fx.MedidasSembradas.Should().OnlyContain(m => m.CodForm == result.CodForm);
    }

    private static (Mock<ISeForplaRepository> Plantillas, Mock<ISeForplaMedidaRepository> Medidas) BuildRepos(
        bool plantillaExiste, List<SeForplaMedida> medidas)
    {
        var plantillas = new Mock<ISeForplaRepository>();
        plantillas.Setup(r => r.ExistsAsync(CodForm)).ReturnsAsync(plantillaExiste);

        var medidasRepo = new Mock<ISeForplaMedidaRepository>();
        medidasRepo.Setup(r => r.GetByCodFormAsync(CodForm)).ReturnsAsync(medidas);
        medidasRepo.Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<SeForplaMedida>>()))
            .Callback<IEnumerable<SeForplaMedida>>(items => medidas.AddRange(items))
            .Returns(Task.CompletedTask);
        medidasRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        return (plantillas, medidasRepo);
    }

    [Fact]
    public async Task GetSeForplaMedidas_Should_Return_Existing_Medidas()
    {
        var existentes = new List<SeForplaMedida> { new(CodForm, 3, "FIRMA", 60, 25, 400, 45) };
        var (plantillas, medidasRepo) = BuildRepos(plantillaExiste: true, existentes);
        var handler = new GetSeForplaMedidasQueryHandler(plantillas.Object, medidasRepo.Object);

        var result = await handler.Handle(new GetSeForplaMedidasQuery(CodForm), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Should().Be(new SeForplaMedidaDto(3, "FIRMA", 60, 25, 400, 45));
        medidasRepo.Verify(r => r.CreateRangeAsync(It.IsAny<IEnumerable<SeForplaMedida>>()), Times.Never);
    }

    [Fact]
    public async Task GetSeForplaMedidas_WhenPlantillaHasNoMedidas_Should_Seed_And_Return_Defaults()
    {
        // Templates created before this subsystem existed have no rows: the query self-heals.
        var (plantillas, medidasRepo) = BuildRepos(plantillaExiste: true, []);
        var handler = new GetSeForplaMedidasQueryHandler(plantillas.Object, medidasRepo.Object);

        var result = await handler.Handle(new GetSeForplaMedidasQuery(CodForm), CancellationToken.None);

        medidasRepo.Verify(r => r.CreateRangeAsync(It.IsAny<IEnumerable<SeForplaMedida>>()), Times.Once);
        result.Should().HaveCount(7);
        result.Select(m => (m.IdForplaMed, m.Objeto, m.X, m.Y, m.Ancho, m.Alto))
            .Should().ContainInOrder(LegacyDefaults.Select(d => (d.Id, d.Objeto, d.X, d.Y, d.Ancho, d.Alto)));
    }

    [Fact]
    public async Task GetSeForplaMedidas_WhenPlantillaDoesNotExist_Throws_KeyNotFound()
    {
        var (plantillas, medidasRepo) = BuildRepos(plantillaExiste: false, []);
        var handler = new GetSeForplaMedidasQueryHandler(plantillas.Object, medidasRepo.Object);

        var act = () => handler.Handle(new GetSeForplaMedidasQuery(CodForm), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        medidasRepo.Verify(r => r.CreateRangeAsync(It.IsAny<IEnumerable<SeForplaMedida>>()), Times.Never);
    }

    private static ActualizarSeForplaMedidasCommandHandler BuildActualizarHandler(
        Mock<ISeForplaRepository> plantillas,
        Mock<ISeForplaMedidaRepository> medidasRepo,
        Mock<IAuditoriaRepository>? auditoria = null)
    {
        auditoria ??= new Mock<IAuditoriaRepository>();
        auditoria.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
        return new ActualizarSeForplaMedidasCommandHandler(
            plantillas.Object,
            medidasRepo.Object,
            auditoria.Object,
            currentUser.Object,
            Mock.Of<ILogger<ActualizarSeForplaMedidasCommandHandler>>());
    }

    [Fact]
    public async Task ActualizarSeForplaMedidas_Should_Update_Only_Received_Items_And_Never_Objeto()
    {
        var medidas = SeForplaMedida.CrearDefaults(CodForm).ToList();
        var (plantillas, medidasRepo) = BuildRepos(plantillaExiste: true, medidas);
        var auditoria = new Mock<IAuditoriaRepository>();
        var handler = BuildActualizarHandler(plantillas, medidasRepo, auditoria);

        await handler.Handle(new ActualizarSeForplaMedidasCommand(CodForm,
        [
            new ActualizarSeForplaMedidaItem(3, 60, 25, 400, 45),
            new ActualizarSeForplaMedidaItem(6, 10, 15, 200, 200),
        ]), CancellationToken.None);

        var firma = medidas.Single(m => m.IdForplaMed == 3);
        (firma.X, firma.Y, firma.Ancho, firma.Alto).Should().Be(((short)60, (short)25, (short)400, (short)45));
        firma.Objeto.Should().Be("FIRMA");

        var qrFirma = medidas.Single(m => m.IdForplaMed == 6);
        (qrFirma.X, qrFirma.Y, qrFirma.Ancho, qrFirma.Alto).Should().Be(((short)10, (short)15, (short)200, (short)200));

        // The QR row (id 4) was not sent, so it must keep its defaults untouched.
        var qr = medidas.Single(m => m.IdForplaMed == 4);
        (qr.X, qr.Y, qr.Ancho, qr.Alto).Should().Be(((short)0, (short)0, (short)0, (short)0));

        medidasRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        auditoria.Verify(r => r.AddAsync(It.Is<RegistroAuditoria>(a => a.Accion == "ActualizarSeForplaMedidas")), Times.Once);
    }

    [Fact]
    public async Task ActualizarSeForplaMedidas_WhenPlantillaDoesNotExist_Throws_KeyNotFound()
    {
        var (plantillas, medidasRepo) = BuildRepos(plantillaExiste: false, []);
        var handler = BuildActualizarHandler(plantillas, medidasRepo);

        var act = () => handler.Handle(new ActualizarSeForplaMedidasCommand(CodForm,
            [new ActualizarSeForplaMedidaItem(3, 60, 25, 400, 45)]), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ActualizarSeForplaMedidas_WhenMedidaDoesNotExist_Throws_KeyNotFound()
    {
        var medidas = SeForplaMedida.CrearDefaults(CodForm).ToList();
        var (plantillas, medidasRepo) = BuildRepos(plantillaExiste: true, medidas);
        var handler = BuildActualizarHandler(plantillas, medidasRepo);

        var act = () => handler.Handle(new ActualizarSeForplaMedidasCommand(CodForm,
            [new ActualizarSeForplaMedidaItem(99, 60, 25, 400, 45)]), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        medidasRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public void ActualizarSeForplaMedidas_Validator_Should_Enforce_Ranges_And_NonEmpty_Items()
    {
        var validator = new ActualizarSeForplaMedidasCommandValidator();

        validator.TestValidate(new ActualizarSeForplaMedidasCommand(CodForm, []))
            .ShouldHaveValidationErrorFor(x => x.Items);

        validator.TestValidate(new ActualizarSeForplaMedidasCommand(CodForm,
            [new ActualizarSeForplaMedidaItem(1, -1, 0, 0, 0)]))
            .IsValid.Should().BeFalse();

        validator.TestValidate(new ActualizarSeForplaMedidasCommand(CodForm,
            [new ActualizarSeForplaMedidaItem(1, 0, -1, 0, 0)]))
            .IsValid.Should().BeFalse();

        validator.TestValidate(new ActualizarSeForplaMedidasCommand(CodForm,
            [new ActualizarSeForplaMedidaItem(1, 0, 0, -1, 0)]))
            .IsValid.Should().BeFalse();

        validator.TestValidate(new ActualizarSeForplaMedidasCommand(CodForm,
            [new ActualizarSeForplaMedidaItem(1, 0, 0, 0, -1)]))
            .IsValid.Should().BeFalse();

        validator.TestValidate(new ActualizarSeForplaMedidasCommand(CodForm,
            [new ActualizarSeForplaMedidaItem(1, 0, 0, 0, 32767)]))
            .ShouldNotHaveAnyValidationErrors();
    }
}
