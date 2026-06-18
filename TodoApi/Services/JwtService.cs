using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Models;

namespace TodoApi.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims =  new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),

            new Claim(ClaimTypes.Email, user.Email),

            new Claim(ClaimTypes.Role, user.Role)
        };
        
        if (user.Role == "Admin")
        {
            claims.Add(new Claim("Permission", "Todos.Read"));
            claims.Add(new Claim("Permission", "Todos.Create"));
            claims.Add(new Claim("Permission", "Todos.Delete"));
        }
        else
        {
            claims.Add(new Claim("Permission", "Todos.Read"));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
    
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];

        using var rng = RandomNumberGenerator.Create();

        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }
}