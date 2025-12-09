using GAM106_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GAM106_ASM.Pages.Monsters
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Monster> Monsters { get; set; } = new List<Monster>();

        public async Task OnGetAsync()
        {
            Monsters = await _context.Monsters
                .OrderBy(m => m.MonsterId)
                .ToListAsync();
        }
    }
}
