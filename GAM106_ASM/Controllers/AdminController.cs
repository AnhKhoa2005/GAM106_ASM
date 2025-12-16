using GAM106_ASM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GAM106_ASM.Controllers
{
    // Chấp nhận cả Cookie (từ Razor Pages) và JWT (từ API calls)
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // Helper method to remove navigation properties from validation
        private void RemoveNavigationPropertiesFromValidation(params string[] properties)
        {
            foreach (var prop in properties)
            {
                ModelState.Remove(prop);
            }
        }

        // ====================================================================
        // QUẢN LÝ PLAYER (Chỉ Admin)
        // ====================================================================

        // GET: api/Admin/Players - Lấy tất cả người chơi (Admin View)
        [HttpGet("Players")]
        public async Task<ActionResult<IEnumerable<Player>>> GetPlayers()
        {
            return await _context.Players
                .OrderBy(p => p.PlayerId)
                .ToListAsync();
        }

        // GET: api/Admin/Players/5 - Lấy thông tin một người chơi
        [HttpGet("Players/{id}")]
        public async Task<ActionResult<Player>> GetPlayer(int id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }
            return player;
        }

        // POST: api/Admin/Players - Thêm người chơi mới
        [HttpPost("Players")]
        public async Task<ActionResult<Player>> PostPlayer(Player player)
        {
            if (string.IsNullOrEmpty(player.Role)) player.Role = "Player";

            // Kiểm tra email đã tồn tại chưa
            if (await _context.Players.AnyAsync(p => p.EmailAccount == player.EmailAccount))
            {
                return BadRequest("Email đã tồn tại trong hệ thống");
            }

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            await LogActivity("Thêm mới", "Người chơi", $"Đã thêm người chơi: {player.EmailAccount}");

            return CreatedAtAction(nameof(GetPlayer), new { id = player.PlayerId }, player);
        }

        // PUT: api/Admin/Players/5 - Cập nhật thông tin người chơi
        [HttpPut("Players/{id}")]
        public async Task<IActionResult> PutPlayer(int id, Player player)
        {
            if (id != player.PlayerId)
            {
                return BadRequest();
            }

            _context.Players.Update(player);

            try
            {
                await _context.SaveChangesAsync();
                await LogActivity("Cập nhật", "Người chơi", $"Đã cập nhật người chơi ID {id}: {player.EmailAccount}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Players.AnyAsync(e => e.PlayerId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Admin/Players/5 - Xóa người chơi (CASCADE)
        [HttpDelete("Players/{id}")]
        public async Task<IActionResult> DeletePlayer(int id, [FromQuery] bool force = false)
        {
            var player = await _context.Players
                .Include(p => p.Character)
                .Include(p => p.Transactions)
                .Include(p => p.PlayHistories)
                .Include(p => p.PlayerQuests)
                .Include(p => p.MonsterKills)
                .Include(p => p.ResourceGatherings)
                .FirstOrDefaultAsync(p => p.PlayerId == id);

            if (player == null)
            {
                return NotFound();
            }

            bool hasRelatedData = (player.Character != null) ||
                                  player.Transactions.Any() ||
                                  player.PlayHistories.Any() ||
                                  player.PlayerQuests.Any() ||
                                  player.MonsterKills.Any() ||
                                  player.ResourceGatherings.Any();

            if (hasRelatedData && !force)
            {
                return Conflict(new { message = "Người chơi này có dữ liệu liên quan (Nhân vật, Giao dịch, Lịch sử chơi...). Bạn có chắc chắn muốn xóa không?", requiresConfirmation = true });
            }

            // Xóa tất cả dữ liệu liên quan
            if (player.Character != null)
                _context.Characters.Remove(player.Character);
            if (player.Transactions.Any())
                _context.Transactions.RemoveRange(player.Transactions);
            if (player.PlayHistories.Any())
                _context.PlayHistories.RemoveRange(player.PlayHistories);
            if (player.PlayerQuests.Any())
                _context.PlayerQuests.RemoveRange(player.PlayerQuests);
            if (player.MonsterKills.Any())
                _context.MonsterKills.RemoveRange(player.MonsterKills);
            if (player.ResourceGatherings.Any())
                _context.ResourceGatherings.RemoveRange(player.ResourceGatherings);

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            await LogActivity("Xóa", "Người chơi", $"Đã xóa người chơi: {player.EmailAccount}");

            return NoContent();
        }

        // ====================================================================
        // QUẢN LÝ ITEMS (Vật phẩm)
        // ====================================================================

        [HttpGet("Items")]
        public async Task<ActionResult<IEnumerable<ItemSalesSheet>>> GetItems()
        {
            return await _context.ItemSalesSheets
                .Include(i => i.ItemType)
                .OrderBy(i => i.ItemSheetId)
                .ToListAsync();
        }

        [HttpGet("Items/{id}")]
        public async Task<ActionResult<ItemSalesSheet>> GetItem(int id)
        {
            var item = await _context.ItemSalesSheets.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        // Helper để ghi log
        private async Task LogActivity(string action, string entity, string desc)
        {
            try
            {
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Admin";
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = action,
                    EntityName = entity,
                    Description = desc,
                    Timestamp = DateTime.Now,
                    PerformedBy = email
                });
                await _context.SaveChangesAsync();
            }
            catch { /* Ignore log errors */ }
        }

        [HttpPost("Items")]
        public async Task<ActionResult<ItemSalesSheet>> PostItem(ItemSalesSheet item)
        {
            // Set ID to 0 for new items to let database generate it
            item.ItemSheetId = 0;

            try
            {
                _context.ItemSalesSheets.Add(item);
                await _context.SaveChangesAsync();

                await LogActivity("Thêm mới", "Vật phẩm", $"Đã thêm vật phẩm: {item.ItemVersionName}");

                return CreatedAtAction(nameof(GetItem), new { id = item.ItemSheetId }, item);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { error = "Database error", message = ex.InnerException?.Message ?? ex.Message });
            }
        }
        [HttpPut("Items/{id}")]
        public async Task<IActionResult> PutItem(int id, ItemSalesSheet item)
        {
            if (id != item.ItemSheetId) return BadRequest();

            _context.ItemSalesSheets.Update(item);
            try
            {
                await _context.SaveChangesAsync();
                await LogActivity("Cập nhật", "Vật phẩm", $"Đã cập nhật vật phẩm ID {id}: {item.ItemVersionName}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.ItemSalesSheets.AnyAsync(e => e.ItemSheetId == id))
                    return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("Items/{id}")]
        public async Task<IActionResult> DeleteItem(int id, [FromQuery] bool force = false)
        {
            var item = await _context.ItemSalesSheets
                .Include(i => i.Transactions)
                .FirstOrDefaultAsync(i => i.ItemSheetId == id);
            if (item == null) return NotFound();

            if (item.Transactions.Any() && !force)
            {
                return Conflict(new { message = "Vật phẩm này đã có giao dịch mua bán. Bạn có chắc chắn muốn xóa không?", requiresConfirmation = true });
            }

            var name = item.ItemVersionName;

            // Xóa các giao dịch liên quan
            if (item.Transactions.Any())
                _context.Transactions.RemoveRange(item.Transactions);

            _context.ItemSalesSheets.Remove(item);
            await _context.SaveChangesAsync();

            await LogActivity("Xóa", "Vật phẩm", $"Đã xóa vật phẩm: {name}");

            return NoContent();
        }        // ====================================================================
        // QUẢN LÝ ITEM TYPES (Loại vật phẩm)
        // ====================================================================

        [HttpGet("ItemTypes")]
        public async Task<ActionResult<IEnumerable<ItemType>>> GetItemTypes()
        {
            return await _context.ItemTypes.OrderBy(t => t.ItemTypeName).ToListAsync();
        }

        [HttpGet("ItemTypes/{id}")]
        public async Task<ActionResult<ItemType>> GetItemType(int id)
        {
            var type = await _context.ItemTypes.FindAsync(id);
            if (type == null) return NotFound();
            return type;
        }

        [HttpPost("ItemTypes")]
        public async Task<ActionResult<ItemType>> PostItemType(ItemType itemType)
        {
            itemType.ItemTypeId = 0;

            try
            {
                _context.ItemTypes.Add(itemType);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetItemType), new { id = itemType.ItemTypeId }, itemType);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { error = "Database error", message = ex.InnerException?.Message ?? ex.Message });
            }
        }
        [HttpPut("ItemTypes/{id}")]
        public async Task<IActionResult> PutItemType(int id, ItemType itemType)
        {
            if (id != itemType.ItemTypeId) return BadRequest();

            _context.ItemTypes.Update(itemType);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.ItemTypes.AnyAsync(e => e.ItemTypeId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("ItemTypes/{id}")]
        public async Task<IActionResult> DeleteItemType(int id, [FromQuery] bool force = false)
        {
            var type = await _context.ItemTypes
                .Include(t => t.ItemSalesSheets)
                    .ThenInclude(i => i.Transactions)
                .FirstOrDefaultAsync(t => t.ItemTypeId == id);
            if (type == null) return NotFound();

            bool hasRelatedData = type.ItemSalesSheets.Any();
            if (hasRelatedData && !force)
            {
                return Conflict(new { message = "Loại vật phẩm này đang chứa các vật phẩm khác. Bạn có chắc chắn muốn xóa không?", requiresConfirmation = true });
            }

            // Xóa tất cả items và transactions liên quan
            foreach (var item in type.ItemSalesSheets)
            {
                if (item.Transactions.Any())
                    _context.Transactions.RemoveRange(item.Transactions);
            }
            if (type.ItemSalesSheets.Any())
                _context.ItemSalesSheets.RemoveRange(type.ItemSalesSheets);

            _context.ItemTypes.Remove(type);
            await _context.SaveChangesAsync();
            return NoContent();
        }        // ====================================================================
        // QUẢN LÝ QUESTS (Nhiệm vụ)
        // ====================================================================

        [HttpGet("Quests")]
        public async Task<ActionResult<IEnumerable<Quest>>> GetQuests()
        {
            return await _context.Quests.OrderBy(q => q.QuestId).ToListAsync();
        }

        [HttpGet("Quests/{id}")]
        public async Task<ActionResult<Quest>> GetQuest(int id)
        {
            var quest = await _context.Quests.FindAsync(id);
            if (quest == null) return NotFound();
            return quest;
        }

        [HttpPost("Quests")]
        public async Task<ActionResult<Quest>> PostQuest(Quest quest)
        {
            quest.QuestId = 0;

            try
            {
                _context.Quests.Add(quest);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetQuest), new { id = quest.QuestId }, quest);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { error = "Database error", message = ex.InnerException?.Message ?? ex.Message });
            }
        }
        [HttpPut("Quests/{id}")]
        public async Task<IActionResult> PutQuest(int id, Quest quest)
        {
            if (id != quest.QuestId) return BadRequest();

            _context.Quests.Update(quest);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Quests.AnyAsync(e => e.QuestId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("Quests/{id}")]
        public async Task<IActionResult> DeleteQuest(int id, [FromQuery] bool force = false)
        {
            var quest = await _context.Quests
                .Include(q => q.PlayerQuests)
                .FirstOrDefaultAsync(q => q.QuestId == id);
            if (quest == null) return NotFound();

            if (quest.PlayerQuests.Any() && !force)
            {
                return Conflict(new { message = "Nhiệm vụ này đang được thực hiện bởi người chơi. Bạn có chắc chắn muốn xóa không?", requiresConfirmation = true });
            }

            // Xóa lịch sử nhiệm vụ của người chơi
            if (quest.PlayerQuests.Any())
                _context.PlayerQuests.RemoveRange(quest.PlayerQuests);

            _context.Quests.Remove(quest);
            await _context.SaveChangesAsync();
            return NoContent();
        }        // ====================================================================
        // QUẢN LÝ MONSTERS (Quái vật)
        // ====================================================================

        [HttpGet("Monsters")]
        public async Task<ActionResult<IEnumerable<Monster>>> GetMonsters()
        {
            return await _context.Monsters.OrderBy(m => m.MonsterId).ToListAsync();
        }

        [HttpGet("Monsters/{id}")]
        public async Task<ActionResult<Monster>> GetMonster(int id)
        {
            var monster = await _context.Monsters.FindAsync(id);
            if (monster == null) return NotFound();
            return monster;
        }

        [HttpPost("Monsters")]
        public async Task<ActionResult<Monster>> PostMonster(Monster monster)
        {
            monster.MonsterId = 0;

            try
            {
                _context.Monsters.Add(monster);
                await _context.SaveChangesAsync();

                await LogActivity("Thêm mới", "Quái vật", $"Đã thêm quái vật: {monster.MonsterName}");

                return CreatedAtAction(nameof(GetMonster), new { id = monster.MonsterId }, monster);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { error = "Database error", message = ex.InnerException?.Message ?? ex.Message });
            }
        }
        [HttpPut("Monsters/{id}")]
        public async Task<IActionResult> PutMonster(int id, Monster monster)
        {
            if (id != monster.MonsterId) return BadRequest();

            _context.Monsters.Update(monster);
            try
            {
                await _context.SaveChangesAsync();
                await LogActivity("Cập nhật", "Quái vật", $"Đã cập nhật quái vật ID {id}: {monster.MonsterName}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Monsters.AnyAsync(e => e.MonsterId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("Monsters/{id}")]
        public async Task<IActionResult> DeleteMonster(int id, [FromQuery] bool force = false)
        {
            var monster = await _context.Monsters
                .Include(m => m.MonsterKills)
                .FirstOrDefaultAsync(m => m.MonsterId == id);
            if (monster == null) return NotFound();

            if (monster.MonsterKills.Any() && !force)
            {
                return Conflict(new { message = "Quái vật này đã bị tiêu diệt bởi người chơi. Bạn có chắc chắn muốn xóa không?", requiresConfirmation = true });
            }

            // Xóa lịch sử tiêu diệt
            if (monster.MonsterKills.Any())
                _context.MonsterKills.RemoveRange(monster.MonsterKills);

            _context.Monsters.Remove(monster);
            await _context.SaveChangesAsync();

            await LogActivity("Xóa", "Quái vật", $"Đã xóa quái vật: {monster.MonsterName}");

            return NoContent();
        }        // ====================================================================
        // QUẢN LÝ VEHICLES (Phương tiện)
        // ====================================================================

        [HttpGet("Vehicles")]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles()
        {
            return await _context.Vehicles.OrderBy(v => v.VehicleId).ToListAsync();
        }

        [HttpGet("Vehicles/{id}")]
        public async Task<ActionResult<Vehicle>> GetVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();
            return vehicle;
        }

        [HttpPost("Vehicles")]
        public async Task<ActionResult<Vehicle>> PostVehicle(Vehicle vehicle)
        {
            vehicle.VehicleId = 0;

            try
            {
                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.VehicleId }, vehicle);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { error = "Database error", message = ex.InnerException?.Message ?? ex.Message });
            }
        }
        [HttpPut("Vehicles/{id}")]
        public async Task<IActionResult> PutVehicle(int id, Vehicle vehicle)
        {
            if (id != vehicle.VehicleId) return BadRequest();

            _context.Vehicles.Update(vehicle);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Vehicles.AnyAsync(e => e.VehicleId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("Vehicles/{id}")]
        public async Task<IActionResult> DeleteVehicle(int id, [FromQuery] bool force = false)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Transactions)
                .FirstOrDefaultAsync(v => v.VehicleId == id);
            if (vehicle == null) return NotFound();

            if (vehicle.Transactions.Any() && !force)
            {
                return Conflict(new { message = "Phương tiện này đã có giao dịch mua bán. Bạn có chắc chắn muốn xóa không?", requiresConfirmation = true });
            }

            // Xóa các giao dịch liên quan
            if (vehicle.Transactions.Any())
                _context.Transactions.RemoveRange(vehicle.Transactions);

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return NoContent();
        }        // ====================================================================
        // QUẢN LÝ RESOURCES (Tài nguyên)
        // ====================================================================

        [HttpGet("Resources")]
        public async Task<ActionResult<IEnumerable<Resource>>> GetResources()
        {
            return await _context.Resources.OrderBy(r => r.ResourceId).ToListAsync();
        }

        [HttpGet("Resources/{id}")]
        public async Task<ActionResult<Resource>> GetResource(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null) return NotFound();
            return resource;
        }

        [HttpPost("Resources")]
        public async Task<ActionResult<Resource>> PostResource(Resource resource)
        {
            resource.ResourceId = 0;

            try
            {
                _context.Resources.Add(resource);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetResource), new { id = resource.ResourceId }, resource);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { error = "Database error", message = ex.InnerException?.Message ?? ex.Message });
            }
        }
        [HttpPut("Resources/{id}")]
        public async Task<IActionResult> PutResource(int id, Resource resource)
        {
            if (id != resource.ResourceId) return BadRequest();

            _context.Resources.Update(resource);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Resources.AnyAsync(e => e.ResourceId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("Resources/{id}")]
        public async Task<IActionResult> DeleteResource(int id, [FromQuery] bool force = false)
        {
            var resource = await _context.Resources
                .Include(r => r.ResourceGatherings)
                .FirstOrDefaultAsync(r => r.ResourceId == id);
            if (resource == null) return NotFound();

            if (resource.ResourceGatherings.Any() && !force)
            {
                return Conflict(new { message = "Tài nguyên này đã được thu thập bởi người chơi. Bạn có chắc chắn muốn xóa không?", requiresConfirmation = true });
            }

            // Xóa lịch sử thu thập
            if (resource.ResourceGatherings.Any())
                _context.ResourceGatherings.RemoveRange(resource.ResourceGatherings);

            _context.Resources.Remove(resource);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ====================================================================
        // QUẢN LÝ CHARACTERS (Nhân vật)
        // ====================================================================

        [HttpGet("Characters")]
        public async Task<ActionResult<IEnumerable<Character>>> GetCharacters()
        {
            return await _context.Characters.OrderBy(c => c.CharacterId).ToListAsync();
        }

        [HttpGet("Characters/{id}")]
        public async Task<ActionResult<Character>> GetCharacter(int id)
        {
            var character = await _context.Characters.FindAsync(id);
            if (character == null) return NotFound();
            return character;
        }

        [HttpPost("Characters")]
        public async Task<ActionResult<Character>> PostCharacter(Character character)
        {
            character.CharacterId = 0;

            try
            {
                _context.Characters.Add(character);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetCharacter), new { id = character.CharacterId }, character);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { error = "Database error", message = ex.InnerException?.Message ?? ex.Message });
            }
        }
        [HttpPut("Characters/{id}")]
        public async Task<IActionResult> PutCharacter(int id, Character character)
        {
            if (id != character.CharacterId) return BadRequest();

            _context.Characters.Update(character);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Characters.AnyAsync(e => e.CharacterId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("Characters/{id}")]
        public async Task<IActionResult> DeleteCharacter(int id)
        {
            var character = await _context.Characters.FindAsync(id);
            if (character == null) return NotFound();

            _context.Characters.Remove(character);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ====================================================================
        // QUẢN LÝ GAME MODES (Chế độ chơi)
        // ====================================================================

        [HttpGet("GameModes")]
        public async Task<ActionResult<IEnumerable<GameMode>>> GetGameModes()
        {
            return await _context.GameModes.OrderBy(g => g.ModeId).ToListAsync();
        }

        [HttpGet("GameModes/{id}")]
        public async Task<ActionResult<GameMode>> GetGameMode(int id)
        {
            var gameMode = await _context.GameModes.FindAsync(id);
            if (gameMode == null) return NotFound();
            return gameMode;
        }

        [HttpPost("GameModes")]
        public async Task<ActionResult<GameMode>> PostGameMode(GameMode gameMode)
        {
            gameMode.ModeId = 0;

            try
            {
                _context.GameModes.Add(gameMode);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetGameMode), new { id = gameMode.ModeId }, gameMode);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { error = "Database error", message = ex.InnerException?.Message ?? ex.Message });
            }
        }
        [HttpPut("GameModes/{id}")]
        public async Task<IActionResult> PutGameMode(int id, GameMode gameMode)
        {
            if (id != gameMode.ModeId) return BadRequest();

            _context.GameModes.Update(gameMode);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.GameModes.AnyAsync(e => e.ModeId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("GameModes/{id}")]
        public async Task<IActionResult> DeleteGameMode(int id, [FromQuery] bool force = false)
        {
            var gameMode = await _context.GameModes
                .Include(m => m.PlayHistories)
                .FirstOrDefaultAsync(m => m.ModeId == id);

            if (gameMode == null) return NotFound();

            if (gameMode.PlayHistories.Any() && !force)
            {
                return Conflict(new { message = "Chế độ chơi này đã có lịch sử chơi của người chơi. Bạn có chắc chắn muốn xóa không?", requiresConfirmation = true });
            }

            if (gameMode.PlayHistories.Any())
                _context.PlayHistories.RemoveRange(gameMode.PlayHistories);

            _context.GameModes.Remove(gameMode);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}