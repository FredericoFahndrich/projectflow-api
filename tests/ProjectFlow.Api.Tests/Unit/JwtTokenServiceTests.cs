using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using ProjectFlow.Api.Domain;
using ProjectFlow.Api.Infrastructure;

namespace ProjectFlow.Api.Tests.Unit;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void Generate_IncludesIdentityAndRoleClaims()
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Name = "Ada Lovelace",
            Email = "ada@example.com",
            PasswordHash = "not-used",
            Role = UserRole.Admin
        };
        var options = Options.Create(new JwtOptions
        {
            Secret = "a-test-secret-with-more-than-32-characters",
            Issuer = "tests",
            Audience = "tests",
            ExpirationMinutes = 30
        });

        var generated = new JwtTokenService(options).Generate(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(generated.Value);

        Assert.Equal(user.Id.ToString(), token.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("Admin", token.Claims.Single(x => x.Type == ClaimTypes.Role).Value);
        Assert.True(generated.ExpiresAt > DateTimeOffset.UtcNow);
    }
}

