using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlogManagement.Application.DTOs;
using BlogManagement.Application.Interfaces;
using BlogManagement.Domain.Entities;
using BlogManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace BlogManagement.Infrastructure.Auth
{
    public class AuthService : IAuthService
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
            if (await _db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
                throw new InvalidOperationException("Email already in use");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            return await CreateAuthResultAsync(user, cancellationToken);
        }

        public async Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
            if (user == null)
                throw new InvalidOperationException("Invalid credentials");
            if (!user.IsActive)
                throw new InvalidOperationException("Account is suspended or inactive");

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verify == PasswordVerificationResult.Failed)
                throw new InvalidOperationException("Invalid credentials");

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return await CreateAuthResultAsync(user, cancellationToken);
        }

        public async Task<AuthResultDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new InvalidOperationException("Invalid refresh token");

            var tokenHash = ComputeSha256Hash(refreshToken);
            var stored = await _db.RefreshTokens.Include(r => r.User).FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);
            if (stored == null)
                throw new InvalidOperationException("Invalid token");

            if (stored.IsExpired || !stored.IsActive)
            {
                // revoke all user tokens as precaution if reuse detected
                var userTokens = _db.RefreshTokens.Where(r => r.UserId == stored.UserId && r.RevokedAt == null);
                await userTokens.ForEachAsync(r => r.RevokedAt = DateTime.UtcNow, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException("Refresh token expired or revoked");
            }

            // rotate token
            stored.RevokedAt = DateTime.UtcNow;
            var (newToken, newHash, newExp) = GenerateRefreshToken();
            stored.ReplacedByTokenHash = newHash;

            var newStored = new RefreshToken
            {
                Id = Guid.NewGuid(),
                TokenHash = newHash,
                UserId = stored.UserId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = newExp
            };
            _db.RefreshTokens.Add(newStored);
            await _db.SaveChangesAsync(cancellationToken);

            var result = GenerateJwtForUser(stored.User!);
            return new AuthResultDto(result.token, newToken, result.expiresAt, newStored.ExpiresAt);
        }

        public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            var tokenHash = ComputeSha256Hash(refreshToken);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);
            if (stored == null) return;
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<AuthResultDto> CreateAuthResultAsync(User user, CancellationToken cancellationToken)
        {
            var jwt = GenerateJwtForUser(user);
            var (refreshToken, hash, expiresAt) = GenerateRefreshToken();

            var stored = new RefreshToken
            {
                Id = Guid.NewGuid(),
                TokenHash = hash,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };
            _db.RefreshTokens.Add(stored);
            await _db.SaveChangesAsync(cancellationToken);

            return new AuthResultDto(jwt.token, refreshToken, jwt.expiresAt, expiresAt);
        }

        private (string token, DateTime expiresAt) GenerateJwtForUser(User user)
        {
            var issuer = _config["Jwt:Issuer"] ?? "blog-api";
            var audience = _config["Jwt:Audience"] ?? "blog-api-clients";
            var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key must be configured");
            var minutes = int.TryParse(_config["Jwt:AccessTokenMinutes"], out var m) ? m : 15;

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.Username)
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(minutes);
            var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: creds);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return (tokenString, expires);
        }

        private static (string token, string hash, DateTime expiresAt) GenerateRefreshToken()
        {
            var rng = RandomNumberGenerator.Create();
            var bytes = new byte[64];
            rng.GetBytes(bytes);
            var token = Convert.ToBase64String(bytes);
            var hash = ComputeSha256Hash(token);
            var expires = DateTime.UtcNow.AddDays(30); // refresh token valid for 30 days
            return (token, hash, expires);
        }

        private static string ComputeSha256Hash(string raw)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes);
        }
    }
}
