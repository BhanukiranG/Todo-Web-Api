using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Moq;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.UnitTests.Services;

public class JwtServiceTests
{
    private readonly Mock<IConfiguration> _configuration;
    private readonly JwtService _service;

    public JwtServiceTests()
    {
        _configuration = new Mock<IConfiguration>();
        
        _configuration.Setup(x => x["Jwt:Key"]).Returns("SuperSecretKeyForJwtAuthentication123456");
        _configuration.Setup(x => x["Jwt:Issuer"]).Returns("TodoApi");
        _configuration.Setup(x => x["Jwt:Audience"]).Returns("TodoApiUsers");

        _service = new JwtService(_configuration.Object);
    }

    [Fact]
    public void GenerateToken_Should_Return_Valid_JwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            Role = "User"
        };

        // Act
        var token = _service.GenerateToken(user);

        // Assert
        Assert.False(string.IsNullOrEmpty(token));
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        Assert.Equal("TodoApi", jwtToken.Issuer);
        Assert.Equal("TodoApiUsers", jwtToken.Audiences.First());
        Assert.Contains(jwtToken.Claims, c => c.Type == "Permission" && c.Value == "Todos.Read");
        Assert.DoesNotContain(jwtToken.Claims, c => c.Type == "Permission" && c.Value == "Todos.Create");
    }

    [Fact]
    public void GenerateToken_For_Admin_Should_Contain_All_Permissions()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            Role = "Admin"
        };

        // Act
        var token = _service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.Contains(jwtToken.Claims, c => c.Type == "Permission" && c.Value == "Todos.Read");
        Assert.Contains(jwtToken.Claims, c => c.Type == "Permission" && c.Value == "Todos.Create");
        Assert.Contains(jwtToken.Claims, c => c.Type == "Permission" && c.Value == "Todos.Delete");
    }

    [Fact]
    public void GenerateRefreshToken_Should_Return_Base64String()
    {
        // Act
        var refreshToken = _service.GenerateRefreshToken();

        // Assert
        Assert.False(string.IsNullOrEmpty(refreshToken));
        
        // Base64 string from 64 bytes should be 88 characters long
        var bytes = Convert.FromBase64String(refreshToken);
        Assert.Equal(64, bytes.Length);
    }
}
