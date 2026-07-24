using DocFlow.Application.OrdenesCompra.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Queries.BuscarOrdenMercadoPublico;

public record BuscarOrdenMercadoPublicoQuery(string Codigo) : IRequest<MercadoPublicoOrdenDto>;

public class BuscarOrdenMercadoPublicoValidator : AbstractValidator<BuscarOrdenMercadoPublicoQuery>
{
    public BuscarOrdenMercadoPublicoValidator()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código de Mercado Público es obligatorio.")
            .MaximumLength(40).WithMessage("El código de Mercado Público no puede superar los 40 caracteres.");
    }
}

public class BuscarOrdenMercadoPublicoHandler
    : IRequestHandler<BuscarOrdenMercadoPublicoQuery, MercadoPublicoOrdenDto>
{
    private readonly IMercadoPublicoService _mercadoPublico;

    public BuscarOrdenMercadoPublicoHandler(IMercadoPublicoService mercadoPublico)
        => _mercadoPublico = mercadoPublico;

    public async Task<MercadoPublicoOrdenDto> Handle(BuscarOrdenMercadoPublicoQuery q, CancellationToken ct)
    {
        var orden = await _mercadoPublico.BuscarPorCodigoAsync(q.Codigo.Trim(), ct)
            ?? throw new KeyNotFoundException("La orden de compra no existe en Mercado Público.");

        return orden;
    }
}
