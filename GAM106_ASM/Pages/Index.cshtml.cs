using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using GAM106_ASM.DTOs; // Đảm bảo DTOs/LoginRequest.cs tồn tại

namespace GAM106_ASM.Pages
{
    [AllowAnonymous] // Trang này là trang công cộng (Login)
    public class IndexModel : PageModel // Tên lớp vẫn là IndexModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;

        [BindProperty]
        public LoginRequest Input { get; set; } = new LoginRequest();
        public string? ErrorMessage { get; set; } // Thêm dấu ?

        public IndexModel(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Xóa cookie cũ khi vào trang login (để luôn hiển thị form login)
            // Nhưng KHÔNG xóa nếu đang trong quá trình redirect từ POST (TempData check)
            if (User.Identity?.IsAuthenticated == true && TempData["JustLoggedIn"] == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            ErrorMessage = "";
            return Page();
        }

        public async Task<IActionResult> OnGetLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Vui lòng nhập Email và Mật khẩu.";
                return Page();
            }

            try
            {
                string baseApiUrl = _configuration.GetValue<string>("BaseApiUrl") ?? Request.Scheme + "://" + Request.Host.Value;
                var client = _clientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30); // Tăng timeout lên 30 giây cho lần đầu

                var loginContent = new StringContent(JsonSerializer.Serialize(Input), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{baseApiUrl}/api/Auth/Login", loginContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response: {responseString}");

                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    Console.WriteLine($"LoginResponse is null: {loginResponse == null}");

                    if (loginResponse != null)
                    {
                        Console.WriteLine($"Token: {loginResponse.Token}");
                        Console.WriteLine($"PlayerId: {loginResponse.PlayerId}");
                        Console.WriteLine($"Role: {loginResponse.Role}");
                    }

                    if (loginResponse != null && loginResponse.Role == "Admin")
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, loginResponse.PlayerId.ToString()),
                            new Claim(ClaimTypes.Email, Input.Email),
                            new Claim(ClaimTypes.Role, loginResponse.Role),
                            new Claim("Token", loginResponse.Token)
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        Console.WriteLine("Signing in...");
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                        // Đánh dấu là vừa mới login để OnGetAsync không xóa cookie
                        TempData["JustLoggedIn"] = "true";

                        Console.WriteLine("Redirecting to AdminDashboard...");
                        return RedirectToPage("/AdminDashboard");
                    }
                    else
                    {
                        ErrorMessage = "Tài khoản không có quyền Admin.";
                        return Page();
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ErrorMessage = "Email hoặc mật khẩu không hợp lệ.";
                    return Page();
                }
                else
                {
                    ErrorMessage = "Lỗi hệ thống khi đăng nhập.";
                    return Page();
                }
            }
            catch (TaskCanceledException)
            {
                ErrorMessage = "Yêu cầu đăng nhập hết thời gian chờ. Vui lòng thử lại.";
                return Page();
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Không thể kết nối đến server. Vui lòng thử lại.";
                Console.WriteLine($"HTTP Error: {ex.Message}");
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại.";
                Console.WriteLine($"Error: {ex.Message}");
                return Page();
            }
        }
    }
}