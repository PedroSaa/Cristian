using DocFlow.Domain.Enums;

namespace DocFlow.Infrastructure.Services.Integraciones;

public class FirmaGobTester : IntegracionHttpTesterBase
{
    public FirmaGobTester(IHttpClientFactory factory) : base(factory) { }

    public override TipoIntegracion Tipo => TipoIntegracion.FirmaGob;
}
