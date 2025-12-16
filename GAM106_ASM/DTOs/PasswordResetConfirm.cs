using System.ComponentModel.DataAnnotations;

namespace GAM106_ASM.DTOs
{
    public class PasswordResetConfirm
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(4)]
        public string NewPassword { get; set; } = string.Empty;

        [Required, StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }
}
