using GAM106_ASM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace GAM106_ASM.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class GameDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Constructor: Inject AppDbContext
        public GameDataController(AppDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------------------
        // BỔ SUNG: GET Item theo ID để fix lỗi CreatedAtAction
        // GET: api/GameData/Item/{id}
        // -------------------------------------------------------------------------
        [HttpGet("Item/{id}")]
        public async Task<ActionResult<ItemSalesSheet>> GetItemById(int id)
        {
            var item = await _context.ItemSalesSheets
                .Include(i => i.ItemType)
                .FirstOrDefaultAsync(i => i.ItemSheetId == id);
            if (item == null)
            {
                return NotFound($"Không tìm thấy Item với ID: {id}");
            }
            return Ok(item);
        }

        // -------------------------------------------------------------------------
        // GET danh sách ItemTypes để hiển thị dropdown
        // GET: api/GameData/ItemTypes
        // -------------------------------------------------------------------------
        [HttpGet("ItemTypes")]
        public async Task<ActionResult<IEnumerable<object>>> GetItemTypes()
        {
            var itemTypes = await _context.ItemTypes
                .Select(it => new { it.ItemTypeId, it.ItemTypeName })
                .ToListAsync();
            return Ok(itemTypes);
        }

        // =========================================================================
        // 1. Lấy thông tin tất cả các loại tài nguyên trong game
        // ... (các phương thức khác giữ nguyên) ...
        // =========================================================================
        [HttpGet("Resources")]
        public async Task<ActionResult<IEnumerable<Resource>>> GetAllResources()
        {
            var resources = await _context.Resources.ToListAsync();
            if (resources == null || resources.Count == 0)
            {
                return NotFound("Không tìm thấy tài nguyên nào.");
            }
            return Ok(resources);
        }

        // -------------------------------------------------------------------------
        // 2. Lấy thông tin tất cả người chơi theo từng chế độ chơi
        // -------------------------------------------------------------------------
        [HttpGet("PlayersByMode/{modeName}")]
        public async Task<ActionResult<IEnumerable<Player>>> GetPlayersByMode(string modeName)
        {
            if (string.IsNullOrEmpty(modeName))
            {
                return BadRequest("Tên chế độ chơi không được để trống.");
            }
            var gameMode = await _context.GameModes.FirstOrDefaultAsync(m => m.ModeName == modeName);
            if (gameMode == null)
            {
                return NotFound($"Không tìm thấy chế độ chơi: {modeName}");
            }
            var playerIds = await _context.PlayHistories
                .Where(ph => ph.ModeId == gameMode.ModeId)
                .Select(ph => ph.PlayerId)
                .Distinct()
                .ToListAsync();
            if (playerIds.Count == 0)
            {
                return NotFound($"Không có người chơi nào đã tham gia chế độ '{modeName}'.");
            }
            var players = await _context.Players
                .Where(p => playerIds.Contains(p.PlayerId))
                .ToListAsync();
            return Ok(players);
        }

        // -------------------------------------------------------------------------
        // 3. Lấy tất cả các vũ khí có giá trị trên 100 điểm kinh nghiệm (Giá trị Mua > 100)
        // -------------------------------------------------------------------------
        [HttpGet("WeaponsOver100")]
        public async Task<ActionResult<IEnumerable<ItemSalesSheet>>> GetWeaponsOver100()
        {
            var weaponTypeId = await _context.ItemTypes
                .Where(it => it.ItemTypeName == "Vũ Khí")
                .Select(it => it.ItemTypeId)
                .FirstOrDefaultAsync();
            if (weaponTypeId == 0)
            {
                return NotFound("Không tìm thấy loại Item 'Vũ Khí'.");
            }
            var weapons = await _context.ItemSalesSheets
                .Include(item => item.ItemType)
                .Where(item => item.ItemTypeId == weaponTypeId && item.PurchaseValue > 100)
                .ToListAsync();
            if (weapons.Count == 0)
            {
                return NotFound("Không tìm thấy vũ khí nào có giá trị trên 100.");
            }
            return Ok(weapons);
        }

        // -------------------------------------------------------------------------
        // 4. Lấy thông tin các item mà người chơi có thể mua
        // -------------------------------------------------------------------------
        [HttpGet("PurchasableItems/{playerId}")]
        public async Task<ActionResult<IEnumerable<ItemSalesSheet>>> GetPurchasableItems(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
            {
                return NotFound($"Không tìm thấy người chơi với ID: {playerId}");
            }
            var currentExp = player.ExperiencePoints;
            var purchasableItems = await _context.ItemSalesSheets
                .Include(item => item.ItemType)
                .Where(item => item.PurchaseValue <= currentExp)
                .ToListAsync();
            if (purchasableItems.Count == 0)
            {
                return NotFound($"Người chơi ID {playerId} không đủ kinh nghiệm để mua bất kỳ item nào.");
            }
            return Ok(purchasableItems);
        }

        // -------------------------------------------------------------------------
        // 5. Lấy thông tin các item có tên chứa 'kim cương' và có giá trị dưới 500
        // -------------------------------------------------------------------------
        [HttpGet("DiamondItems")]
        public async Task<ActionResult<IEnumerable<ItemSalesSheet>>> GetDiamondItems()
        {
            var items = await _context.ItemSalesSheets
                .Include(item => item.ItemType)
                .Where(item => item.ItemVersionName.ToLower().Contains("kim cương") && item.PurchaseValue < 500)
                .ToListAsync();
            if (items.Count == 0)
            {
                return NotFound("Không tìm thấy item nào chứa 'kim cương' và có giá trị dưới 500.");
            }
            return Ok(items);
        }

        // -------------------------------------------------------------------------
        // 6. Lấy thông tin tất cả các giao dịch mua item và phương tiện của một người chơi cụ thể
        // -------------------------------------------------------------------------
        [HttpGet("PlayerTransactions/{playerId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetPlayerTransactions(int playerId)
        {
            var transactions = await _context.Transactions
                .Where(t => t.PlayerId == playerId)
                .Include(t => t.ItemSheet)
                .Include(t => t.Vehicle)
                .OrderByDescending(t => t.TransactionTime)
                .Select(t => new
                {
                    t.TransactionId,
                    t.PlayerId,
                    t.TransactionTime,
                    t.TransactionValue,
                    t.TransactionType,
                    Item = t.ItemSheet != null ? new
                    {
                        t.ItemSheet.ItemSheetId,
                        t.ItemSheet.ItemVersionName,
                        t.ItemSheet.PurchaseValue,
                        t.ItemSheet.ImageUrl
                    } : null,
                    Vehicle = t.Vehicle != null ? new
                    {
                        t.Vehicle.VehicleId,
                        t.Vehicle.VehicleName,
                        t.Vehicle.Description,
                        t.Vehicle.PurchaseValue
                    } : null
                })
                .ToListAsync();

            if (transactions.Count == 0)
            {
                return NotFound($"Người chơi ID {playerId} chưa thực hiện giao dịch nào.");
            }
            return Ok(transactions);
        }

        // =========================================================================
        // THAO TÁC (POST/PUT) - Người chơi có thể dùng
        // =========================================================================

        // 7. Thêm thông tin của một item mới (ĐÃ SỬA LỖI CreatedAtAction)
        [HttpPost("NewItem")]
        public async Task<ActionResult<ItemSalesSheet>> AddNewItem(ItemSalesSheet newItem)
        {
            var itemTypeExists = await _context.ItemTypes.AnyAsync(it => it.ItemTypeId == newItem.ItemTypeId);
            if (!itemTypeExists)
            {
                return BadRequest($"Item Type ID {newItem.ItemTypeId} không tồn tại.");
            }
            _context.ItemSalesSheets.Add(newItem);
            await _context.SaveChangesAsync();

            // Sửa lỗi: Trỏ đến GetItemById
            return CreatedAtAction(nameof(GetItemById), new { id = newItem.ItemSheetId }, newItem);
        }

        // 8. Cập nhật mật khẩu của người chơi (Self-Update)
        [HttpPut("UpdatePassword/{playerId}")]
        public async Task<IActionResult> UpdatePlayerPassword(int playerId, [FromBody] PasswordUpdateDto updateData)
        {
            if (string.IsNullOrEmpty(updateData.NewPassword))
            {
                return BadRequest("Mật khẩu mới không được để trống.");
            }
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
            {
                return NotFound($"Không tìm thấy người chơi với ID: {playerId}");
            }
            player.LoginPassword = updateData.NewPassword;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "Lỗi cập nhật dữ liệu.");
            }
            return NoContent();
        }

        // =========================================================================
        // TRUY VẤN TỔNG HỢP VÀ THỐNG KÊ (Aggregate Queries)
        // =========================================================================

        // 9. Lấy danh sách các item được mua nhiều nhất
        [HttpGet("TopSellingItems")]
        public async Task<ActionResult<IEnumerable<object>>> GetTopSellingItems()
        {
            var topItems = await _context.Transactions
                .Where(t => t.ItemSheetId.HasValue) // Chỉ tính các giao dịch mua Item
                .GroupBy(t => t.ItemSheetId)
                .Select(g => new
                {
                    ItemSheetId = g.Key,
                    TotalQuantity = g.Count()
                })
                .OrderByDescending(result => result.TotalQuantity)
                .Take(10)
                .Join(
                    _context.ItemSalesSheets.Include(i => i.ItemType),
                    result => result.ItemSheetId,
                    item => item.ItemSheetId,
                    (result, item) => new
                    {
                        item.ItemVersionName,
                        item.PurchaseValue,
                        ItemTypeName = item.ItemType.ItemTypeName,
                        result.TotalQuantity
                    }
                )
                .ToListAsync();
            if (topItems.Count == 0)
            {
                return NotFound("Không có dữ liệu giao dịch item.");
            }
            return Ok(topItems);
        }

        // 10. Lấy danh sách tất cả người chơi và số lần họ đã mua hàng
        [HttpGet("PlayerPurchaseCounts")]
        public async Task<ActionResult<IEnumerable<object>>> GetPlayerPurchaseCounts()
        {
            var playerCounts = await _context.Players
                .GroupJoin(
                    _context.Transactions,
                    player => player.PlayerId,
                    transaction => transaction.PlayerId,
                    (player, transactions) => new
                    {
                        player.PlayerId,
                        player.EmailAccount,
                        TotalPurchases = transactions.Count(t => t.ItemSheetId.HasValue || t.VehicleId.HasValue) // Đếm giao dịch mua Item HOẶC Phương tiện
                    }
                )
                .OrderByDescending(p => p.TotalPurchases)
                .ToListAsync();
            return Ok(playerCounts);
        }
    }

    // --- DTO để cập nhật mật khẩu ---
    public class PasswordUpdateDto
    {
        // Thêm dấu ? để cho phép null (hoặc dùng = string.Empty;)
        public string? NewPassword { get; set; }
    }
}