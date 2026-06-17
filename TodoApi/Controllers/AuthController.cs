using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs.Auth;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
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
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request)
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

        var token =
            _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Email = user.Email,
            Token = token
        });
    }
}