using DocFlow.Application.Admin.Integraciones.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Integraciones.Commands.ProbarConexion;

public class ProbarConexionIntegracionCommandHandler
    : IRequestHandler<ProbarConexionIntegracionCommand, IntegracionTestResultDto>
{
    private readonly IIntegracionRepository _repo;
    private readonly IEnumerable<IIntegracionTester> _testers;
    private readonly IConfiguration _config;
    private readonly ILogger<ProbarConexionIntegracionCommandHandler> _logger;

    public ProbarConexionIntegracionCommandHandler(
        IIntegracionRepository repo,
        IEnumerable<IIntegracionTester> testers,
        IConfiguration config,
        ILogger<ProbarConexionIntegracionCommandHandler> logger)
    {
        _repo = repo;
        _testers = testers;
        _config = config;
        _logger = logger;
    }

    public async Task<IntegracionTestResultDto> Handle(
        ProbarConexionIntegracionCommand request,
        CancellationToken cancellationToken)
    {
        // Step 1: entity lookup — throws if missing (controller maps → 404)
        var integracion = await _repo.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Integración {request.Id} no encontrada.");

        // Step 2: empty-BaseUrl guard — no network call
        if (string.IsNullOrWhiteSpace(integracion.BaseUrl))
        {
            return new IntegracionTestResultDto(false, "Configurá la URL base antes de probar la conexión.", null);
        }

        // Step 3: resolve tester by Tipo — no throw for unsupported types
        var tester = _testers.FirstOrDefault(t => t.Tipo == integracion.Tipo);
        if (tester is null)
        {
            return new IntegracionTestResultDto(false, "Prueba no soportada para este tipo de integración.", null);
        }

        // Step 4: linked CTS with configurable timeout
        var seconds = _config.GetValue<int?>("Integraciones:TestTimeoutSeconds") ?? 10;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(seconds));

        // Step 5: run the tester
        try
        {
            var result = await tester.TestAsync(integracion, cts.Token);
            // Step 6: map to DTO
            return new IntegracionTestResultDto(result.Success, result.Mensaje, result.LatencyMs);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Our linked CTS fired — timeout
            _logger.LogWarning("ProbarConexion timeout after {Seconds}s for integración {Id}", seconds, request.Id);
            return new IntegracionTestResultDto(false, $"La prueba excedió el tiempo límite ({seconds}s).", null);
        }
        catch (OperationCanceledException)
        {
            // Client cancelled the request
            return new IntegracionTestResultDto(false, "La solicitud fue cancelada.", null);
        }
    }
}
