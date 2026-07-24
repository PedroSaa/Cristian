using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Queries.ListOrdenesCompra;

public record ListOrdenesCompraQuery(
    string? Estado = null,
    Guid? ProveedorId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PaginatedOrdenesCompraResponse>;

public class ListOrdenesCompraValidator : AbstractValidator<ListOrdenesCompraQuery>
{
    public ListOrdenesCompraValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("El tamaño de página debe estar entre 1 y 100.");

        RuleFor(x => x.Estado)
            .Must(estado => Enum.TryParse<EstadoOrdenCompra>(estado, ignoreCase: true, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Estado))
            .WithMessage("El estado indicado no es válido.");
    }
}

public class ListOrdenesCompraHandler : IRequestHandler<ListOrdenesCompraQuery, PaginatedOrdenesCompraResponse>
{
    private readonly IOrdenCompraRepository _repo;

    public ListOrdenesCompraHandler(IOrdenCompraRepository repo) => _repo = repo;

    public async Task<PaginatedOrdenesCompraResponse> Handle(ListOrdenesCompraQuery q, CancellationToken ct)
    {
        EstadoOrdenCompra? estado = null;
        if (!string.IsNullOrWhiteSpace(q.Estado)
            && Enum.TryParse<EstadoOrdenCompra>(q.Estado, ignoreCase: true, out var parsed))
        {
            estado = parsed;
        }

        var (items, total) = await _repo.GetListAsync(
            estado,
            q.ProveedorId,
            q.Search,
            q.Page,
            q.PageSize,
            ct);

        var dtos = items.Select(row => new OrdenCompraListItemDto(
            row.Id,
            row.Numero,
            row.ProveedorId,
            row.ProveedorNombre,
            row.Fecha.ToString("o"),
            row.Moneda,
            row.Neto,
            row.Iva,
            row.Total,
            row.Estado.ToString(),
            row.CreadoEn.ToString("o"),
            row.CodigoMercadoPublico
        )).ToList();

        var totalPaginas = total == 0 ? 1 : (int)Math.Ceiling((double)total / q.PageSize);

        return new PaginatedOrdenesCompraResponse(dtos, total, q.Page, totalPaginas);
    }
}
