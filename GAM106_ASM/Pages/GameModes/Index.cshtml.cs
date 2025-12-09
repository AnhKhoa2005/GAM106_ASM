using GAM106_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GAM106_ASM.Pages.GameModes
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;
        public List<GameMode> GameModes { get; set; } = new();
        public async Task OnGetAsync() => GameModes = await _context.GameModes.OrderBy(g => g.ModeId).ToListAsync();
    }
}
