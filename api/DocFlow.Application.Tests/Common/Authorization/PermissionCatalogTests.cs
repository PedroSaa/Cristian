using DocFlow.Application.Common.Authorization;
using FluentAssertions;
using Xunit;

namespace DocFlow.Application.Tests.Common.Authorization;

public class PermissionCatalogTests
{
    [Fact]
    public void Export_ReturnsCanonicalPermissionsWithAliasMetadata()
    {
        var catalog = PermissionCatalog.Export();

        catalog.Should().HaveCount(64);
        catalog.Should().ContainSingle(p =>
            p.Name == "admin.config.ver" &&
            p.Group == "admin" &&
            p.Label == "Ver configuración" &&
            p.Aliases.Contains("admin.configuracion"));
        catalog.Should().ContainSingle(p =>
            p.Name == "admin.catalogos.ver" &&
            p.Group == "admin" &&
            p.Label == "Ver catálogos" &&
            p.Aliases.Contains("admin.catalogos"));
        catalog.Should().ContainSingle(p =>
            p.Name == "admin.numeracion.ver" &&
            p.Group == "admin" &&
            p.Label == "Ver numeración");
        catalog.Should().ContainSingle(p =>
            p.Name == "admin.plantillasNumeracion.editar" &&
            p.Group == "admin" &&
            p.Label == "Editar plantillas de numeración");
        catalog.Should().ContainSingle(p =>
            p.Name == "admin.respaldos.descargar" &&
            p.Group == "admin" &&
            p.Label == "Descargar respaldos");
        catalog.Should().ContainSingle(p =>
            p.Name == "admin.respaldos.restaurar" &&
            p.Group == "admin" &&
            p.Label == "Restaurar respaldos");
        catalog.Should().ContainSingle(p =>
            p.Name == "admin.respaldos.configurar" &&
            p.Group == "admin" &&
            p.Label == "Configurar respaldos");
    }

    [Fact]
    public void Seed_ReturnsCanonicalAndLegacyDefinitionsForBackendSeeding()
    {
        var catalog = PermissionCatalog.Seed();

        catalog.Should().HaveCount(69);
        catalog.Should().ContainSingle(p => p.Name == "admin.configuracion" && p.IsLegacyAlias && p.CanonicalName == "admin.config.ver");
        catalog.Should().ContainSingle(p => p.Name == "admin.respaldos" && p.IsLegacyAlias && p.CanonicalName == "admin.respaldos.ver");
    }

    [Theory]
    [InlineData("admin.configuracion", "admin.config.ver")]
    [InlineData("admin.respaldos", "admin.respaldos.ver")]
    [InlineData("admin.catalogos", "admin.catalogos.ver")]
    public void TryNormalize_MapsLegacyAliasesToCanonicalPermissionNames(string legacyName, string canonicalName)
    {
        var result = PermissionCatalog.TryNormalize(legacyName, out var normalized);

        result.Should().BeTrue();
        normalized.Should().Be(canonicalName);
    }
}
