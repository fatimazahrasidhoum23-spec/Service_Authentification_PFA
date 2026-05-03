using AuthService.DTOs;

namespace AuthService.Interfaces
{
    public interface IAuthService
    {
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken);
        Task<UserProfileDto> GetProfileAsync(string userId);
        Task<bool> LogoutAsync(string userId);
    }

}