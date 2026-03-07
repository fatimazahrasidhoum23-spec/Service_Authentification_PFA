using AuthService.DTOs;
using AuthService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    

    public AuthController(IAuthService authService )
    {
        _authService = authService;
      
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(result);
    }


    //[HttpGet("register")]
    //public IActionResult TestGet()
    //{
    //    return Ok("GET fonctionne !");
    //}

    //Login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }
    // ---------------- REFRESH TOKEN ----------------
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (result == null)
            return Unauthorized(new { message = "Refresh token invalide ou expiré" });

        return Ok(result);
    }
    /// Profile

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($">>> userId from token: {userId}"); // ← debug

        if (userId == null)
            return Unauthorized();

        var result = await _authService.GetProfileAsync(userId);
        if (result == null)
            return NotFound("User not found");
        return Ok(result);
    }
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok("API OK");
    }
}
