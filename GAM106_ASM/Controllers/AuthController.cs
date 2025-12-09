using GAM106_ASM.DTOs;
using GAM106_ASM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GAM106_ASM.Controllers
{
    // Controller này không cần Authorize vì mục đích là đăng nhập
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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