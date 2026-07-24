using DocFlow.Domain.Enums;

namespace DocFlow.Infrastructure.Services.Integraciones;

public class DocDigitalTester : IntegracionHttpTesterBase
{
    public DocDigitalTester(IHttpClientFactory factory) : base(factory) { }

    public override TipoIntegracion Tipo => TipoIntegracion.DocDigital;
}
