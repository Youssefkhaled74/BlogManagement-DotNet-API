using System;
using System.Threading;
using System.Threading.Tasks;
using BlogManagement.Application.DTOs;

namespace BlogManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
        Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
        Task<AuthResultDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    }
}
