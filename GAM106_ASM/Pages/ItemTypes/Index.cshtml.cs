using GAM106_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GAM106_ASM.Pages.ItemTypes
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ItemType> ItemTypes { get; set; } = new List<ItemType>();

        public async Task OnGetAsync()
        {
            ItemTypes = await _context.ItemTypes
                .Include(t => t.ItemSalesSheets)
                .OrderBy(t => t.ItemTypeName)
                .ToListAsync();
        }
    }
}
