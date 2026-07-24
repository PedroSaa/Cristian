using DocFlow.Api.Controllers;
using DocFlow.Application.OrdenesCompra.Commands.ActualizarOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.DesvincularMercadoPublicoOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.VincularMercadoPublicoOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.AgregarAdjuntoOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.AnularOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.AprobarOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.CrearOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.EliminarAdjuntoOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.EnviarAprobacionOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.MarcarEnviadaOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.RechazarOrdenCompra;
using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Application.OrdenesCompra.Interfaces;
using DocFlow.Application.OrdenesCompra.Queries.BuscarOrdenMercadoPublico;
using DocFlow.Application.OrdenesCompra.Queries.GetAdjuntoContenido;
using DocFlow.Application.OrdenesCompra.Queries.GetOrdenCompra;
using DocFlow.Application.OrdenesCompra.Queries.GetOrdenCompraPdf;
using DocFlow.Application.OrdenesCompra.Queries.ListOrdenesCompra;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class OrdenesCompraControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly OrdenesCompraController _controller;

    public OrdenesCompraControllerTests()
    {
        _controller = new OrdenesCompraController(_mediatorMock.Object);
    }

    private static OrdenCompraDto SampleDto(string estado = "Borrador") => new(
        Id: Guid.NewGuid(),
        Numero: "OC-2026-0001",
        ProveedorId: Guid.NewGuid(),
        ProveedorNombre: "Acme SA",
        ProveedorRut: "12345678-5",
        Fecha: DateTime.UtcNow.ToString("o"),
        Moneda: "CLP",
        FormaPago: "Transferencia",
        PlazoEntrega: null,
        LugarEntrega: null,
        Observaciones: null,
        Neto: 1000m,
        Iva: 190m,
        Total: 1190m,
        Estado: estado,
        CreadoPor: Guid.NewGuid(),
        CreadoEn: DateTime.UtcNow.ToString("o"),
        ActualizadoEn: DateTime.UtcNow.ToString("o"),
        AprobadoPor: null,
        AprobadoEn: null,
        ComentarioAprobacion: null,
        MotivoAnulacion: null,
        Items: [new OrdenCompraItemDto(Guid.NewGuid(), 1, "Item", 1m, 1000m, 1000m)],
        Adjuntos: []);

    // ── POST Create ──

    [Fact]
    public async Task Create_Should_Return_201_With_Dto()
    {
        var dto = SampleDto();
        _mediatorMock.Setup(m => m.Send(It.IsAny<CrearOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Create(
            new CrearOrdenCompraRequest(dto.ProveedorId, DateTime.UtcNow), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(OrdenesCompraController.GetById));
        created.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task Create_Should_Return_400_On_ValidationException()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CrearOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FluentValidation.ValidationException("El proveedor indicado no existe."));

        var result = await _controller.Create(
            new CrearOrdenCompraRequest(Guid.NewGuid(), DateTime.UtcNow), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_Should_Return_Clean_Field_Messages_Without_ValidationFailed_Wrapper()
    {
        // ValidationException.Message con failures trae el prefijo técnico
        // "Validation failed: -- Prop: ..." — al usuario le llega solo el mensaje de campo.
        _mediatorMock.Setup(m => m.Send(It.IsAny<CrearOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FluentValidation.ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    "Items[0].Cantidad", "La cantidad del ítem no puede superar 9.999.999.999,9999."),
                new FluentValidation.Results.ValidationFailure(
                    "Items[0].PrecioUnitario", "Ingrese un precio válido."),
            }));

        var result = await _controller.Create(
            new CrearOrdenCompraRequest(Guid.NewGuid(), DateTime.UtcNow), CancellationToken.None);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var mensaje = bad.Value!.GetType().GetProperty("mensaje")!.GetValue(bad.Value) as string;
        mensaje.Should().Be("La cantidad del ítem no puede superar 9.999.999.999,9999. Ingrese un precio válido.");
        mensaje.Should().NotContain("Validation failed");
    }

    // ── PUT Update ──

    [Fact]
    public async Task Update_Should_Return_200_With_Dto()
    {
        var dto = SampleDto();
        _mediatorMock.Setup(m => m.Send(It.IsAny<ActualizarOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Update(dto.Id,
            new ActualizarOrdenCompraRequest(dto.ProveedorId, DateTime.UtcNow), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Should_Return_404_When_Missing()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<ActualizarOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("La orden de compra no existe."));

        var result = await _controller.Update(Guid.NewGuid(),
            new ActualizarOrdenCompraRequest(Guid.NewGuid(), DateTime.UtcNow), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_Should_Return_409_On_State_Conflict()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<ActualizarOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Solo se puede modificar una orden en estado Borrador o Rechazada."));

        var result = await _controller.Update(Guid.NewGuid(),
            new ActualizarOrdenCompraRequest(Guid.NewGuid(), DateTime.UtcNow), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── State transitions ──

    [Fact]
    public async Task EnviarAprobacion_Should_Return_200()
    {
        var dto = SampleDto("PendienteAprobacion");
        _mediatorMock.Setup(m => m.Send(It.IsAny<EnviarAprobacionOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.EnviarAprobacion(dto.Id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task EnviarAprobacion_Should_Return_409_When_No_Items()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<EnviarAprobacionOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("La orden de compra debe tener al menos un ítem."));

        var result = await _controller.EnviarAprobacion(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Aprobar_Should_Return_200()
    {
        var dto = SampleDto("Aprobada");
        _mediatorMock.Setup(m => m.Send(It.IsAny<AprobarOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Aprobar(dto.Id, new AprobarOrdenCompraRequest("OK"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Aprobar_Should_Return_409_On_SelfApproval()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<AprobarOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Un usuario no puede aprobar su propia orden de compra."));

        var result = await _controller.Aprobar(Guid.NewGuid(), null, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task EnviarAprobacion_Should_Return_409_When_Numero_Already_Exists()
    {
        // Índice único de numero violado (p. ej. un admin reseteó el contador a mano):
        // el usuario recibe un 409 accionable, no un 500 crudo.
        var postgresEx = new Npgsql.PostgresException(
            "duplicate key value violates unique constraint \"ix_ordenes_compra_numero\"",
            "ERROR", "ERROR", "23505");
        _mediatorMock.Setup(m => m.Send(It.IsAny<EnviarAprobacionOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("dup", postgresEx));

        var result = await _controller.EnviarAprobacion(Guid.NewGuid(), CancellationToken.None);

        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var mensaje = conflict.Value!.GetType().GetProperty("mensaje")!.GetValue(conflict.Value) as string;
        mensaje.Should().Contain("número").And.Contain("contador");
    }

    [Fact]
    public async Task Aprobar_Should_Return_409_On_ConcurrentUpdate()
    {
        // Choque de concurrencia optimista (xmin): otra transición ganó la carrera.
        _mediatorMock.Setup(m => m.Send(It.IsAny<AprobarOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException(
                "The database operation was expected to affect 1 row(s), but actually affected 0 row(s)."));

        var result = await _controller.Aprobar(Guid.NewGuid(), null, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Rechazar_Should_Return_400_When_Comment_Missing()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<RechazarOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FluentValidation.ValidationException("El comentario de rechazo es obligatorio."));

        var result = await _controller.Rechazar(
            Guid.NewGuid(), new RechazarOrdenCompraRequest(""), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task MarcarEnviada_Should_Return_200()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<MarcarEnviadaOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto("Enviada"));

        var result = await _controller.MarcarEnviada(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Anular_Should_Return_200()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<AnularOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto("Anulada"));

        var result = await _controller.Anular(
            Guid.NewGuid(), new AnularOrdenCompraRequest("Duplicada"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ── Queries ──

    [Fact]
    public async Task List_Should_Return_200_With_Paginated_Results()
    {
        var response = new PaginatedOrdenesCompraResponse(
            [new OrdenCompraListItemDto(Guid.NewGuid(), "OC-2026-0001", Guid.NewGuid(), "Acme SA",
                DateTime.UtcNow.ToString("o"), "CLP", 1000m, 190m, 1190m, "Borrador", DateTime.UtcNow.ToString("o"))],
            1, 1, 1);
        _mediatorMock.Setup(m => m.Send(It.IsAny<ListOrdenesCompraQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.List(ct: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetById_Should_Return_404_When_Missing()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOrdenCompraQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("La orden de compra no existe."));

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPdf_Should_Return_Pdf_File()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOrdenCompraPdfQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrdenCompraPdfDto("orden-compra-OC-2026-0001.pdf", [0x25, 0x50]));

        var result = await _controller.GetPdf(Guid.NewGuid(), CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("application/pdf");
        file.FileDownloadName.Should().Be("orden-compra-OC-2026-0001.pdf");
    }

    [Fact]
    public async Task DownloadAdjunto_Should_Return_File_With_ContentType()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdjuntoContenidoQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrdenCompraAdjuntoContenidoDto("a.pdf", "application/pdf", [1, 2, 3]));

        var result = await _controller.DownloadAdjunto(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("application/pdf");
        file.FileDownloadName.Should().Be("a.pdf");
    }

    // ── Adjuntos ──

    [Fact]
    public async Task AgregarAdjunto_Should_Return_201()
    {
        var adjunto = new OrdenCompraAdjuntoDto(
            Guid.NewGuid(), "a.pdf", "application/pdf", 3, Guid.NewGuid(), DateTime.UtcNow.ToString("o"));
        _mediatorMock.Setup(m => m.Send(It.IsAny<AgregarAdjuntoOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adjunto);

        var result = await _controller.AgregarAdjunto(Guid.NewGuid(),
            new AgregarAdjuntoOrdenCompraRequest("a.pdf", "application/pdf", Convert.ToBase64String([1, 2, 3])),
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.Value.Should().BeEquivalentTo(adjunto);
    }

    [Fact]
    public async Task AgregarAdjunto_Should_Return_400_When_Too_Large()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<AgregarAdjuntoOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FluentValidation.ValidationException("El adjunto no puede superar los 10 MB."));

        var result = await _controller.AgregarAdjunto(Guid.NewGuid(),
            new AgregarAdjuntoOrdenCompraRequest("a.bin", "application/octet-stream", "AAAA"),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task EliminarAdjunto_Should_Return_204()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<EliminarAdjuntoOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.EliminarAdjunto(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task EliminarAdjunto_Should_Return_404_When_Missing()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<EliminarAdjuntoOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("El adjunto no existe."));

        var result = await _controller.EliminarAdjunto(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Mercado Público ──

    private static MercadoPublicoOrdenDto SampleMercadoPublicoDto() => new(
        Codigo: "1123-109-SE13",
        Nombre: "Mantención Áreas verdes",
        Estado: "Aceptada",
        FechaCreacion: "2013-07-05T12:59:15.443",
        CompradorNombre: "INDAP",
        CompradorRut: "61.307.000-1",
        ProveedorNombre: "Proveedora SA",
        ProveedorRut: "7.445.387-2",
        MontoTotal: 110908m,
        Items: [new MercadoPublicoOrdenItemDto("Servicio de jardinería", 1m, 46200m)]);

    [Fact]
    public async Task BuscarMercadoPublico_Should_Return_200_With_Dto()
    {
        var dto = SampleMercadoPublicoDto();
        _mediatorMock.Setup(m => m.Send(It.IsAny<BuscarOrdenMercadoPublicoQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.BuscarMercadoPublico("1123-109-SE13", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task BuscarMercadoPublico_Should_Return_404_When_Not_In_Portal()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<BuscarOrdenMercadoPublicoQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("La orden de compra no existe en Mercado Público."));

        var result = await _controller.BuscarMercadoPublico("0000-0-XX00", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task BuscarMercadoPublico_Should_Return_503_When_Ticket_Missing_Or_Portal_Down()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<BuscarOrdenMercadoPublicoQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("El ticket de acceso de Mercado Público no está configurado."));

        var result = await _controller.BuscarMercadoPublico("1123-109-SE13", CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task VincularMercadoPublico_Should_Return_200_With_Updated_Dto()
    {
        var dto = SampleDto();
        _mediatorMock.Setup(m => m.Send(It.IsAny<VincularMercadoPublicoOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.VincularMercadoPublico(
            dto.Id, new VincularMercadoPublicoOrdenCompraRequest("1123-109-SE13"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task VincularMercadoPublico_Should_Return_400_When_Codigo_Not_In_Portal()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<VincularMercadoPublicoOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FluentValidation.ValidationException("El código indicado no existe en Mercado Público."));

        var result = await _controller.VincularMercadoPublico(
            Guid.NewGuid(), new VincularMercadoPublicoOrdenCompraRequest("0000-0-XX00"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task VincularMercadoPublico_Should_Return_404_When_Orden_Missing()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<VincularMercadoPublicoOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("La orden de compra no existe."));

        var result = await _controller.VincularMercadoPublico(
            Guid.NewGuid(), new VincularMercadoPublicoOrdenCompraRequest("1123-109-SE13"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task VincularMercadoPublico_Should_Return_503_When_Portal_Unavailable()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<VincularMercadoPublicoOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No se pudo conectar con Mercado Público."));

        var result = await _controller.VincularMercadoPublico(
            Guid.NewGuid(), new VincularMercadoPublicoOrdenCompraRequest("1123-109-SE13"), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task DesvincularMercadoPublico_Should_Return_200_With_Updated_Dto()
    {
        var dto = SampleDto();
        _mediatorMock.Setup(m => m.Send(It.IsAny<DesvincularMercadoPublicoOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.DesvincularMercadoPublico(dto.Id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task DesvincularMercadoPublico_Should_Return_404_When_Orden_Missing()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<DesvincularMercadoPublicoOrdenCompraCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("La orden de compra no existe."));

        var result = await _controller.DesvincularMercadoPublico(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
