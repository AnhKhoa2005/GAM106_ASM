using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using GAM106_ASM.Models;
using Microsoft.EntityFrameworkCore;

namespace GAM106_ASM.Pages
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardModel : PageModel
    {
        private readonly ILogger<AdminDashboardModel> _logger;
        private readonly AppDbContext _context;

        public string? AdminEmail { get; set; }
        public int TotalPlayers { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalActivity { get; set; }
        public List<ActivityViewModel> RecentActivities { get; set; } = new List<ActivityViewModel>();

        public AdminDashboardModel(ILogger<AdminDashboardModel> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task OnGetAsync()
        {
            AdminEmail = User.FindFirstValue(ClaimTypes.Email);

            // Thống kê tổng quan
            TotalPlayers = await _context.Players.CountAsync();
            TotalItems = await _context.ItemSalesSheets.CountAsync();
            TotalRevenue = await _context.Transactions.SumAsync(t => (decimal)t.TransactionValue);
            TotalActivity = await _context.PlayHistories.CountAsync();

            // Lấy log hoạt động của Admin (Thêm/Sửa/Xóa) - Chỉ hiển thị hoạt động thật
            RecentActivities = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(20)
                .Select(l => new ActivityViewModel
                {
                    Type = l.Action,
                    Description = $"{l.PerformedBy}: {l.Description}",
                    Time = l.Timestamp,
                    IconClass = "fas fa-user-shield",
                    ColorClass = "blue"
                })
                .ToListAsync();
        }

        public class ActivityViewModel
        {
            public string Type { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime Time { get; set; }
            public string IconClass { get; set; } = string.Empty;
            public string ColorClass { get; set; } = string.Empty;
        }
    }
}