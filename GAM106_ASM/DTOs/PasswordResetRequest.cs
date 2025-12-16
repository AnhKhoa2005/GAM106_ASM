using System.ComponentModel.DataAnnotations;

namespace GAM106_ASM.DTOs
{
    public class PasswordResetRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
