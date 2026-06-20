using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs.Auth;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[ApiVersionNeutral]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwtService;

    public AuthController(
        ApplicationDbContext db,
        JwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request)
    {
        var exists = await _db.Users
            .AnyAsync(x => x.Email == request.Email);

        if (exists)
        {
            return BadRequest("Email already exists");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    request.Password)
        };

        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        return Ok("User registered successfully");
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x =>
                x.Email == request.Email);

        if (user == null)
        {
            return Unauthorized();
        }

        var validPassword =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash);

        if (!validPassword)
        {
            return Unauthorized();
        }

        var token = _jwtService.GenerateToken(user);

        var refreshToken = _jwtService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        });

        await _db.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            Email = user.Email
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        var storedToken = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

        if (storedToken == null)
        {
            return Unauthorized("Invalid refresh token");
        }

        if (storedToken.IsRevoked)
        {
            return Unauthorized("Token revoked");
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized("Token expired");
        }

        var newAccessToken =
            _jwtService.GenerateToken(storedToken.User);

        return Ok(new AuthResponse
        {
            Token = newAccessToken,
            RefreshToken = storedToken.Token,
            Email = storedToken.User.Email
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(RefreshTokenRequest request)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(x =>
                x.Token == request.RefreshToken);

        if (token == null)
        {
            return NotFound();
        }

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok("Logged out");
    }
    
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var tokens = await _db.RefreshTokens
            .Where(x =>
                x.UserId == Guid.Parse(userId!)
                && !x.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok();
    }
}