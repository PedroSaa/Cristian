using DocFlow.Application.Auth.Queries.GetPasswordPolicy;
using DocFlow.Application.Common.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class GetPasswordPolicyQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsConfiguredValues_WithLowercaseAndDigitAlwaysRequired()
    {
        var policy = new Mock<ISecurityPolicyService>();
        policy.Setup(x => x.GetPasswordMinLength()).Returns(12);
        policy.Setup(x => x.GetPasswordRequireUpper()).Returns(false);
        policy.Setup(x => x.GetPasswordRequireSpecial()).Returns(false);
        var sut = new GetPasswordPolicyQueryHandler(policy.Object);

        var result = await sut.Handle(new GetPasswordPolicyQuery(), CancellationToken.None);

        result.MinLength.Should().Be(12);
        result.RequireUppercase.Should().BeFalse();
        result.RequireSpecial.Should().BeFalse();
        // Minúscula y dígito son piso fijo del backend (PasswordPolicyValidator), no configurables.
        result.RequireLowercase.Should().BeTrue();
        result.RequireDigit.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReflectsEnabledConfigurableRules()
    {
        var policy = new Mock<ISecurityPolicyService>();
        policy.Setup(x => x.GetPasswordMinLength()).Returns(8);
        policy.Setup(x => x.GetPasswordRequireUpper()).Returns(true);
        policy.Setup(x => x.GetPasswordRequireSpecial()).Returns(true);
        var sut = new GetPasswordPolicyQueryHandler(policy.Object);

        var result = await sut.Handle(new GetPasswordPolicyQuery(), CancellationToken.None);

        result.MinLength.Should().Be(8);
        result.RequireUppercase.Should().BeTrue();
        result.RequireSpecial.Should().BeTrue();
    }
}
