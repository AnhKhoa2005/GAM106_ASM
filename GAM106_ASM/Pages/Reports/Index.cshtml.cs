using GAM106_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GAM106_ASM.Pages.Reports
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public int TotalTransactions { get; set; }
        public int TotalPlaySessions { get; set; }
        public List<Transaction> RecentTransactions { get; set; } = new();
        public List<PlayHistory> RecentPlayHistory { get; set; } = new();
        public List<MonsterKill> TopMonsterKills { get; set; } = new();
        public List<ResourceGathering> RecentResourceGathering { get; set; } = new();
        public List<PlayerQuest> RecentPlayerQuests { get; set; } = new();

        public async Task OnGetAsync()
        {
            TotalTransactions = await _context.Transactions.CountAsync();
            TotalPlaySessions = await _context.PlayHistories.CountAsync();

            RecentTransactions = await _context.Transactions
                .Include(t => t.Player)
                .OrderByDescending(t => t.TransactionTime)
                .Take(10)
                .ToListAsync();

            RecentPlayHistory = await _context.PlayHistories
                .Include(p => p.Player)
                .Include(p => p.Mode)
                .OrderByDescending(p => p.StartTime)
                .Take(10)
                .ToListAsync();

            TopMonsterKills = await _context.MonsterKills
                .Include(m => m.Player)
                .Include(m => m.Monster)
                .OrderByDescending(m => m.Quantity)
                .Take(10)
                .ToListAsync();

            RecentResourceGathering = await _context.ResourceGatherings
                .Include(r => r.Player)
                .Include(r => r.Resource)
                .OrderByDescending(r => r.GatheringTime)
                .Take(10)
                .ToListAsync();

            RecentPlayerQuests = await _context.PlayerQuests
                .Include(p => p.Player)
                .Include(p => p.Quest)
                .OrderByDescending(p => p.CompletionTime)
                .Take(10)
                .ToListAsync();
        }
    }
}
