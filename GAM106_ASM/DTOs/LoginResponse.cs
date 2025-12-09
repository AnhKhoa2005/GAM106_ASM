namespace GAM106_ASM.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int PlayerId { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
