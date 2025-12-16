using GAM106_ASM.DTOs;
using GAM106_ASM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GAM106_ASM.Services;
using System.ComponentModel.DataAnnotations;

namespace GAM106_ASM.Controllers
{
    // Controller này không cần Authorize vì mục đích là đăng nhập
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IOtpService _otpService;

        public AuthController(AppDbContext context, IConfiguration configuration, IEmailSender emailSender, IOtpService otpService)
        {
            _context = context;
            _configuration = configuration;
            _emailSender = emailSender;
            _otpService = otpService;
        }

        // POST api/Auth/Register: Đăng ký tài khoản người chơi mới
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null || !TryValidateModel(request))
            {
                return BadRequest(new { Message = "Dữ liệu đăng ký không hợp lệ." });
            }

            var exists = await _context.Players.AnyAsync(p => p.EmailAccount == request.Email);
            if (exists)
            {
                return Conflict(new { Message = "Email đã tồn tại." });
            }

            var player = new Player
            {
                EmailAccount = request.Email,
                LoginPassword = request.Password,
                ExperiencePoints = 0,
                HealthBar = 20,
                FoodBar = 20,
                Role = "Player"
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Login), new { email = request.Email }, new { Message = "Đăng ký thành công." });
        }

        // POST api/Auth/Login: Điểm cuối để cấp Token
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Log để debug
            Console.WriteLine($"=== LOGIN REQUEST ===");
            Console.WriteLine($"Request is null: {request == null}");

            if (request != null)
            {
                Console.WriteLine($"Email: '{request.Email}'");
                Console.WriteLine($"Password: '{request.Password}'");
                Console.WriteLine($"Email is null or empty: {string.IsNullOrEmpty(request.Email)}");
                Console.WriteLine($"Password is null or empty: {string.IsNullOrEmpty(request.Password)}");
            }

            // Kiểm tra request null
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                Console.WriteLine("BAD REQUEST: Email hoặc Password rỗng");
                return BadRequest(new { Message = "Email và Password không được để trống." });
            }

            // 1. Xác thực người dùng (sử dụng mật khẩu thô như yêu cầu)
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.EmailAccount == request.Email && p.LoginPassword == request.Password);

            Console.WriteLine($"Player found: {player != null}");

            if (player == null)
            {
                Console.WriteLine("UNAUTHORIZED: Không tìm thấy player");
                return Unauthorized(new { Message = "Email hoặc Mật khẩu không đúng." });
            }

            Console.WriteLine($"Player Role: {player.Role}");

            // 2. Tạo Token có Role
            var tokenString = GenerateJwtToken(player);

            Console.WriteLine("LOGIN SUCCESS");
            return Ok(new
            {
                Token = tokenString,
                PlayerId = player.PlayerId,
                Role = player.Role
            });
        }

        // POST api/Auth/RequestPasswordReset: gửi OTP qua email
        [HttpPost("RequestPasswordReset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            if (request == null || !TryValidateModel(request))
            {
                return BadRequest(new { Message = "Email không hợp lệ." });
            }

            var player = await _context.Players.FirstOrDefaultAsync(p => p.EmailAccount == request.Email);
            if (player == null)
            {
                // Để tránh lộ thông tin, vẫn trả về Ok
                return Ok(new { Message = "Nếu email tồn tại, OTP đã được gửi." });
            }

            var otp = _otpService.GenerateOtp(player.EmailAccount, TimeSpan.FromMinutes(10));
            var body = $"Mã OTP đặt lại mật khẩu của bạn là: {otp}. Mã có hiệu lực trong 10 phút.";
            try
            {
                await _emailSender.SendEmailAsync(player.EmailAccount, "OTP đặt lại mật khẩu", body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send email failed: {ex.Message}");
                return StatusCode(500, new { Message = "Gửi email thất bại. Kiểm tra cấu hình SMTP." });
            }

            return Ok(new { Message = "OTP đã được gửi đến email." });
        }

        // POST api/Auth/ConfirmPasswordReset: xác nhận OTP và đổi mật khẩu
        [HttpPost("ConfirmPasswordReset")]
        public async Task<IActionResult> ConfirmPasswordReset([FromBody] PasswordResetConfirm request)
        {
            if (request == null || !TryValidateModel(request))
            {
                return BadRequest(new { Message = "Dữ liệu không hợp lệ." });
            }

            var player = await _context.Players.FirstOrDefaultAsync(p => p.EmailAccount == request.Email);
            if (player == null)
            {
                return Unauthorized(new { Message = "Email không hợp lệ." });
            }

            var isValid = _otpService.ValidateOtp(player.EmailAccount, request.Otp);
            if (!isValid)
            {
                return Unauthorized(new { Message = "OTP không đúng hoặc đã hết hạn." });
            }

            player.LoginPassword = request.NewPassword;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đổi mật khẩu thành công." });
        }

        private string GenerateJwtToken(Player player)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured."));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, player.PlayerId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, player.EmailAccount),
                
                // Thêm Role vào Claims để phân quyền
                new Claim(ClaimTypes.Role, player.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1), // Token hết hạn sau 1 giờ
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}