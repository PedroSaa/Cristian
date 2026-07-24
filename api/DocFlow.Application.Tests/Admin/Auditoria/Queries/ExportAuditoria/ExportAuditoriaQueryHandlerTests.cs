using DocFlow.Application.Admin.Auditoria.DTOs;
using DocFlow.Application.Admin.Auditoria.Interfaces;
using DocFlow.Application.Admin.Auditoria.Queries.ExportAuditoria;
using DocFlow.Application.Common;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Auditoria.Queries.ExportAuditoria;

public class ExportAuditoriaQueryHandlerTests
{
    private readonly Mock<IAuditoriaRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaCsvService> _csvMock = new(MockBehavior.Strict);
    private readonly ExportAuditoriaQueryHandler _handler;

    public ExportAuditoriaQueryHandlerTests()
    {
        _handler = new ExportAuditoriaQueryHandler(_repoMock.Object, _csvMock.Object);
    }

    private static List<AuditoriaQueryResult> ToResults(List<RegistroAuditoria> registros)
        => registros.Select(r => new AuditoriaQueryResult(r, "Test User")).ToList();

    /// <summary>
    /// Sets up the repository mock to return the given items for ANY GetPaginatedAsync call.
    /// Both the count check (pageSize:1) and the data fetch (pageSize:MaxExportRows) will match.
    /// </summary>
    private void SetupRepoReturns(List<RegistroAuditoria> registros)
    {
        _repoMock
            .Setup(r => r.GetPaginatedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>()))
            .ReturnsAsync((ToResults(registros), registros.Count));
    }

    [Fact]
    public async Task Handle_Should_Return_CsvBytes_When_DataExists()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var registros = new List<RegistroAuditoria>
        {
            RegistroAuditoria.Crear(usuarioId, "Login", "Usuario", "usr-1", "Inicio de sesión"),
            RegistroAuditoria.Crear(usuarioId, "Logout", "Usuario", "usr-2", "Cierre de sesión"),
        };

        var expectedBytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

        SetupRepoReturns(registros);

        _csvMock
            .Setup(s => s.GenerateCsv(It.IsAny<IReadOnlyList<RegistroAuditoriaDto>>()))
            .Returns(expectedBytes);

        var query = new ExportAuditoriaQuery(UsuarioId: usuarioId, Desde: DateTime.UtcNow.AddDays(-1));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedBytes);
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_Csv_When_No_Data()
    {
        // Arrange
        var expectedBytes = Array.Empty<byte>();

        SetupRepoReturns(new List<RegistroAuditoria>());

        _csvMock
            .Setup(s => s.GenerateCsv(It.IsAny<IReadOnlyList<RegistroAuditoriaDto>>()))
            .Returns(expectedBytes);

        var query = new ExportAuditoriaQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Pass_Accion_Filter_To_Repository()
    {
        // Arrange
        SetupRepoReturns(new List<RegistroAuditoria>());

        _csvMock
            .Setup(s => s.GenerateCsv(It.IsAny<IReadOnlyList<RegistroAuditoriaDto>>()))
            .Returns(Array.Empty<byte>());

        var query = new ExportAuditoriaQuery(Accion: "CrearUsuario");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        // Verify that at least one call was made with the correct Accion filter
        _repoMock.Verify(r => r.GetPaginatedAsync(
            It.IsAny<int>(), It.IsAny<int>(), null, null, "CrearUsuario", null, null, null), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Exceeds_Limit()
    {
        // Arrange
        var manyRegistros = Enumerable.Range(0, 10_001)
            .Select(i => RegistroAuditoria.Crear(Guid.NewGuid(), "Login", "Usuario", $"usr-{i}", null))
            .ToList();

        var results = manyRegistros.Select(r => new AuditoriaQueryResult(r, "Test User")).ToList();

        _repoMock
            .Setup(r => r.GetPaginatedAsync(
                1, 1, null, null, null, null, null, null))
            .ReturnsAsync((results.AsReadOnly(), manyRegistros.Count));

        var query = new ExportAuditoriaQuery();

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*10000*");
    }

    [Fact]
    public async Task Handle_Should_Not_Throw_When_At_Exact_Limit()
    {
        // Arrange
        var limitRegistros = Enumerable.Range(0, 10_000)
            .Select(i => RegistroAuditoria.Crear(Guid.NewGuid(), "Login", "Usuario", $"usr-{i}", null))
            .ToList();

        var results = limitRegistros.Select(r => new AuditoriaQueryResult(r, "Test User")).ToList();

        _repoMock
            .Setup(r => r.GetPaginatedAsync(
                1, 1, null, null, null, null, null, null))
            .ReturnsAsync((results.AsReadOnly(), limitRegistros.Count));

        _repoMock
            .Setup(r => r.GetPaginatedAsync(
                1, ExportAuditoriaQueryHandler.MaxExportRows, null, null, null, null, null, null))
            .ReturnsAsync((results.AsReadOnly(), limitRegistros.Count));

        _csvMock
            .Setup(s => s.GenerateCsv(It.IsAny<IReadOnlyList<RegistroAuditoriaDto>>()))
            .Returns(Array.Empty<byte>());

        var query = new ExportAuditoriaQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }
}
