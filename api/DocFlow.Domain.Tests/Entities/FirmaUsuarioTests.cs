using DocFlow.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace DocFlow.Domain.Tests.Entities;

public class FirmaUsuarioTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly byte[] Imagen = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3];

    [Fact]
    public void Crear_ShouldSetAllProperties()
    {
        var id = Guid.NewGuid();

        var firma = FirmaUsuario.Crear(id, UsuarioId, Imagen, "image/png", "clave-cifrada", "JPG");

        firma.Id.Should().Be(id);
        firma.UsuarioId.Should().Be(UsuarioId);
        firma.ImagenFirma.Should().BeEquivalentTo(Imagen);
        firma.ContentType.Should().Be("image/png");
        firma.ClaveCifrada.Should().Be("clave-cifrada");
        firma.Sigla.Should().Be("JPG");
        firma.CreadoEn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        firma.ActualizadoEn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Crear_ShouldAllowNullClaveAndSigla()
    {
        var firma = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, Imagen, "image/png");

        firma.ClaveCifrada.Should().BeNull();
        firma.Sigla.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_ShouldTreatBlankClaveAsNull(string? clave)
    {
        var firma = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, Imagen, "image/png", clave);

        firma.ClaveCifrada.Should().BeNull();
    }

    [Fact]
    public void Crear_ShouldThrow_WhenUsuarioIdEmpty()
    {
        var act = () => FirmaUsuario.Crear(Guid.NewGuid(), Guid.Empty, Imagen, "image/png");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Crear_ShouldThrow_WhenImagenEmpty()
    {
        var act = () => FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, [], "image/png");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Crear_ShouldThrow_WhenContentTypeMissing(string? contentType)
    {
        var act = () => FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, Imagen, contentType!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Crear_ShouldThrow_WhenSiglaTooLong()
    {
        var sigla = new string('a', FirmaUsuario.SiglaMaxLength + 1);

        var act = () => FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, Imagen, "image/png", null, sigla);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Actualizar_ShouldReplacePayload_AndKeepId()
    {
        var firma = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, Imagen, "image/png", "vieja", "OLD");
        var idOriginal = firma.Id;
        var creadoOriginal = firma.CreadoEn;
        var nuevaImagen = new byte[] { 0xFF, 0xD8, 0xFF, 9, 9 };

        firma.Actualizar(nuevaImagen, "image/jpeg", "nueva-cifrada", "NEW");

        firma.Id.Should().Be(idOriginal);
        firma.CreadoEn.Should().Be(creadoOriginal);
        firma.ImagenFirma.Should().BeEquivalentTo(nuevaImagen);
        firma.ContentType.Should().Be("image/jpeg");
        firma.ClaveCifrada.Should().Be("nueva-cifrada");
        firma.Sigla.Should().Be("NEW");
    }

    [Fact]
    public void Actualizar_ShouldClearClave_WhenBlank()
    {
        var firma = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, Imagen, "image/png", "vieja");

        firma.Actualizar(Imagen, "image/png", null, null);

        firma.ClaveCifrada.Should().BeNull();
        firma.Sigla.Should().BeNull();
    }

    [Fact]
    public void Actualizar_ShouldThrow_WhenImagenEmpty()
    {
        var firma = FirmaUsuario.Crear(Guid.NewGuid(), UsuarioId, Imagen, "image/png");

        var act = () => firma.Actualizar([], "image/png");

        act.Should().Throw<ArgumentException>();
    }
}
