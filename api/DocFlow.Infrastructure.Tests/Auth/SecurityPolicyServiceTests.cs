using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using DocFlow.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DocFlow.Infrastructure.Tests.Auth;

public sealed class SecurityPolicyServiceTests
{
    private const string CachePrefix = "SecurityPolicy:";

    private static (SecurityPolicyService, Mock<IConfiguracionRepository>) CreateSut(
        Dictionary<string, string>? preloadedCache = null)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        if (preloadedCache is not null)
        {
            foreach (var (key, value) in preloadedCache)
                cache.Set(key, value);
        }

        var repoMock = new Mock<IConfiguracionRepository>(MockBehavior.Strict);

        var innerProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        innerProvider
            .Setup(p => p.GetService(typeof(IConfiguracionRepository)))
            .Returns(repoMock.Object);

        var scope = new Mock<IServiceScope>(MockBehavior.Strict);
        scope.Setup(s => s.ServiceProvider).Returns(innerProvider.Object);
        scope.Setup(s => s.Dispose());

        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        scopeFactory
            .Setup(f => f.CreateScope())
            .Returns(scope.Object);

        var sut = new SecurityPolicyService(cache, scopeFactory.Object);
        return (sut, repoMock);
    }

    // ─── CACHE HIT ────────────────────────────────────────────────

    [Fact]
    public void GetLockoutMaxAttempts_WhenCacheHasValue_ReturnsCachedValue()
    {
        var (sut, repo) = CreateSut(new Dictionary<string, string>
        {
            [$"{CachePrefix}LockoutMaxIntentos"] = "7"
        });

        var result = sut.GetLockoutMaxAttempts();

        result.Should().Be(7);
        repo.Verify(r => r.GetByClaveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetLockoutDurationMinutes_WhenCacheHasValue_ReturnsCachedValue()
    {
        var (sut, repo) = CreateSut(new Dictionary<string, string>
        {
            [$"{CachePrefix}LockoutDuracionMinutos"] = "45"
        });

        var result = sut.GetLockoutDurationMinutes();

        result.Should().Be(45);
        repo.Verify(r => r.GetByClaveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetPasswordRequireUpper_WhenCacheHasTrue_ReturnsTrue()
    {
        var (sut, repo) = CreateSut(new Dictionary<string, string>
        {
            [$"{CachePrefix}PasswordRequireUpper"] = "false"
        });

        var result = sut.GetPasswordRequireUpper();

        result.Should().BeFalse();
        repo.Verify(r => r.GetByClaveAsync(It.IsAny<string>()), Times.Never);
    }

    // ─── CACHE MISS → DB HIT ──────────────────────────────────────

    [Fact]
    public void GetLockoutMaxAttempts_WhenCacheMissAndDbHasValue_ReturnsDbValueAndCachesIt()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("LockoutMaxIntentos"))
            .ReturnsAsync(ConfiguracionSistema.Crear(Guid.NewGuid(), "LockoutMaxIntentos", "3", ""));

        var result = sut.GetLockoutMaxAttempts();

        result.Should().Be(3);
        repo.Verify(r => r.GetByClaveAsync("LockoutMaxIntentos"), Times.Once);

        // Second call should hit cache, not repo
        var result2 = sut.GetLockoutMaxAttempts();
        result2.Should().Be(3);
        repo.Verify(r => r.GetByClaveAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void GetJwtExpirationMinutes_WhenCacheMissAndDbHasValue_ReturnsDbValue()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("JwtExpirationMinutos"))
            .ReturnsAsync(ConfiguracionSistema.Crear(Guid.NewGuid(), "JwtExpirationMinutos", "120", ""));

        var result = sut.GetJwtExpirationMinutes();

        result.Should().Be(120);
    }

    [Fact]
    public void GetPasswordRequireSpecial_WhenCacheMissAndDbHasValue_ReturnsDbValue()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("PasswordRequireSpecial"))
            .ReturnsAsync(ConfiguracionSistema.Crear(Guid.NewGuid(), "PasswordRequireSpecial", "false", ""));

        var result = sut.GetPasswordRequireSpecial();

        result.Should().BeFalse();
    }

    [Fact]
    public void GetRefreshTokenExpirationDays_WhenCacheMissAndDbHasValue_ReturnsDbValue()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("RefreshTokenExpirationDias"))
            .ReturnsAsync(ConfiguracionSistema.Crear(Guid.NewGuid(), "RefreshTokenExpirationDias", "14", ""));

        var result = sut.GetRefreshTokenExpirationDays();

        result.Should().Be(14);
    }

    [Fact]
    public void GetRefreshTokenExpirationDays_WhenNoData_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("RefreshTokenExpirationDias"))
            .ReturnsAsync((ConfiguracionSistema?)null);

        var result = sut.GetRefreshTokenExpirationDays();

        result.Should().Be(7);
    }

    [Fact]
    public void GetRefreshTokenExpirationDays_WhenBelowMin_ClampsToMinimum()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("RefreshTokenExpirationDias"))
            .ReturnsAsync(ConfiguracionSistema.Crear(Guid.NewGuid(), "RefreshTokenExpirationDias", "0", ""));

        var result = sut.GetRefreshTokenExpirationDays();

        result.Should().Be(1);
    }

    [Fact]
    public void GetRefreshTokenExpirationDays_WhenAboveMax_ClampsToMaximum()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("RefreshTokenExpirationDias"))
            .ReturnsAsync(ConfiguracionSistema.Crear(Guid.NewGuid(), "RefreshTokenExpirationDias", "9999", ""));

        var result = sut.GetRefreshTokenExpirationDays();

        result.Should().Be(90);
    }

    [Fact]
    public void GetIsMfaRequiredForAdministrators_WhenNoData_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("RequireMfaAdministradores"))
            .ReturnsAsync((ConfiguracionSistema?)null);

        var result = sut.IsMfaRequiredForAdministrators();

        result.Should().BeFalse();
    }

    [Fact]
    public void GetIsMfaRequiredForOtherUsers_WhenNoData_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("RequireMfaOtrosUsuarios"))
            .ReturnsAsync((ConfiguracionSistema?)null);

        var result = sut.IsMfaRequiredForOtherUsers();

        result.Should().BeFalse();
    }

    // ─── CACHE MISS → DB MISS → FALLBACK DEFAULT ─────────────────

    [Fact]
    public void GetLockoutMaxAttempts_WhenNoData_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("LockoutMaxIntentos"))
            .ReturnsAsync((ConfiguracionSistema?)null);

        var result = sut.GetLockoutMaxAttempts();

        result.Should().Be(5);
    }

    [Fact]
    public void GetLockoutDurationMinutes_WhenNoData_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("LockoutDuracionMinutos"))
            .ReturnsAsync((ConfiguracionSistema?)null);

        var result = sut.GetLockoutDurationMinutes();

        result.Should().Be(30);
    }

    [Fact]
    public void GetJwtExpirationMinutes_WhenNoData_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("JwtExpirationMinutos"))
            .ReturnsAsync((ConfiguracionSistema?)null);

        var result = sut.GetJwtExpirationMinutes();

        result.Should().Be(480);
    }

    [Fact]
    public void GetTotpWindowSeconds_WhenNoData_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("TotpWindowSegundos"))
            .ReturnsAsync((ConfiguracionSistema?)null);

        var result = sut.GetTotpWindowSeconds();

        result.Should().Be(90);
    }

    [Fact]
    public void GetPasswordMinLength_WhenNoData_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("PasswordMinLength"))
            .ReturnsAsync((ConfiguracionSistema?)null);

        var result = sut.GetPasswordMinLength();

        result.Should().Be(8);
    }

    [Fact]
    public void GetPasswordRequireUpper_WhenNoData_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("PasswordRequireUpper"))
            .ReturnsAsync((ConfiguracionSistema?)null);

        var result = sut.GetPasswordRequireUpper();

        result.Should().BeTrue();
    }

    [Fact]
    public void GetPasswordRequireSpecial_WhenNoData_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("PasswordRequireSpecial"))
            .ReturnsAsync((ConfiguracionSistema?)null);

        var result = sut.GetPasswordRequireSpecial();

        result.Should().BeTrue();
    }

    [Fact]
    public void GetLockoutMaxAttempts_WhenCacheHasInvalidValue_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut(new Dictionary<string, string>
        {
            [$"{CachePrefix}LockoutMaxIntentos"] = "not-a-number"
        });

        var result = sut.GetLockoutMaxAttempts();

        result.Should().Be(5);
        repo.Verify(r => r.GetByClaveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetPasswordRequireUpper_WhenCacheHasInvalidValue_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut(new Dictionary<string, string>
        {
            [$"{CachePrefix}PasswordRequireUpper"] = "maybe"
        });

        var result = sut.GetPasswordRequireUpper();

        result.Should().BeTrue();
        repo.Verify(r => r.GetByClaveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetPasswordRequireSpecial_WhenDbHasInvalidValue_ReturnsFallbackDefault()
    {
        var (sut, repo) = CreateSut();
        repo
            .Setup(r => r.GetByClaveAsync("PasswordRequireSpecial"))
            .ReturnsAsync(ConfiguracionSistema.Crear(Guid.NewGuid(), "PasswordRequireSpecial", "invalid", ""));

        var result = sut.GetPasswordRequireSpecial();

        result.Should().BeTrue();
        repo.Verify(r => r.GetByClaveAsync("PasswordRequireSpecial"), Times.Once);
    }

    // ─── INVALIDATE ───────────────────────────────────────────────

    [Fact]
    public void Invalidate_RemovesCacheEntry_AndSubsequentCallFallsThrough()
    {
        var (sut, repo) = CreateSut(new Dictionary<string, string>
        {
            [$"{CachePrefix}LockoutMaxIntentos"] = "9"
        });

        // Precondition: cache hit works without touching repo
        _ = sut.GetLockoutMaxAttempts();
        repo.Verify(r => r.GetByClaveAsync(It.IsAny<string>()), Times.Never);

        // Act: invalidate
        sut.Invalidate("LockoutMaxIntentos");

        // Now cache miss should fall through to repo
        repo
            .Setup(r => r.GetByClaveAsync("LockoutMaxIntentos"))
            .ReturnsAsync(ConfiguracionSistema.Crear(Guid.NewGuid(), "LockoutMaxIntentos", "2", ""));

        var result = sut.GetLockoutMaxAttempts();

        result.Should().Be(2);
        repo.Verify(r => r.GetByClaveAsync("LockoutMaxIntentos"), Times.Once);
    }

    [Theory]
    [InlineData("LockoutMaxIntentos")]
    [InlineData("TotpWindowSegundos")]
    [InlineData("NonExistentKey")]
    public void Invalidate_DoesNotThrow_ForAnyClave(string clave)
    {
        var (sut, _) = CreateSut();

        var act = () => sut.Invalidate(clave);

        act.Should().NotThrow();
    }
}
