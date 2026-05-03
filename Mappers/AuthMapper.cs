using AuthService.DTOs;
using AuthService.Models;

namespace AuthService.Mappers
{
    public static class AuthMapper
    {
        // RegisterRequestDto → User
        public static User ToUser(RegisterRequestDto request)
        {
            return new User
            {
                UserName = request.Email,
                Email = request.Email,
                Nom = request.Nom,
                Prenom = request.Prenom,
                Telephone = request.Telephone // ← ajoute ça
            };
        }

        // User + role + jwt + refreshToken → LoginResponseDto
        public static LoginResponseDto ToLoginDto(User user, string role, string jwt, string refreshToken)
        {
            return new LoginResponseDto
            {
                Token = jwt,
                RefreshToken = refreshToken,
                Email = user.Email!,
                Role = role
            };
        }

        // User + role → UserProfileDto
        public static UserProfileDto ToProfileDto(User user, string? role)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Nom = user.Nom,
                Prenom = user.Prenom,
                Role = role
            };
        }
    }
}