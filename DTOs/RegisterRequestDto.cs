namespace AuthService.DTOs
{
    public class RegisterRequestDto
    {
        public string Email { get; set; }
        public string Password { get; set; }

        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Telephone { get; set; }

        public string Role { get; set; } // RH / Technique / Candidat
    }
}
