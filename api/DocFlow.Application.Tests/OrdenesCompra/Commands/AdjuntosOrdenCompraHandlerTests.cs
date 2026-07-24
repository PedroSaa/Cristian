using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.Commands.AgregarAdjuntoOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.EliminarAdjuntoOrdenCompra;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.OrdenesCompra.Commands;

public class AgregarAdjuntoOrdenCompraHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly AgregarAdjuntoOrdenCompraHandler _handler;

    public AgregarAdjuntoOrdenCompraHandlerTests()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns(OrdenCompraTestFactory.CreadorId);
        _handler = new AgregarAdjuntoOrdenCompraHandler(
            _repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    private OrdenCompra SetupOrden(OrdenCompra? orden = null, int adjuntosExistentes = 0)
    {
        var oc = orden ?? OrdenCompraTestFactory.Borrador();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);
        _repoMock.Setup(r => r.GetAdjuntosMetadataAsync(oc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(0, adjuntosExistentes)
                .Select(i => new OrdenCompraAdjuntoMetadata(
                    Guid.NewGuid(), $"adjunto-{i}.pdf", "application/pdf", 10,
                    OrdenCompraTestFactory.CreadorId, DateTime.UtcNow))
                .ToList());
        return oc;
    }

    /// <summary>Minimal content whose magic bytes match the declared type.</summary>
    private static byte[] PdfBytes() => "%PDF-1.4\n"u8.ToArray();
    private static byte[] PngBytes() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2];
    private static byte[] JpegBytes() => [0xFF, 0xD8, 0xFF, 0xDB, 1, 2];
    private static byte[] ZipBytes() => [0x50, 0x4B, 0x03, 0x04, 1, 2]; // docx/xlsx (OpenXML = zip)
    private static byte[] OleBytes() => [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 1]; // doc/xls legado
    private static byte[] WebpBytes() =>
        [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50]; // RIFF....WEBP

    [Fact]
    public async Task Should_Add_Adjunto_And_Return_Metadata()
    {
        var oc = SetupOrden();
        var contenido = PdfBytes();
        var cmd = new AgregarAdjuntoOrdenCompraCommand(
            oc.Id, "cotizacion.pdf", "application/pdf", Convert.ToBase64String(contenido));

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.NombreArchivo.Should().Be("cotizacion.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.Tamano.Should().Be(contenido.Length);
        result.SubidoPor.Should().Be(OrdenCompraTestFactory.CreadorId);

        _repoMock.Verify(r => r.AddAdjuntoAsync(
            It.Is<OrdenCompraAdjunto>(a => a.OrdenCompraId == oc.Id && a.Tamano == contenido.Length),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(
            It.Is<RegistroAuditoria>(r => r.Accion == "OrdenCompraAdjuntoAgregado")), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_Adjunto_Exceeds_10MB()
    {
        var oc = SetupOrden();
        // application/pdf: content type must be whitelisted so the size rule is the one under test.
        var contenido = new byte[AgregarAdjuntoOrdenCompraHandler.TamanoMaximoBytes + 1];
        var cmd = new AgregarAdjuntoOrdenCompraCommand(
            oc.Id, "grande.pdf", "application/pdf", Convert.ToBase64String(contenido));

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*10 MB*");
        _repoMock.Verify(r => r.AddAdjuntoAsync(It.IsAny<OrdenCompraAdjunto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Fail_When_Base64_Invalid()
    {
        var oc = SetupOrden();
        var cmd = new AgregarAdjuntoOrdenCompraCommand(oc.Id, "x.pdf", "application/pdf", "esto-no-es-base64!!!");

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Should_Fail_When_Orden_Anulada()
    {
        var oc = OrdenCompraTestFactory.Anulada();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);
        var cmd = new AgregarAdjuntoOrdenCompraCommand(
            oc.Id, "x.pdf", "application/pdf", Convert.ToBase64String(PdfBytes()));

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_Orden_Missing()
    {
        var cmd = new AgregarAdjuntoOrdenCompraCommand(
            Guid.NewGuid(), "x.pdf", "application/pdf", Convert.ToBase64String(PdfBytes()));

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("application/x-msdownload")]
    [InlineData("application/octet-stream")]
    public async Task Should_Fail_When_ContentType_Not_Allowed(string contentType)
    {
        var oc = SetupOrden();
        var cmd = new AgregarAdjuntoOrdenCompraCommand(
            oc.Id, "archivo.bin", contentType, Convert.ToBase64String(new byte[] { 1, 2 }));

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*no permitido*");
        _repoMock.Verify(r => r.AddAdjuntoAsync(It.IsAny<OrdenCompraAdjunto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("application/pdf")]                // PNG disfrazado de PDF
    [InlineData("image/jpeg")]                     // PNG disfrazado de JPEG
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task Should_Fail_When_Content_Signature_Does_Not_Match_Declared_Type(string contentType)
    {
        var oc = SetupOrden();
        var cmd = new AgregarAdjuntoOrdenCompraCommand(
            oc.Id, "disfrazado", contentType, Convert.ToBase64String(PngBytes()));

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*no corresponde*");
        _repoMock.Verify(r => r.AddAdjuntoAsync(It.IsAny<OrdenCompraAdjunto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Fail_When_Executable_Disguised_As_Pdf()
    {
        var oc = SetupOrden();
        var exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03 }; // "MZ" — cabecera PE de Windows
        var cmd = new AgregarAdjuntoOrdenCompraCommand(
            oc.Id, "informe.pdf", "application/pdf", Convert.ToBase64String(exe));

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*no corresponde*");
    }

    [Fact]
    public async Task Should_Fail_When_Binary_Disguised_As_TextPlain()
    {
        // text/plain no tiene firma: se rechaza si trae bytes NUL (típico de binarios renombrados).
        var oc = SetupOrden();
        var binario = new byte[] { 0x4D, 0x5A, 0x00, 0x01, 0x02 };
        var cmd = new AgregarAdjuntoOrdenCompraCommand(
            oc.Id, "notas.txt", "text/plain", Convert.ToBase64String(binario));

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*no corresponde*");
    }

    public static TheoryData<string, byte[]> TiposConFirmaValida => new()
    {
        { "image/png", PngBytes() },
        { "IMAGE/JPEG", JpegBytes() },
        { "text/plain", "guía de recepción"u8.ToArray() },
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ZipBytes() },
        { "application/msword", OleBytes() },
        { "image/webp", WebpBytes() },
    };

    [Theory]
    [MemberData(nameof(TiposConFirmaValida))]
    public async Task Should_Allow_Whitelisted_ContentTypes_CaseInsensitive(string contentType, byte[] contenido)
    {
        var oc = SetupOrden();
        var cmd = new AgregarAdjuntoOrdenCompraCommand(
            oc.Id, "archivo", contentType, Convert.ToBase64String(contenido));

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_Fail_When_Orden_Already_Has_Max_Adjuntos()
    {
        var oc = SetupOrden(adjuntosExistentes: 10);
        var cmd = new AgregarAdjuntoOrdenCompraCommand(
            oc.Id, "extra.pdf", "application/pdf", Convert.ToBase64String(PdfBytes()));

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*10*adjuntos*");
        _repoMock.Verify(r => r.AddAdjuntoAsync(It.IsAny<OrdenCompraAdjunto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Allow_Adjunto_When_Orden_Has_Nine()
    {
        var oc = SetupOrden(adjuntosExistentes: 9);
        var cmd = new AgregarAdjuntoOrdenCompraCommand(
            oc.Id, "decimo.pdf", "application/pdf", Convert.ToBase64String(PdfBytes()));

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_Allow_Agregar_When_Orden_Aprobada()
    {
        // Los respaldos posteriores a la aprobación (guías, recepciones) son legítimos.
        var oc = SetupOrden(OrdenCompraTestFactory.Aprobada());
        var cmd = new AgregarAdjuntoOrdenCompraCommand(
            oc.Id, "guia.pdf", "application/pdf", Convert.ToBase64String(PdfBytes()));

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

public class EliminarAdjuntoOrdenCompraHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly EliminarAdjuntoOrdenCompraHandler _handler;

    public EliminarAdjuntoOrdenCompraHandlerTests()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns(OrdenCompraTestFactory.CreadorId);
        _handler = new EliminarAdjuntoOrdenCompraHandler(
            _repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Should_Remove_Adjunto()
    {
        var oc = OrdenCompraTestFactory.Borrador();
        var adjunto = OrdenCompraAdjunto.Crear(
            Guid.NewGuid(), oc.Id, "a.pdf", "application/pdf", [1, 2], OrdenCompraTestFactory.CreadorId);
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);
        _repoMock.Setup(r => r.GetAdjuntoAsync(oc.Id, adjunto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(adjunto);

        await _handler.Handle(new EliminarAdjuntoOrdenCompraCommand(oc.Id, adjunto.Id), CancellationToken.None);

        _repoMock.Verify(r => r.RemoveAdjuntoAsync(adjunto, It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(
            It.Is<RegistroAuditoria>(r => r.Accion == "OrdenCompraAdjuntoEliminado")), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_Adjunto_Missing()
    {
        var oc = OrdenCompraTestFactory.Borrador();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

        var act = async () => await _handler.Handle(
            new EliminarAdjuntoOrdenCompraCommand(oc.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private async Task<Func<Task>> ActEliminar(OrdenCompra oc)
    {
        var adjunto = OrdenCompraAdjunto.Crear(
            Guid.NewGuid(), oc.Id, "a.pdf", "application/pdf", [1, 2], OrdenCompraTestFactory.CreadorId);
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);
        _repoMock.Setup(r => r.GetAdjuntoAsync(oc.Id, adjunto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(adjunto);

        await Task.CompletedTask;
        return () => _handler.Handle(new EliminarAdjuntoOrdenCompraCommand(oc.Id, adjunto.Id), CancellationToken.None);
    }

    [Fact]
    public async Task Should_Fail_When_Orden_Aprobada()
    {
        // Integridad documental: los respaldos que sustentaron la decisión de jefatura
        // no se pueden eliminar después de la aprobación.
        var act = await ActEliminar(OrdenCompraTestFactory.Aprobada());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*eliminar respaldos*");
        _repoMock.Verify(r => r.RemoveAdjuntoAsync(It.IsAny<OrdenCompraAdjunto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Fail_When_Orden_Enviada()
    {
        var act = await ActEliminar(OrdenCompraTestFactory.Enviada());

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repoMock.Verify(r => r.RemoveAdjuntoAsync(It.IsAny<OrdenCompraAdjunto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Fail_When_Orden_Anulada()
    {
        var act = await ActEliminar(OrdenCompraTestFactory.Anulada());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_Allow_Eliminar_When_PendienteAprobacion()
    {
        // Todavía no hay decisión de jefatura: los respaldos siguen siendo editables.
        var act = await ActEliminar(OrdenCompraTestFactory.Pendiente());

        await act.Should().NotThrowAsync();
        _repoMock.Verify(r => r.RemoveAdjuntoAsync(It.IsAny<OrdenCompraAdjunto>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
