using GAM106_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GAM106_ASM.Pages.Characters
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;
        public List<Character> Characters { get; set; } = new();
        public List<Player> Players { get; set; } = new();
        public async Task OnGetAsync()
        {
            Characters = await _context.Characters.Include(c => c.Player).OrderBy(c => c.CharacterId).ToListAsync();
            Players = await _context.Players.OrderBy(p => p.EmailAccount).ToListAsync();
        }
    }
}
