using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using DocFlow.Infrastructure.Data;
using DocFlow.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Auditoria.Repositories;

public class AuditoriaRepositoryTests
{
    private static DocFlowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DocFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DocFlowDbContext(options);
    }

    private static AuditoriaRepository CreateRepo(DocFlowDbContext db, string? ip = null, string? ua = null)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(c => c.IpAddress).Returns(ip);
        currentUser.SetupGet(c => c.UserAgent).Returns(ua);
        return new AuditoriaRepository(db, currentUser.Object);
    }

    private static async Task<DocFlowDbContext> SeedAsync()
    {
        var db = CreateDbContext();

        var personal = SePersonal.Crear("ADMIN", "Admin", "Istrator", "", "11111111-1", "admin@docflow.cl");
        var usuari = SeUsuari.Crear(Guid.NewGuid(), "ADMIN", "hash", estadoCuenta: true);
        usuari.VincularPersonal(personal);

        var otroPersonal = SePersonal.Crear("JPEREZ", "Juan", "Pérez", "González", "22222222-2", "jperez@docflow.cl");
        var otroUsuari = SeUsuari.Crear(Guid.NewGuid(), "JPEREZ", "hash", estadoCuenta: true);
        otroUsuari.VincularPersonal(otroPersonal);

        db.SePersonales.AddRange(personal, otroPersonal);
        db.SeUsuaris.AddRange(usuari, otroUsuari);

        db.RegistrosAuditoria.AddRange(
            RegistroAuditoria.Crear(usuari.UsuarioId, "Login", "Usuario", usuari.UsuarioId.ToString(), "Inicio de sesión", "192.168.1.1", "Chrome"),
            RegistroAuditoria.Crear(usuari.UsuarioId, "Logout", "Usuario", usuari.UsuarioId.ToString(), "Cierre de sesión", "10.0.0.1", "Firefox"),
            RegistroAuditoria.Crear(otroUsuari.UsuarioId, "CrearUsuario", "Usuario", "usr-3", "Creación manual", null, null),
            RegistroAuditoria.Crear(usuari.UsuarioId, "EliminarUsuario", "Config", "cfg-1", "Eliminado", "192.168.1.1", "Chrome"),
            RegistroAuditoria.Crear(otroUsuari.UsuarioId, "Login", "Config", "cfg-2", "Config actualizada", "10.0.0.2", "Edge")
        );

        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task GetValoresFiltroAsync_Should_Return_DistinctActions()
    {
        // Arrange
        var db = await SeedAsync();
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetValoresFiltroAsync();

        // Assert
        result.Acciones.Should().BeEquivalentTo(
            new[] { "CrearUsuario", "EliminarUsuario", "Login", "Logout" },
            opts => opts.WithStrictOrdering());
    }

    [Fact]
    public async Task GetValoresFiltroAsync_Should_Return_DistinctEntities()
    {
        // Arrange
        var db = await SeedAsync();
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetValoresFiltroAsync();

        // Assert
        result.Entidades.Should().BeEquivalentTo(
            new[] { "Config", "Usuario" },
            opts => opts.WithStrictOrdering());
    }

    [Fact]
    public async Task GetValoresFiltroAsync_WhenEmpty_Should_ReturnEmptyLists()
    {
        // Arrange
        var db = CreateDbContext();
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetValoresFiltroAsync();

        // Assert
        result.Acciones.Should().BeEmpty();
        result.Entidades.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPaginatedAsync_WithUsuarioNombre_Should_Filter()
    {
        // Arrange
        var db = await SeedAsync();

        var admin = await db.SeUsuaris.Include(u => u.Personal).FirstAsync(u => u.Usucod == "ADMIN");
        var adminName = $"{admin.Personal!.Nombres} {admin.Personal.ApellidoPaterno}";

        var repo = CreateRepo(db);

        // Act
        var (items, total) = await repo.GetPaginatedAsync(
            1, 10, null, null, null, null, null, "Admin");

        // Assert
        total.Should().Be(3); // The 3 records belonging to ADMIN user
        items.Should().HaveCount(3);
        items.Should().AllSatisfy(r => r.UsuarioNombre.Should().Be(adminName));
    }

    [Fact]
    public async Task GetPaginatedAsync_WithUsuarioNombreNoMatch_Should_ReturnEmpty()
    {
        // Arrange
        var db = await SeedAsync();
        var repo = CreateRepo(db);

        // Act
        var (items, total) = await repo.GetPaginatedAsync(
            1, 10, null, null, null, null, null, "NonExistentUser");

        // Assert
        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPaginatedAsync_Should_Include_UsuarioNombre()
    {
        // Arrange
        var db = await SeedAsync();

        var admin = await db.SeUsuaris.Include(u => u.Personal).FirstAsync(u => u.Usucod == "ADMIN");
        var adminName = $"{admin.Personal!.Nombres} {admin.Personal.ApellidoPaterno}";

        var repo = CreateRepo(db);

        // Act
        var (items, _) = await repo.GetPaginatedAsync(
            1, 10, null, null, null, null, null, null);

        // Assert — all ADMIN records have the name populated
        var adminItems = items.Where(i => i.Registro.UsuarioId == admin.UsuarioId).ToList();
        adminItems.Should().NotBeEmpty();
        adminItems.Should().AllSatisfy(i => i.UsuarioNombre.Should().Be(adminName));
    }

    [Fact]
    public async Task GetByIdWithUserAsync_Should_Return_WithUsuarioNombre()
    {
        // Arrange
        var db = await SeedAsync();
        var registro = await db.RegistrosAuditoria.FirstAsync();
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetByIdWithUserAsync(registro.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Registro.Id.Should().Be(registro.Id);
        result.UsuarioNombre.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdWithUserAsync_WhenNotFound_Should_ReturnNull()
    {
        // Arrange
        var db = await SeedAsync();
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetByIdWithUserAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_Should_Enrich_Ip_And_UserAgent_From_Context_When_Missing()
    {
        var db = CreateDbContext();
        var repo = CreateRepo(db, ip: "5.6.7.8", ua: "Mozilla/Test");
        // Registro creado sin IP/UA (como hacen los handlers admin).
        var registro = RegistroAuditoria.Crear(Guid.NewGuid(), "ActualizarUsuario", "Usuario", "x", "detalle");

        await repo.AddAsync(registro);

        var saved = await db.RegistrosAuditoria.FirstAsync();
        saved.DireccionIp.Should().Be("5.6.7.8");
        saved.UserAgent.Should().Be("Mozilla/Test");
    }

    [Fact]
    public async Task AddAsync_Should_Not_Override_Ip_And_UserAgent_When_Already_Set()
    {
        var db = CreateDbContext();
        var repo = CreateRepo(db, ip: "5.6.7.8", ua: "Mozilla/Test");
        // Registro que YA trae IP/UA (como login o respaldos): no debe pisarse.
        var registro = RegistroAuditoria.Crear(
            Guid.NewGuid(), "Login", "Usuario", "x", "detalle", "1.1.1.1", "Chrome");

        await repo.AddAsync(registro);

        var saved = await db.RegistrosAuditoria.FirstAsync();
        saved.DireccionIp.Should().Be("1.1.1.1");
        saved.UserAgent.Should().Be("Chrome");
    }
}
