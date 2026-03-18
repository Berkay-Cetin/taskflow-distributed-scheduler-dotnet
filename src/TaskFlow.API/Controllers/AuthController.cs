using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.API.Data;
using TaskFlow.API.Services;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TaskFlowDbContext _db;
    private readonly JwtService _jwt;
    private readonly IConfiguration _config;

    public AuthController(TaskFlowDbContext db, JwtService jwt, IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _config = config;
    }

    public record RegisterRequest(string Username, string Email, string Password, string Role = "Viewer");
    public record LoginRequest(string Username, string Password);
    public record RefreshRequest(string RefreshToken);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            return Conflict("Username already exists");

        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict("Email already exists");

        var user = new User
        {
            Username = req.Username,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Username, user.Email, user.Role });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == req.Username && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken();

        // Eski refresh token'ları temizle
        var old = _db.RefreshTokens.Where(r => r.UserId == user.Id && !r.IsRevoked);
        _db.RefreshTokens.RemoveRange(old);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(
                double.Parse(_config["Jwt:RefreshTokenExpiryDays"]!)),
        });

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken,
            expiresIn = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"]!) * 60,
            user = new { user.Id, user.Username, user.Email, user.Role }
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var token = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == req.RefreshToken && !r.IsRevoked);

        if (token is null || token.ExpiresAt < DateTime.UtcNow)
            return Unauthorized("Invalid or expired refresh token");

        var newAccessToken = _jwt.GenerateAccessToken(token.User!);
        var newRefreshToken = _jwt.GenerateRefreshToken();

        // Rotate refresh token
        token.IsRevoked = true;
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = token.UserId,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(
                double.Parse(_config["Jwt:RefreshTokenExpiryDays"]!)),
        });

        await _db.SaveChangesAsync();

        return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = Guid.Parse(User.FindFirst("userId")!.Value);
        var tokens = _db.RefreshTokens.Where(r => r.UserId == userId && !r.IsRevoked);
        _db.RefreshTokens.RemoveRange(tokens);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = Guid.Parse(User.FindFirst("userId")!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();
        return Ok(new { user.Id, user.Username, user.Email, user.Role, user.LastLoginAt });
    }
}