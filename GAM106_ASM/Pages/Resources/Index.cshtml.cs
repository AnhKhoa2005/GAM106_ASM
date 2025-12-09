using GAM106_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GAM106_ASM.Pages.Resources
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;
        public List<Resource> Resources { get; set; } = new();
        public async Task OnGetAsync() => Resources = await _context.Resources.OrderBy(r => r.ResourceId).ToListAsync();
    }
}
