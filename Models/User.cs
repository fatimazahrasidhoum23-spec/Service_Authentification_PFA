using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;

namespace AuthService.Models;

public class User : IdentityUser
{
    public string Nom { get; set; }
    public string Prenom { get; set; }
    public string? Telephone { get; set; }

    public UserProfile Profile { get; set; }
    //public string? RefreshToken { get; set; }
    //public DateTime? RefreshTokenExpiryTime { get; set; }
    public Token Token { get; set; }
}