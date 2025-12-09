using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System;
using GAM106_ASM.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies; // Thêm Cookie Auth
using Microsoft.AspNetCore.HttpOverrides;

namespace GAM106_ASM
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Cấu hình port cho Fly.io (chỉ khi deploy production)
            if (Environment.GetEnvironmentVariable("FLY_APP_NAME") != null)
            {
                builder.WebHost.UseUrls("http://0.0.0.0:8080");
            }

            // Tăng giới hạn Header size để tránh lỗi 400 khi Cookie quá lớn (chứa JWT)
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestHeadersTotalSize = 64 * 1024; // 64KB
            });

            // --- CẤU HÌNH DB CONTEXT ---
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                                 ?? builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // --- ĐĂNG KÝ HTTP CLIENT FACTORY ---
            builder.Services.AddHttpClient()
                .ConfigureHttpClientDefaults(config =>
                {
                    config.ConfigureHttpClient(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(30); // Default timeout 30 giây
                    });
                });

            // --- CẤU HÌNH JWT AUTHENTICATION VÀ AUTHORIZATION ---
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured."));

            builder.Services.AddAuthentication(options =>
            {
                // SỬA: Đặt Cookie làm mặc định để trang Admin hoạt động
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                // Cấu hình Cookie Auth cho Razor Pages (Admin UI)
                options.LoginPath = "/Index"; // Chuyển về trang Login nếu chưa đăng nhập
                options.AccessDeniedPath = "/Error";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true; // Tự động gia hạn cookie khi user active
                options.Cookie.HttpOnly = true; // Bảo mật: JavaScript không thể truy cập cookie
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS nếu có
                options.Cookie.SameSite = SameSiteMode.Lax; // Tránh conflict CSRF
            })
            .AddJwtBearer(options =>
            {
                // Cấu hình JWT Auth cho API Controllers
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                // Thêm chính sách (Policy) Admin nếu cần thiết (tùy chọn)
                options.AddPolicy("AdminRequired", policy =>
                    policy.RequireRole("Admin"));
            });

            builder.Services.AddAuthorization();
            // --- KẾT THÚC CẤU HÌNH JWT ---

            builder.Services.AddRazorPages();
            builder.Services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    // Tắt automatic model validation để tự xử lý validation
                    options.SuppressModelStateInvalidFilter = true;
                });

            // Tôn trọng các header X-Forwarded-* từ Fly.io để biết request gốc là https
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // THÊM CORS POLICY
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // --- SỬA LỖI DATABASE CONSTRAINT (Cho phép giá = 0) ---
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    // Xóa constraint cũ (nếu có) và tạo constraint mới cho phép >= 0
                    dbContext.Database.ExecuteSqlRaw(@"
                        ALTER TABLE item_sales_sheet DROP CONSTRAINT IF EXISTS item_sales_sheet_purchase_value_check;
                        ALTER TABLE item_sales_sheet ADD CONSTRAINT item_sales_sheet_purchase_value_check CHECK (purchase_value >= 0);
                    ");

                    // Tạo bảng audit_log nếu chưa tồn tại (theo chuẩn snake_case của dự án)
                    dbContext.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS audit_log (
                            id SERIAL PRIMARY KEY,
                            action TEXT,
                            entity_name TEXT,
                            description TEXT,
                            timestamp TIMESTAMP WITHOUT TIME ZONE,
                            performed_by TEXT
                        );
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi cập nhật constraint DB: {ex.Message}");
                }
            }

            // --- MIDDLEWARE PIPELINE ---
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // Không dùng HSTS/HttpsRedirect trên Fly (TLS terminate ở edge)
            }

            // Tôn trọng X-Forwarded-* trước mọi middleware khác
            app.UseForwardedHeaders();

            // Disable CSP trong production để tránh conflict với Fly.io
            if (!app.Environment.IsDevelopment())
            {
                app.Use(async (context, next) =>
                {
                    context.Response.Headers.Remove("Content-Security-Policy");
                    await next();
                });
            }

            // Chỉ redirect HTTPS khi local dev (có cert localhost)
            if (app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }
            app.UseStaticFiles();
            app.UseRouting();

            app.UseCors("AllowAll"); // SỬA: Thêm dòng này để sử dụng CORS policy

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapRazorPages();

            app.Run();
        }
    }
}