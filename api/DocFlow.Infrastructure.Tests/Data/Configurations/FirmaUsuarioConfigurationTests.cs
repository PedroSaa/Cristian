using DocFlow.Domain.Entities;
using DocFlow.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DocFlow.Infrastructure.Tests.Data.Configurations;

public class FirmaUsuarioConfigurationTests
{
    private static DbContextOptions<DocFlowDbContext> CreateInMemoryOptions()
        => new DbContextOptionsBuilder<DocFlowDbContext>()
            .UseInMemoryDatabase($"FirmaUsuarioConfigTest_{Guid.NewGuid()}")
            .Options;

    [Fact]
    public void FirmaUsuario_ShouldMapToSnakeCaseTable()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var entityType = context.Model.FindEntityType(typeof(FirmaUsuario));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("firmas_usuario");
        entityType.FindProperty(nameof(FirmaUsuario.UsuarioId))!.GetColumnName().Should().Be("usuario_id");
        entityType.FindProperty(nameof(FirmaUsuario.ImagenFirma))!.GetColumnName().Should().Be("imagen_firma");
        entityType.FindProperty(nameof(FirmaUsuario.ContentType))!.GetColumnName().Should().Be("content_type");
        entityType.FindProperty(nameof(FirmaUsuario.ClaveCifrada))!.GetColumnName().Should().Be("clave_cifrada");
        entityType.FindProperty(nameof(FirmaUsuario.Sigla))!.GetColumnName().Should().Be("sigla");
        entityType.FindProperty(nameof(FirmaUsuario.CreadoEn))!.GetColumnName().Should().Be("creado_en");
        entityType.FindProperty(nameof(FirmaUsuario.ActualizadoEn))!.GetColumnName().Should().Be("actualizado_en");
    }

    [Fact]
    public void FirmaUsuario_ShouldMapImagenAsBytea()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var property = context.Model.FindEntityType(typeof(FirmaUsuario))!
            .FindProperty(nameof(FirmaUsuario.ImagenFirma))!;

        // GetColumnType() needs a relational type mapping (unavailable under the InMemory provider),
        // so assert the configured relational column-type annotation directly.
        property.FindAnnotation("Relational:ColumnType")!.Value.Should().Be("bytea");
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void FirmaUsuario_ShouldMapSiglaAsNullableWithMaxLength50()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var property = context.Model.FindEntityType(typeof(FirmaUsuario))!
            .FindProperty(nameof(FirmaUsuario.Sigla))!;

        property.GetMaxLength().Should().Be(50);
        property.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void FirmaUsuario_ShouldMapClaveCifradaAsNullable()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var property = context.Model.FindEntityType(typeof(FirmaUsuario))!
            .FindProperty(nameof(FirmaUsuario.ClaveCifrada))!;

        property.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void FirmaUsuario_ShouldHaveUniqueIndexOnUsuarioId()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var index = context.Model.FindEntityType(typeof(FirmaUsuario))!
            .GetIndexes()
            .Single(i => i.Properties.Any(p => p.Name == nameof(FirmaUsuario.UsuarioId)));

        index.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void FirmaUsuario_ShouldCascadeDeleteFromSeUsuari()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var foreignKey = context.Model.FindEntityType(typeof(FirmaUsuario))!
            .GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(SeUsuari));

        foreignKey.Properties.Should().ContainSingle(p => p.Name == nameof(FirmaUsuario.UsuarioId));
        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }
}
