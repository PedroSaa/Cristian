using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeCorfor;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeFordoc;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeForpla;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeCorfor;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeFordoc;
using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeForpla;
using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeCorfor;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeCorfor;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeFordoc;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeForpla;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeForplaPdf;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeCorfors;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeFordocs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeForplas;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado;

public class DocumentConfigurationCatalogsTests
{
    [Fact]
    public void CrearSeFordoc_Validator_Should_Require_Core_Fields()
    {
        var validator = new CrearSeFordocCommandValidator();

        var result = validator.TestValidate(new CrearSeFordocCommand(0, 0, "", 1, null, 0, 0, 0, null));

        // Legacy rule: TipoRec/TipoInt default to 0 when empty, so 0 is valid; only TipoDesc is required.
        result.ShouldNotHaveValidationErrorFor(x => x.TipoRec);
        result.ShouldNotHaveValidationErrorFor(x => x.TipoInt);
        result.ShouldHaveValidationErrorFor(x => x.TipoDesc);
    }

    [Fact]
    public async Task CrearSeFordoc_Handler_Should_Accept_Zero_TipoRec()
    {
        // 03:00Z in June (Chile winter, UTC-4) = 23:00 of the 14th in Chile → CorrFecha must be the 14th, not the UTC 15th.
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 3, 0, 0, TimeSpan.Zero));
        var repo = new Mock<ISeFordocRepository>();
        repo.Setup(r => r.GetProximoIdAsync()).ReturnsAsync((short)1);

        var auditoria = new Mock<IAuditoriaRepository>();
        auditoria.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
        var handler = new CrearSeFordocCommandHandler(repo.Object, auditoria.Object, currentUser.Object, Mock.Of<ILogger<CrearSeFordocCommandHandler>>(), fakeTime);

        var result = await handler.Handle(new CrearSeFordocCommand(0, 2, "Formato", 1, null, 0, 0, 0, null), CancellationToken.None);

        result.TipoRec.Should().Be(0);
        result.CorrFecha.Should().Be(new DateTime(2026, 6, 14));
    }

    [Fact]
    public async Task CrearSeFordoc_Handler_Should_Autogenerate_TipoCod()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 3, 0, 0, TimeSpan.Zero));
        var repo = new Mock<ISeFordocRepository>();
        repo.Setup(r => r.GetProximoIdAsync()).ReturnsAsync((short)12);
        SeFordoc? persisted = null;
        repo.Setup(r => r.CreateAsync(It.IsAny<SeFordoc>()))
            .Callback<SeFordoc>(e => persisted = e)
            .Returns(Task.CompletedTask);

        var auditoria = new Mock<IAuditoriaRepository>();
        auditoria.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
        var handler = new CrearSeFordocCommandHandler(repo.Object, auditoria.Object, currentUser.Object, Mock.Of<ILogger<CrearSeFordocCommandHandler>>(), fakeTime);

        var result = await handler.Handle(new CrearSeFordocCommand(1, 2, "Formato", 1, null, 0, 0, 0, null), CancellationToken.None);

        result.TipoCod.Should().Be((short)12);
        persisted!.TipoCod.Should().Be((short)12);
        repo.Verify(r => r.GetProximoIdAsync(), Times.Once);
    }

    [Fact]
    public async Task ActualizarSeFordoc_Handler_Should_Accept_Zero_TipoRec()
    {
        // 03:00Z in June (Chile winter, UTC-4) = 23:00 of the 14th in Chile → CorrFecha must be overwritten to the 14th.
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 3, 0, 0, TimeSpan.Zero));
        var entity = new SeFordoc(1, 2, 3, "Inicial", 4, new DateTime(2026, 5, 20));
        var repo = new Mock<ISeFordocRepository>();
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        var auditoria = new Mock<IAuditoriaRepository>();
        auditoria.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
        var handler = new ActualizarSeFordocCommandHandler(repo.Object, auditoria.Object, currentUser.Object, Mock.Of<ILogger<ActualizarSeFordocCommandHandler>>(), fakeTime);

        await handler.Handle(new ActualizarSeFordocCommand(1, 0, 2, "Formato", 1, null, 0, 0, 0, null), CancellationToken.None);

        entity.TipoRec.Should().Be(0);
        entity.CorrFecha.Should().Be(new DateTime(2026, 6, 14));
    }

    private sealed class CrearSeForplaFixture
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Mock<ISeForplaRepository> Repo { get; } = new();
        public Mock<IAuditoriaRepository> Auditoria { get; } = new();
        public Mock<ICurrentUser> CurrentUser { get; } = new();
        public Mock<ISeFordocRepository> Formatos { get; } = new();
        public Mock<ICatalogoCategoriaRepository> Categorias { get; } = new();
        public Mock<ICatalogoSubcategoriaRepository> Subcategorias { get; } = new();
        public Mock<ISeUsuariRepository> Usuarios { get; } = new();
        public Mock<ISeForplaMedidaRepository> Medidas { get; } = new();
        public SeForpla? Persisted { get; private set; }

        public CrearSeForplaFixture()
        {
            Repo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            Repo.Setup(r => r.CreateAsync(It.IsAny<SeForpla>()))
                .Callback<SeForpla>(e => Persisted = e)
                .Returns(Task.CompletedTask);
            Auditoria.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
            CurrentUser.SetupGet(x => x.UserId).Returns(UserId);
            Usuarios.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(SeUsuari.Crear(UserId, "USR01", "hash"));
            Formatos.Setup(r => r.ExistsAsync(It.IsAny<short>())).ReturnsAsync(true);
            Categorias.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int catCod) => new CatalogoCategoria(catCod, "Categoría"));
            Subcategorias.Setup(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<short>())).ReturnsAsync(true);
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

    private static string ValidBlob() => Convert.ToBase64String(new byte[] { 1, 2, 3 });

    [Fact]
    public async Task CrearSeForpla_Formato_Should_Build_Exact_CodForm_And_Derive_Fields()
    {
        var fx = new CrearSeForplaFixture();
        var handler = fx.BuildHandler();

        var result = await handler.Handle(
            new CrearSeForplaCommand("T", 5, null, null, "oficio.docx", ValidBlob(), "Observación"),
            CancellationToken.None);

        fx.Persisted.Should().NotBeNull();
        fx.Persisted!.CodForm.Should().Be("{\"tipo\":\"T\",\"nt\":5,\"nc\":0,\"ns\":0}");
        fx.Persisted.TipoCod.Should().Be((short)5);
        fx.Persisted.NomForm.Should().Be("oficio");
        fx.Persisted.ExtForm.Should().Be("docx");
        fx.Persisted.SisForm.Should().Be("1");
        fx.Persisted.Usucod.Should().Be("USR01");
        fx.Persisted.ObsForm.Should().Be("Observación");
        fx.Persisted.Alto.Should().BeNull();
        fx.Persisted.Ancho.Should().BeNull();
        fx.Persisted.BlobForm.Should().Equal(1, 2, 3);
        result.CodForm.Should().Be(fx.Persisted.CodForm);
        fx.Auditoria.Verify(r => r.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Once);
    }

    [Fact]
    public async Task CrearSeForpla_Categoria_Should_Build_Exact_CodForm_And_Store_CatCod_In_TipoCod()
    {
        var fx = new CrearSeForplaFixture();
        var handler = fx.BuildHandler();

        await handler.Handle(
            new CrearSeForplaCommand("C", null, 12, null, "memo.docx", ValidBlob(), null),
            CancellationToken.None);

        fx.Persisted!.CodForm.Should().Be("{\"tipo\":\"C\",\"nt\":0,\"nc\":12,\"ns\":0}");
        fx.Persisted.TipoCod.Should().Be((short)12);
    }

    [Fact]
    public async Task CrearSeForpla_Subcategoria_Should_Build_Exact_CodForm_And_Store_IdSubcategoria_In_TipoCod()
    {
        var fx = new CrearSeForplaFixture();
        var handler = fx.BuildHandler();

        await handler.Handle(
            new CrearSeForplaCommand("S", null, 12, 3, "acta.docx", ValidBlob(), null),
            CancellationToken.None);

        fx.Persisted!.CodForm.Should().Be("{\"tipo\":\"S\",\"nt\":0,\"nc\":12,\"ns\":3}");
        fx.Persisted.TipoCod.Should().Be((short)3);
        fx.Subcategorias.Verify(r => r.ExistsAsync(12, (short)3), Times.Once);
    }

    [Fact]
    public async Task CrearSeForpla_Should_Truncate_NomForm_To_30_And_Lowercase_ExtForm()
    {
        var fx = new CrearSeForplaFixture();
        var handler = fx.BuildHandler();
        const string fileName = "nombre-de-archivo-larguisimo-que-supera-treinta.DOCX";

        await handler.Handle(
            new CrearSeForplaCommand("T", 5, null, null, fileName, ValidBlob(), null),
            CancellationToken.None);

        fx.Persisted!.NomForm.Should().Be("nombre-de-archivo-larguisimo-q");
        fx.Persisted.NomForm.Length.Should().Be(30);
        fx.Persisted.ExtForm.Should().Be("docx");
    }

    [Fact]
    public async Task CrearSeForpla_WithNonExistentFormato_Throws_And_DoesNotPersist()
    {
        var fx = new CrearSeForplaFixture();
        fx.Formatos.Setup(r => r.ExistsAsync((short)777)).ReturnsAsync(false);
        var handler = fx.BuildHandler();

        var act = async () => await handler.Handle(
            new CrearSeForplaCommand("T", 777, null, null, "oficio.docx", ValidBlob(), null),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>().WithMessage("*777*");
        fx.Repo.Verify(r => r.CreateAsync(It.IsAny<SeForpla>()), Times.Never);
        fx.Auditoria.Verify(r => r.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task CrearSeForpla_WithNonExistentCategoria_Throws()
    {
        var fx = new CrearSeForplaFixture();
        fx.Categorias.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((CatalogoCategoria?)null);
        var handler = fx.BuildHandler();

        var act = async () => await handler.Handle(
            new CrearSeForplaCommand("C", null, 99, null, "memo.docx", ValidBlob(), null),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>().WithMessage("*99*");
        fx.Repo.Verify(r => r.CreateAsync(It.IsAny<SeForpla>()), Times.Never);
    }

    [Fact]
    public async Task CrearSeForpla_WithNonExistentSubcategoria_Throws()
    {
        var fx = new CrearSeForplaFixture();
        fx.Subcategorias.Setup(r => r.ExistsAsync(12, (short)9)).ReturnsAsync(false);
        var handler = fx.BuildHandler();

        var act = async () => await handler.Handle(
            new CrearSeForplaCommand("S", null, 12, 9, "acta.docx", ValidBlob(), null),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        fx.Repo.Verify(r => r.CreateAsync(It.IsAny<SeForpla>()), Times.Never);
    }

    [Fact]
    public async Task CrearSeForpla_WithDuplicateCodForm_Throws_InvalidOperationException()
    {
        var fx = new CrearSeForplaFixture();
        fx.Repo.Setup(r => r.ExistsAsync("{\"tipo\":\"T\",\"nt\":5,\"nc\":0,\"ns\":0}")).ReturnsAsync(true);
        var handler = fx.BuildHandler();

        var act = async () => await handler.Handle(
            new CrearSeForplaCommand("T", 5, null, null, "oficio.docx", ValidBlob(), null),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        fx.Repo.Verify(r => r.CreateAsync(It.IsAny<SeForpla>()), Times.Never);
    }

    [Fact]
    public async Task CrearSeForpla_WithCatCodOutOfShortRange_Throws_ValidationException()
    {
        var fx = new CrearSeForplaFixture();
        var handler = fx.BuildHandler();

        var act = async () => await handler.Handle(
            new CrearSeForplaCommand("C", null, 40000, null, "memo.docx", ValidBlob(), null),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        fx.Repo.Verify(r => r.CreateAsync(It.IsAny<SeForpla>()), Times.Never);
    }

    [Fact]
    public async Task CrearSeForpla_WhenAuthenticatedUserHasNoSeUsuari_Throws_ValidationException()
    {
        var fx = new CrearSeForplaFixture();
        fx.Usuarios.Setup(r => r.GetByIdAsync(fx.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeUsuari?)null);
        var handler = fx.BuildHandler();

        var act = async () => await handler.Handle(
            new CrearSeForplaCommand("T", 5, null, null, "oficio.docx", ValidBlob(), null),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        fx.Repo.Verify(r => r.CreateAsync(It.IsAny<SeForpla>()), Times.Never);
    }

    [Fact]
    public async Task CrearSeForpla_WithInvalidBase64_Throws_ValidationException()
    {
        var fx = new CrearSeForplaFixture();
        var handler = fx.BuildHandler();

        var act = async () => await handler.Handle(
            new CrearSeForplaCommand("T", 5, null, null, "oficio.docx", "esto-no-es-base64!!", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public void CrearSeForpla_Validator_Should_Enforce_Conditional_Fields()
    {
        var validator = new CrearSeForplaCommandValidator();

        validator.TestValidate(new CrearSeForplaCommand("X", null, null, null, "", "", null))
            .ShouldHaveValidationErrorFor(x => x.TipoSeleccion);
        validator.TestValidate(new CrearSeForplaCommand("T", null, null, null, "a.docx", ValidBlob(), null))
            .ShouldHaveValidationErrorFor(x => x.TipoCod);
        validator.TestValidate(new CrearSeForplaCommand("C", null, null, null, "a.docx", ValidBlob(), null))
            .ShouldHaveValidationErrorFor(x => x.CatCod);
        validator.TestValidate(new CrearSeForplaCommand("S", null, 12, null, "a.docx", ValidBlob(), null))
            .ShouldHaveValidationErrorFor(x => x.IdSubcategoria);
        validator.TestValidate(new CrearSeForplaCommand("T", 5, null, null, "", ValidBlob(), null))
            .ShouldHaveValidationErrorFor(x => x.FileName);
        validator.TestValidate(new CrearSeForplaCommand("T", 5, null, null, "a.docx", "", null))
            .ShouldHaveValidationErrorFor(x => x.BlobForm);
        validator.TestValidate(new CrearSeForplaCommand("T", 5, null, null, "a.docx", ValidBlob(), new string('x', 256)))
            .ShouldHaveValidationErrorFor(x => x.ObsForm);
        validator.TestValidate(new CrearSeForplaCommand("S", null, 12, 3, "a.docx", ValidBlob(), "ok"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ActualizarSeForpla_WithFile_Should_Replace_Blob_And_Rederive_Name_And_Extension()
    {
        var entity = new SeForpla("{\"tipo\":\"T\",\"nt\":5,\"nc\":0,\"ns\":0}", "USR01", 5, "Vieja", new byte[] { 9 }, "1", "obs vieja", "docx");
        var repo = new Mock<ISeForplaRepository>();
        repo.Setup(r => r.GetByIdAsync(entity.CodForm)).ReturnsAsync(entity);
        var auditoria = new Mock<IAuditoriaRepository>();
        auditoria.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

        var handler = new ActualizarSeForplaCommandHandler(repo.Object, auditoria.Object, currentUser.Object, Mock.Of<ILogger<ActualizarSeForplaCommandHandler>>());
        await handler.Handle(new ActualizarSeForplaCommand(entity.CodForm, "nueva version.DOC", Convert.ToBase64String(new byte[] { 7, 8 }), "obs nueva"), CancellationToken.None);

        entity.NomForm.Should().Be("nueva version");
        entity.ExtForm.Should().Be("doc");
        entity.BlobForm.Should().Equal(7, 8);
        entity.ObsForm.Should().Be("obs nueva");
        entity.TipoCod.Should().Be((short)5);
        entity.Usucod.Should().Be("USR01");
        repo.Verify(r => r.UpdateAsync(entity), Times.Once);
    }

    [Fact]
    public async Task ActualizarSeForpla_WithoutFile_Should_Only_Update_Observacion()
    {
        var entity = new SeForpla("{\"tipo\":\"C\",\"nt\":0,\"nc\":12,\"ns\":0}", "USR01", 12, "Plantilla", new byte[] { 9 }, "1", "obs vieja", "docx");
        var repo = new Mock<ISeForplaRepository>();
        repo.Setup(r => r.GetByIdAsync(entity.CodForm)).ReturnsAsync(entity);
        var auditoria = new Mock<IAuditoriaRepository>();
        auditoria.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

        var handler = new ActualizarSeForplaCommandHandler(repo.Object, auditoria.Object, currentUser.Object, Mock.Of<ILogger<ActualizarSeForplaCommandHandler>>());
        await handler.Handle(new ActualizarSeForplaCommand(entity.CodForm, null, null, "solo obs"), CancellationToken.None);

        entity.NomForm.Should().Be("Plantilla");
        entity.ExtForm.Should().Be("docx");
        entity.BlobForm.Should().Equal(9);
        entity.ObsForm.Should().Be("solo obs");
        repo.Verify(r => r.UpdateAsync(entity), Times.Once);
    }

    [Fact]
    public async Task GetSeForplaPdf_Query_Should_Return_Converted_Pdf()
    {
        var repo = new Mock<ISeForplaRepository>();
        repo.Setup(r => r.GetByIdAsync("PLA-01"))
            .ReturnsAsync(new SeForpla("PLA-01", "USR01", 2, "Plantilla", new byte[] { 1, 2 }, "Sistema", null, ".docx", 8.5, 11.0));

        var converter = new Mock<IOnlyOfficeDocumentService>();
        converter.Setup(s => s.ConvertToPdfAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31 });

        var handler = new GetSeForplaPdfQueryHandler(repo.Object, converter.Object);
        var result = await handler.Handle(new GetSeForplaPdfQuery("PLA-01"), CancellationToken.None);

        result.FileName.Should().Be("Plantilla.pdf");
        result.FileBytes.Should().StartWith(new byte[] { 0x25, 0x50, 0x44, 0x46 });
    }

    [Fact]
    public async Task GetSeCorfor_Query_Should_Return_Dto()
    {
        var repo = new Mock<ISeCorforRepository>();
        repo.Setup(r => r.GetByIdAsync("COR-01")).ReturnsAsync(new SeCorfor("COR-01", 3, "Correlativo", new DateTime(2026, 5, 20)));

        var handler = new GetSeCorforQueryHandler(repo.Object);
        var result = await handler.Handle(new GetSeCorforQuery("COR-01"), CancellationToken.None);

        result.CorrTip.Should().Be("COR-01");
        result.CorrDes.Should().Be("Correlativo");
    }

    [Fact]
    public async Task UpdateSeFordoc_Handler_Should_Update_And_Audit()
    {
        var entity = new SeFordoc(1, 2, 3, "Inicial", 4, new DateTime(2026, 5, 20), 5, 1, 0, 1, "FMT-1");
        var repo = new Mock<ISeFordocRepository>();
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        repo.Setup(r => r.UpdateAsync(It.IsAny<SeFordoc>())).Returns(Task.CompletedTask);

        var auditoria = new Mock<IAuditoriaRepository>();
        auditoria.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

        var handler = new ActualizarSeFordocCommandHandler(repo.Object, auditoria.Object, currentUser.Object, Mock.Of<ILogger<ActualizarSeFordocCommandHandler>>(), TimeProvider.System);
        await handler.Handle(new ActualizarSeFordocCommand(1, 20, 21, "Actualizado", 22, 23, 0, 1, 0, "FMT-2"), CancellationToken.None);

        entity.TipoRec.Should().Be(20);
        entity.TipoInt.Should().Be(21);
        auditoria.Verify(r => r.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSeCorfor_Handler_Should_Delete_And_Audit()
    {
        var entity = new SeCorfor("COR-01", 3, "Correlativo", new DateTime(2026, 5, 20));
        var repo = new Mock<ISeCorforRepository>();
        repo.Setup(r => r.GetByIdAsync("COR-01")).ReturnsAsync(entity);
        repo.Setup(r => r.DeleteAsync(It.IsAny<SeCorfor>())).Returns(Task.CompletedTask);

        var auditoria = new Mock<IAuditoriaRepository>();
        auditoria.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

        var handler = new EliminarSeCorforCommandHandler(repo.Object, auditoria.Object, currentUser.Object, Mock.Of<ILogger<EliminarSeCorforCommandHandler>>());
        await handler.Handle(new EliminarSeCorforCommand("COR-01"), CancellationToken.None);

        repo.Verify(r => r.DeleteAsync(It.IsAny<SeCorfor>()), Times.Once);
        auditoria.Verify(r => r.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Once);
    }
}
