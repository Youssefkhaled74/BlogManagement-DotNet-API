using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Application.Security;
using BlogManagement.Domain.Entities;
using BlogManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BlogManagement.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly BlogManagementDbContext _db;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(BlogManagementDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim();

        if (await _db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            throw new InvalidOperationException("Email already in use.");
        if (await _db.Users.AnyAsync(x => x.Username == username, cancellationToken))
            throw new InvalidOperationException("Username already in use.");

        var isFirstUser = !await _db.Users.AnyAsync(cancellationToken);
        var defaultRoleName = isFirstUser ? SystemRoles.Admin : SystemRoles.Author;
        var defaultRole = await _db.Roles.Include(x => x.Permissions)
            .FirstOrDefaultAsync(x => x.Name == defaultRoleName, cancellationToken)
            ?? throw new InvalidOperationException($"Default {defaultRoleName} role is not configured.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Roles = new List<Role> { defaultRole }
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return await CreateAuthResultAsync(user, cancellationToken);
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await UsersWithAccess()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid credentials.");
        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is suspended or inactive.");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await CreateAuthResultAsync(user, cancellationToken);
    }

    public async Task<AuthResultDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var tokenHash = ComputeSha256Hash(refreshToken);
        var stored = await _db.RefreshTokens
            .Include(x => x.User).ThenInclude(x => x!.Roles).ThenInclude(x => x.Permissions)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (stored?.User is null || !stored.User.IsActive)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!stored.IsActive)
        {
            await _db.RefreshTokens.Where(x => x.UserId == stored.UserId && x.RevokedAt == null)
                .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, DateTime.UtcNow), cancellationToken);
            throw new UnauthorizedAccessException("Refresh token expired or revoked.");
        }

        stored.RevokedAt = DateTime.UtcNow;
        var generated = GenerateRefreshToken();
        stored.ReplacedByTokenHash = generated.hash;
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = generated.hash,
            UserId = stored.UserId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = generated.expiresAt
        });
        await _db.SaveChangesAsync(cancellationToken);

        var jwt = GenerateJwtForUser(stored.User);
        return new AuthResultDto(jwt.token, generated.token, jwt.expiresAt, generated.expiresAt, MapUser(stored.User));
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var tokenHash = ComputeSha256Hash(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (stored is null) return;

        stored.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");
        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Current password is incorrect.");
        if (request.CurrentPassword == request.NewPassword)
            throw new InvalidOperationException("The new password must be different from the current password.");

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, DateTime.UtcNow), cancellationToken);
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(), Action = AuditAction.Update, EntityName = nameof(User), EntityId = userId,
            PerformedByUserId = userId, Timestamp = DateTime.UtcNow, Data = "{\"passwordChanged\":true}"
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<User> UsersWithAccess() => _db.Users
        .Include(x => x.Roles)
        .ThenInclude(x => x.Permissions);

    private async Task<AuthResultDto> CreateAuthResultAsync(User user, CancellationToken ct)
    {
        if (!_db.Entry(user).Collection(x => x.Roles).IsLoaded)
            await _db.Entry(user).Collection(x => x.Roles).Query().Include(x => x.Permissions).LoadAsync(ct);

        var jwt = GenerateJwtForUser(user);
        var refresh = GenerateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = refresh.hash,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refresh.expiresAt
        });
        await _db.SaveChangesAsync(ct);
        return new AuthResultDto(jwt.token, refresh.token, jwt.expiresAt, refresh.expiresAt, MapUser(user));
    }

    private (string token, DateTime expiresAt) GenerateJwtForUser(User user)
    {
        var issuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
        var audience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing.");
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
        if (Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");

        var minutes = int.TryParse(_config["Jwt:AccessTokenMinutes"], out var value) ? value : 15;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new("username", user.Username)
        };
        claims.AddRange(user.Roles.Select(x => new Claim(ClaimTypes.Role, x.Name)));
        claims.AddRange(user.Roles.SelectMany(x => x.Permissions).Select(x => x.Name).Distinct()
            .Select(x => new Claim("permission", x)));

        var expires = DateTime.UtcNow.AddMinutes(minutes);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    private (string token, string hash, DateTime expiresAt) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        var days = int.TryParse(_config["Jwt:RefreshTokenDays"], out var value) ? value : 7;
        return (token, ComputeSha256Hash(token), DateTime.UtcNow.AddDays(days));
    }

    private static string ComputeSha256Hash(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    private static UserSummaryDto MapUser(User user) => new(
        user.Id, user.Username, user.Email, user.DisplayName, user.IsActive,
        user.CreatedAt, user.LastLoginAt, user.Roles.Select(x => x.Name).OrderBy(x => x).ToArray(), user.ProfileImageUrl);
}
