using GAM106_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GAM106_ASM.Pages.Items
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ItemSalesSheet> Items { get; set; } = new List<ItemSalesSheet>();
        public List<ItemType> ItemTypes { get; set; } = new List<ItemType>();

        public async Task OnGetAsync()
        {
            Items = await _context.ItemSalesSheets
                .Include(i => i.ItemType)
                .OrderBy(i => i.ItemSheetId)
                .ToListAsync();

            ItemTypes = await _context.ItemTypes
                .OrderBy(t => t.ItemTypeName)
                .ToListAsync();
        }
    }
}
