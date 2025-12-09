using GAM106_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GAM106_ASM.Pages.Quests
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Quest> Quests { get; set; } = new List<Quest>();

        public async Task OnGetAsync()
        {
            Quests = await _context.Quests
                .OrderBy(q => q.QuestId)
                .ToListAsync();
        }
    }
}
