using AuthService.Data;
using AuthService.DTOs;
using AuthService.Interfaces;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        private readonly AuthDbContext _context;

        public AuthServiceImpl(UserManager<User> userManager, IConfiguration configuration, AuthDbContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
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

            // Cherche un token existant pour ce user
            var existingToken = await _context.Tokens.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (existingToken != null)
            {
                // Met à jour le token existant
                existingToken.RefreshToken = refresh;
                existingToken.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                _context.Tokens.Update(existingToken);
            }
            else
            {
                // Crée un nouveau token
                await _context.Tokens.AddAsync(new Token
                {
                    RefreshToken = refresh,
                    RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
                    UserId = user.Id
                });
            }
            await _context.SaveChangesAsync();

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
            var token = await _context.Tokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);

            if (token == null || token.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return null;

            var user = token.User;
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Candidat";

            var newJwt = GenerateJwt(user, role);
            var newRefreshToken = GenerateRefreshToken();

            token.RefreshToken = newRefreshToken;
            token.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            _context.Tokens.Update(token);
            await _context.SaveChangesAsync();

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

        // ---------------- PROFILE ----------------
        public async Task<UserProfileDto> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Nom = user.Nom,
                Prenom = user.Prenom,
                Role = roles.FirstOrDefault()
            };
        }
    }
}