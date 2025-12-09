using GAM106_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GAM106_ASM.Pages.Vehicles
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;
        public List<Vehicle> Vehicles { get; set; } = new();
        public async Task OnGetAsync() => Vehicles = await _context.Vehicles.OrderBy(v => v.VehicleId).ToListAsync();
    }
}
