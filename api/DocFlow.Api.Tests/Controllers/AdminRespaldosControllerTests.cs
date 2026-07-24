using System.Reflection;
using DocFlow.Api.Controllers;
using DocFlow.Api.Filters;
using DocFlow.Application.Admin.Respaldos.Commands.RestoreRespaldo;
using DocFlow.Application.Admin.Respaldos.Commands.TriggerRespaldo;
using DocFlow.Application.Admin.Respaldos.Commands.UpsertRespaldoConfig;
using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Application.Admin.Respaldos.Queries.GetRespaldoById;
using DocFlow.Application.Admin.Respaldos.Queries.GetRespaldoConfig;
using DocFlow.Application.Admin.Respaldos.Queries.GetRestoreLogs;
using DocFlow.Application.Admin.Respaldos.Queries.ListRespaldos;
using DocFlow.Application.Common.Authorization;
using DocFlow.Domain.Enums;
using DocFlow.Infrastructure.Configuration;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class AdminRespaldosControllerTests : IDisposable
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AdminRespaldosController _controller;
    private readonly string _tempDir;
    private readonly BackupSettings _settings;

    public AdminRespaldosControllerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"DocFlow-AdminRespaldosControllerTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _settings = new BackupSettings { OutputPath = _tempDir };
        _controller = new AdminRespaldosController(_mediatorMock.Object, _settings);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task List_Should_Return_200_Ok_With_RespaldoList()
    {
        var dtos = new List<RespaldoDto>
        {
            new(Guid.NewGuid(), "Respaldo-001", DateTime.UtcNow, 1024, EstadoRespaldo.Completado, "/respaldos/stub"),
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ListRespaldosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<RespaldoDto>)dtos);

        var result = await _controller.List(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<List<RespaldoDto>>().Subject;
        value.Should().HaveCount(1);
        value[0].Nombre.Should().Be("Respaldo-001");
    }

    [Fact]
    public async Task Trigger_Should_Return_202_Accepted_With_Pendiente_RespaldoDto()
    {
        var dto = new RespaldoDto(Guid.NewGuid(), "Respaldo-001", DateTime.UtcNow, 0, EstadoRespaldo.Pendiente, "/respaldos/stub");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TriggerRespaldoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Trigger(CancellationToken.None);

        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(202);
        var value = created.Value.Should().BeOfType<RespaldoDto>().Subject;
        value.Nombre.Should().Be("Respaldo-001");
        value.Estado.Should().Be(EstadoRespaldo.Pendiente);
    }

    // ── GET config endpoint ────────────────────────────────────────────────

    [Fact]
    public async Task GetConfig_Should_Return_200_Ok_With_RespaldoConfigDto()
    {
        var dto = new RespaldoConfigDto(
            Guid.NewGuid(), 60, true, 10, 30,
            "./Respaldos", 30, DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetRespaldoConfigQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetConfig(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<RespaldoConfigDto>().Subject;
        value.IntervaloMinutos.Should().Be(60);
        value.Habilitado.Should().BeTrue();
    }

    // ── PUT config endpoint ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateConfig_Should_Return_200_Ok_With_Updated_RespaldoConfigDto()
    {
        var updatedDto = new RespaldoConfigDto(
            Guid.NewGuid(), 120, false, 5, 7,
            "/backups", 60, DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpsertRespaldoConfigCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        var cmd = new UpsertRespaldoConfigCommand(
            IntervaloMinutos: 120, Habilitado: false,
            MaxBackupCount: 5, RetentionDays: 7,
            OutputPath: "/backups", TimeoutMinutos: 60);

        var result = await _controller.UpdateConfig(cmd, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<RespaldoConfigDto>().Subject;
        value.IntervaloMinutos.Should().Be(120);
        value.Habilitado.Should().BeFalse();
    }

    [Fact]
    public void Controller_Should_Have_AuthorizeAttribute_Without_RoleRestriction()
    {
        var attr = typeof(AdminRespaldosController)
            .GetCustomAttributes(inherit: true)
            .OfType<AuthorizeAttribute>()
            .Where(a => a.GetType() == typeof(AuthorizeAttribute))
            .ToList();

        attr.Should().ContainSingle();
        attr[0].Roles.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public void Controller_Should_Have_RequireMfaAttribute()
    {
        var attr = typeof(AdminRespaldosController)
            .GetCustomAttributes(typeof(RequireMfaAttribute), inherit: true)
            .Cast<RequireMfaAttribute>()
            .ToList();

        attr.Should().NotBeEmpty("Admin endpoints require MFA enforcement");
    }

    // ── Download endpoint ─────────────────────────────────────────────────

    [Fact]
    public async Task Download_Should_Return_404_When_NotFound()
    {
        var id = Guid.NewGuid();

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetRespaldoByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.Download(id, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [InlineData(EstadoRespaldo.Pendiente)]
    [InlineData(EstadoRespaldo.EnProceso)]
    [InlineData(EstadoRespaldo.Fallido)]
    public async Task Download_Should_Return_400_When_NotCompleted(EstadoRespaldo estado)
    {
        var id = Guid.NewGuid();

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetRespaldoByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RespaldoDto(id, "test", DateTime.UtcNow, 0, estado, "/ruta/falsa"));

        var result = await _controller.Download(id, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Download_Should_Return_200_PhysicalFileResult_When_Completed()
    {
        var id = Guid.NewGuid();
        var backupPath = Path.Combine(_tempDir, "Respaldo-20260516.sql.gz");
        File.WriteAllText(backupPath, "backup");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetRespaldoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RespaldoDto(
                id,
                "Respaldo-20260516",
                DateTime.UtcNow,
                2048,
                EstadoRespaldo.Completado,
                backupPath));

        var result = await _controller.Download(id, CancellationToken.None);

        var fileResult = result.Should().BeOfType<PhysicalFileResult>().Subject;
        fileResult.FileDownloadName.Should().Be("Respaldo-20260516.sql.gz");
        fileResult.ContentType.Should().Be("application/octet-stream");
    }

    // ── Download path traversal fix ────────────────────────────────────────

    [Fact]
    public async Task Download_Should_Return_400_When_Path_Resolves_Outside_OutputPath()
    {
        var id = Guid.NewGuid();
        var maliciousDto = new RespaldoDto(
            id,
            "respaldo-trampa",
            DateTime.UtcNow,
            2048,
            EstadoRespaldo.Completado,
            Path.Combine(Path.GetPathRoot(_tempDir)!, "outside-backup.sql.gz"));

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetRespaldoByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(maliciousDto);

        var result = await _controller.Download(id, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Download_Should_Return_400_When_Path_Is_Sibling_Prefix_Of_OutputPath()
    {
        var id = Guid.NewGuid();
        var maliciousDto = new RespaldoDto(
            id,
            "respaldo-trampa",
            DateTime.UtcNow,
            2048,
            EstadoRespaldo.Completado,
            _tempDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + "Mal" + Path.DirectorySeparatorChar + "backup.sql.gz");

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetRespaldoByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(maliciousDto);

        var result = await _controller.Download(id, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Download_Should_Return_200_When_Path_Is_Within_OutputPath()
    {
        var id = Guid.NewGuid();
        var safePath = Path.Combine(_tempDir, "backup-20260516.sql.gz");
        File.WriteAllText(safePath, "backup");
        var dto = new RespaldoDto(
            id,
            "backup-20260516",
            DateTime.UtcNow,
            2048,
            EstadoRespaldo.Completado,
            safePath);

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetRespaldoByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Download(id, CancellationToken.None);

        var fileResult = result.Should().BeOfType<PhysicalFileResult>().Subject;
        fileResult.FileName.Should().Be(safePath);
        fileResult.FileDownloadName.Should().Be("backup-20260516.sql.gz");
    }

    [Fact]
    public async Task Download_Should_Return_400_When_File_Is_Missing()
    {
        var id = Guid.NewGuid();
        var missingPath = Path.Combine(_tempDir, "missing.sql.gz");
        var dto = new RespaldoDto(
            id,
            "missing",
            DateTime.UtcNow,
            2048,
            EstadoRespaldo.Completado,
            missingPath);

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetRespaldoByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Download(id, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Restore endpoint ────────────────────────────────────────────────────

    [Fact]
    public async Task Restore_Should_Return_400_When_Header_Missing()
    {
        var id = Guid.NewGuid();

        var result = await _controller.Restore(id, confirmName: null!, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Restore_Should_Return_400_When_Header_Does_Not_Match_Respaldo_Name()
    {
        var id = Guid.NewGuid();
        var dto = new RespaldoDto(
            id, "RealBackup", DateTime.UtcNow, 2048,
            EstadoRespaldo.Completado, "/ruta/falsa");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetRespaldoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Restore(id, confirmName: "WrongName", CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Restore_Should_Return_202_When_Header_Matches()
    {
        var id = Guid.NewGuid();
        var backupPath = Path.Combine(_tempDir, "backup.sql.gz");
        File.WriteAllText(backupPath, "backup");
        var dto = new RespaldoDto(
            id, "Respaldo-20260516", DateTime.UtcNow, 2048,
            EstadoRespaldo.Completado, backupPath);

        var restoreDto = new RestoreLogDto(
            Guid.NewGuid(), id, DateTime.UtcNow, null,
            EstadoRestore.Pendiente, null);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetRespaldoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RestoreRespaldoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(restoreDto);

        var result = await _controller.Restore(id, confirmName: "Respaldo-20260516", CancellationToken.None);

        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(202);
        var value = created.Value.Should().BeOfType<RestoreLogDto>().Subject;
        value.RespaldoId.Should().Be(id);
        value.Estado.Should().Be(EstadoRestore.Pendiente);
    }

    [Fact]
    public async Task Restore_Should_Return_400_When_Path_Resolves_Outside_OutputPath()
    {
        var id = Guid.NewGuid();
        var dto = new RespaldoDto(
            id, "Respaldo-20260516", DateTime.UtcNow, 2048,
            EstadoRespaldo.Completado,
            Path.Combine(Path.GetPathRoot(_tempDir)!, "outside-backup.sql.gz"));

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetRespaldoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Restore(id, confirmName: "Respaldo-20260516", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mediatorMock.Verify(x => x.Send(It.IsAny<RestoreRespaldoCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Restore_Should_Return_400_When_File_Is_Missing()
    {
        var id = Guid.NewGuid();
        var dto = new RespaldoDto(
            id, "Respaldo-20260516", DateTime.UtcNow, 2048,
            EstadoRespaldo.Completado,
            Path.Combine(_tempDir, "missing.sql.gz"));

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetRespaldoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Restore(id, confirmName: "Respaldo-20260516", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mediatorMock.Verify(x => x.Send(It.IsAny<RestoreRespaldoCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── RestoreLogs endpoint ────────────────────────────────────────────────

    [Fact]
    public async Task GetRestoreLogs_Should_Return_200_Ok_With_RestoreLogList()
    {
        var id = Guid.NewGuid();
        var logs = new List<RestoreLogDto>
        {
            new(Guid.NewGuid(), id, DateTime.UtcNow, DateTime.UtcNow,
                EstadoRestore.Completado, null),
            new(Guid.NewGuid(), id, DateTime.UtcNow, null,
                EstadoRestore.Fallido, "Error"),
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetRestoreLogsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<RestoreLogDto>)logs);

        var result = await _controller.GetRestoreLogs(id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<List<RestoreLogDto>>().Subject;
        value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRestoreLogs_Should_Return_Empty_When_No_Logs()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetRestoreLogsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<RestoreLogDto>)new List<RestoreLogDto>());

        var result = await _controller.GetRestoreLogs(Guid.NewGuid(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<List<RestoreLogDto>>().Subject;
        value.Should().BeEmpty();
    }

    [Theory]
    [InlineData("List", "admin.respaldos.ver")]
    [InlineData("Post", "admin.respaldos.crear")]
    [InlineData("Trigger", "admin.respaldos.crear")]
    [InlineData("Download", "admin.respaldos.descargar")]
    [InlineData("Restore", "admin.respaldos.restaurar")]
    [InlineData("GetRestoreLogs", "admin.respaldos.ver")]
    [InlineData("GetConfig", "admin.respaldos.ver")]
    [InlineData("UpdateConfig", "admin.respaldos.configurar")]
    public void Action_Should_Have_HasPermissionAttribute(string actionName, string expectedPermission)
    {
        // Arrange
        var method = typeof(AdminRespaldosController).GetMethod(actionName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Act
        var attr = method!
            .GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true)
            .Cast<HasPermissionAttribute>()
            .FirstOrDefault();

        // Assert
        attr.Should().NotBeNull($"Action {actionName} should have [HasPermission(\"{expectedPermission}\")]");
        attr!.Policy.Should().Be($"Permission:{expectedPermission}");
    }
}
