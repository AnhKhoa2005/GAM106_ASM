using System.ComponentModel.DataAnnotations;

namespace GAM106_ASM.DTOs
{
    public class RegisterRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(4)]
        public string Password { get; set; } = string.Empty;
    }
}
