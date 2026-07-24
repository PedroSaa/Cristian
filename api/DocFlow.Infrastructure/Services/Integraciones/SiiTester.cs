using DocFlow.Domain.Enums;

namespace DocFlow.Infrastructure.Services.Integraciones;

public class SiiTester : IntegracionHttpTesterBase
{
    public SiiTester(IHttpClientFactory factory) : base(factory) { }

    public override TipoIntegracion Tipo => TipoIntegracion.SII;
}
