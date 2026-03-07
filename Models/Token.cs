namespace AuthService.Models
{
    public class Token
    {
        public int Id { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
    }
}
