namespace AuthService.Models;

public class UserProfile
{
    public int Id { get; set; }

    public string Adresse { get; set; }

    public string UserId { get; set; }
    public User User { get; set; }
}