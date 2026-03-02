using AuthService.DTOs;
using AuthService.Interfaces;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Services
{
    public class AuthServiceImpl : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public AuthServiceImpl(UserManager<User> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        // ---------------- REGISTER ----------------
        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                Nom = request.Nom,
                Prenom = request.Prenom,
                Telephone = request.Telephone
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                throw new Exception(string.Join(",", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, request.Role);

            return new RegisterResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = request.Role
            };
        }

        // ---------------- LOGIN ----------------
        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                throw new Exception("Email ou mot de passe invalide");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Candidat";

            var jwt = GenerateJwt(user, role);
            var refresh = GenerateRefreshToken();

            user.RefreshToken = refresh;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return new LoginResponseDto
            {
                Token = jwt,
                RefreshToken = refresh,
                Email = user.Email!,
                Role = role
            };
        }

        // ---------------- REFRESH ----------------
        public async Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.RefreshToken == refreshToken);

            // Vérifie si le token est invalide ou expiré
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return null; // ← ne throw plus d'exception

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Candidat";

            var newJwt = GenerateJwt(user, role);

            // Optionnel : génère un nouveau refresh token
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return new LoginResponseDto
            {
                Token = newJwt,
                RefreshToken = newRefreshToken,
                Email = user.Email!,
                Role = role
            };
        }

        // ---------------- HELPERS ----------------
        private string GenerateJwt(User user, string role)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
        public async Task<UserProfileDto> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                throw new Exception("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Candidat";

            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email!,
                Nom = user.Nom,
                Prenom = user.Prenom,
                Telephone = user.Telephone,
                Role = role
            };
        }
    }
}