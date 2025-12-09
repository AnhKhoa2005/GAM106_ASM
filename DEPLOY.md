# GAM106 ASM - Game Backend API

API Backend cho game Minecraft-like, xây dựng bằng ASP.NET Core 8.0 + PostgreSQL.

## 🚀 Deploy lên Fly.io

### Bước 1: Cài đặt Fly CLI

```powershell
# Windows
iwr https://fly.io/install.ps1 -useb | iex
```

### Bước 2: Login và khởi tạo

```powershell
# Login vào Fly.io
fly auth login

# Khởi tạo app (chọn region Singapore hoặc Tokyo)
fly launch --name gam106-api --region sin
```

### Bước 3: Set Environment Variables

```powershell
fly secrets set ConnectionStrings__DefaultConnection="Host=db.ocunzshajfroqqvdvyed.supabase.co;Database=postgres;Username=postgres;Password=Anhkhoa2005@;SSL Mode=Require;Trust Server Certificate=true"

fly secrets set Jwt__Key="2C8E4A7D9B6F1C8E4A7D9B6F1C8E4A7D9B6F1C8E4A7D9B6F1C8E4A7D9B6F1C8E4A7D9B6F1C8E4A7D9B6F1C8E4A7D9B6F1C8E4A7D9B6F1C8E4A7D9B6F1C8E4A7D"

fly secrets set Jwt__Issuer="GameBackendAPI"

fly secrets set Jwt__Audience="UnityClient;AdminWeb"
```

### Bước 4: Deploy

```powershell
fly deploy
```

### Bước 5: Mở app

```powershell
fly open
```

## 📝 API Endpoints

### Authentication

- `POST /api/Auth/Login` - Login và nhận JWT token

### Game Data (Requires JWT)

- `GET /api/GameData/Resources` - Lấy tất cả tài nguyên
- `GET /api/GameData/PlayersByMode/{modeName}` - Người chơi theo chế độ
- `GET /api/GameData/WeaponsOver100` - Vũ khí giá > 100
- `GET /api/GameData/PurchasableItems/{playerId}` - Item người chơi có thể mua
- `GET /api/GameData/DiamondItems` - Item kim cương < 500
- `GET /api/GameData/PlayerTransactions/{playerId}` - Giao dịch của người chơi
- `POST /api/GameData/NewItem` - Thêm item mới
- `PUT /api/GameData/UpdatePassword/{playerId}` - Đổi mật khẩu
- `GET /api/GameData/TopSellingItems` - Top item bán chạy
- `GET /api/GameData/PlayerPurchaseCounts` - Thống kê mua hàng

### Admin (Requires Authentication)

- CRUD operations cho Players, Items, Monsters, Quests, Vehicles, Resources, Characters, ItemTypes, GameModes

## 🔧 Tech Stack

- ASP.NET Core 8.0
- PostgreSQL (Supabase)
- JWT Authentication
- Entity Framework Core
- Razor Pages (Admin UI)

## 📦 Local Development

```powershell
# Restore packages
dotnet restore

# Run
dotnet run --project GAM106_ASM

# Test
curl http://localhost:5229/api/GameData/Resources -H "Authorization: Bearer YOUR_TOKEN"
```

## 🌐 Production URL

Sau khi deploy: `https://gam106-api.fly.dev`
