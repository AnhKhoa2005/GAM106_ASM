using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace GAM106_ASM.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Character> Characters { get; set; }

    public virtual DbSet<GameMode> GameModes { get; set; }

    public virtual DbSet<ItemSalesSheet> ItemSalesSheets { get; set; }

    public virtual DbSet<ItemType> ItemTypes { get; set; }

    public virtual DbSet<Monster> Monsters { get; set; }

    public virtual DbSet<MonsterKill> MonsterKills { get; set; }

    public virtual DbSet<PlayHistory> PlayHistories { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<PlayerQuest> PlayerQuests { get; set; }

    public virtual DbSet<Quest> Quests { get; set; }

    public virtual DbSet<Resource> Resources { get; set; }

    public virtual DbSet<ResourceGathering> ResourceGatherings { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Connection string is configured in Program.cs and appsettings.json
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("graphql", "pg_graphql")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasKey(e => e.CharacterId).HasName("character_pkey");

            entity.ToTable("character");

            entity.HasIndex(e => e.PlayerId, "character_player_id_key").IsUnique();

            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.CharacterName)
                .HasMaxLength(50)
                .HasColumnName("character_name");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");

            entity.HasOne(d => d.Player).WithOne(p => p.Character)
                .HasForeignKey<Character>(d => d.PlayerId)
                .HasConstraintName("character_player_id_fkey");
        });

        modelBuilder.Entity<GameMode>(entity =>
        {
            entity.HasKey(e => e.ModeId).HasName("game_mode_pkey");

            entity.ToTable("game_mode");

            entity.HasIndex(e => e.ModeName, "game_mode_mode_name_key").IsUnique();

            entity.Property(e => e.ModeId).HasColumnName("mode_id");
            entity.Property(e => e.ModeName)
                .HasMaxLength(50)
                .HasColumnName("mode_name");
        });

        modelBuilder.Entity<ItemSalesSheet>(entity =>
        {
            entity.HasKey(e => e.ItemSheetId).HasName("item_sales_sheet_pkey");

            entity.ToTable("item_sales_sheet");

            entity.Property(e => e.ItemSheetId).HasColumnName("item_sheet_id");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.ItemTypeId).HasColumnName("item_type_id");
            entity.Property(e => e.ItemVersionName)
                .HasMaxLength(50)
                .HasColumnName("item_version_name");
            entity.Property(e => e.PurchaseValue).HasColumnName("purchase_value");

            entity.HasOne(d => d.ItemType).WithMany(p => p.ItemSalesSheets)
                .HasForeignKey(d => d.ItemTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("item_sales_sheet_item_type_id_fkey");
        });

        modelBuilder.Entity<ItemType>(entity =>
        {
            entity.HasKey(e => e.ItemTypeId).HasName("item_type_pkey");

            entity.ToTable("item_type");

            entity.HasIndex(e => e.ItemTypeName, "item_type_item_type_name_key").IsUnique();

            entity.Property(e => e.ItemTypeId).HasColumnName("item_type_id");
            entity.Property(e => e.ItemTypeName)
                .HasMaxLength(50)
                .HasColumnName("item_type_name");
        });

        modelBuilder.Entity<Monster>(entity =>
        {
            entity.HasKey(e => e.MonsterId).HasName("monster_pkey");

            entity.ToTable("monster");

            entity.Property(e => e.MonsterId).HasColumnName("monster_id");
            entity.Property(e => e.ExperienceReward).HasColumnName("experience_reward");
            entity.Property(e => e.MonsterName)
                .HasMaxLength(50)
                .HasColumnName("monster_name");
        });

        modelBuilder.Entity<MonsterKill>(entity =>
        {
            entity.HasKey(e => new { e.PlayerId, e.MonsterId, e.KillTime }).HasName("monster_kill_pkey");

            entity.ToTable("monster_kill");

            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.MonsterId).HasColumnName("monster_id");
            entity.Property(e => e.KillTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("kill_time");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Monster).WithMany(p => p.MonsterKills)
                .HasForeignKey(d => d.MonsterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("monster_kill_monster_id_fkey");

            entity.HasOne(d => d.Player).WithMany(p => p.MonsterKills)
                .HasForeignKey(d => d.PlayerId)
                .HasConstraintName("monster_kill_player_id_fkey");
        });

        modelBuilder.Entity<PlayHistory>(entity =>
        {
            entity.HasKey(e => new { e.PlayerId, e.ModeId, e.StartTime }).HasName("play_history_pkey");

            entity.ToTable("play_history");

            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.ModeId).HasColumnName("mode_id");
            entity.Property(e => e.StartTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_time");
            entity.Property(e => e.EndTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_time");

            entity.HasOne(d => d.Mode).WithMany(p => p.PlayHistories)
                .HasForeignKey(d => d.ModeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("play_history_mode_id_fkey");

            entity.HasOne(d => d.Player).WithMany(p => p.PlayHistories)
                .HasForeignKey(d => d.PlayerId)
                .HasConstraintName("play_history_player_id_fkey");
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.PlayerId).HasName("player_pkey");

            entity.ToTable("player");

            entity.HasIndex(e => e.EmailAccount, "player_email_account_key").IsUnique();

            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.EmailAccount)
                .HasMaxLength(50)
                .HasColumnName("email_account");
            entity.Property(e => e.ExperiencePoints)
                .HasDefaultValue(0)
                .HasColumnName("experience_points");
            entity.Property(e => e.FoodBar)
                .HasDefaultValue(20)
                .HasColumnName("food_bar");
            entity.Property(e => e.HealthBar)
                .HasDefaultValue(20)
                .HasColumnName("health_bar");
            entity.Property(e => e.LoginPassword)
                .HasMaxLength(50)
                .HasColumnName("login_password");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Player'::character varying")
                .HasColumnName("role");
        });

        modelBuilder.Entity<PlayerQuest>(entity =>
        {
            entity.HasKey(e => new { e.PlayerId, e.QuestId }).HasName("player_quest_pkey");

            entity.ToTable("player_quest");

            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.QuestId).HasColumnName("quest_id");
            entity.Property(e => e.CompletionTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("completion_time");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.HasOne(d => d.Player).WithMany(p => p.PlayerQuests)
                .HasForeignKey(d => d.PlayerId)
                .HasConstraintName("player_quest_player_id_fkey");

            entity.HasOne(d => d.Quest).WithMany(p => p.PlayerQuests)
                .HasForeignKey(d => d.QuestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("player_quest_quest_id_fkey");
        });

        modelBuilder.Entity<Quest>(entity =>
        {
            entity.HasKey(e => e.QuestId).HasName("quest_pkey");

            entity.ToTable("quest");

            entity.Property(e => e.QuestId).HasColumnName("quest_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ExperienceReward).HasColumnName("experience_reward");
            entity.Property(e => e.QuestName)
                .HasMaxLength(50)
                .HasColumnName("quest_name");
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasKey(e => e.ResourceId).HasName("resource_pkey");

            entity.ToTable("resource");

            entity.Property(e => e.ResourceId).HasColumnName("resource_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ResourceName)
                .HasMaxLength(50)
                .HasColumnName("resource_name");
        });

        modelBuilder.Entity<ResourceGathering>(entity =>
        {
            entity.HasKey(e => new { e.PlayerId, e.ResourceId, e.GatheringTime }).HasName("resource_gathering_pkey");

            entity.ToTable("resource_gathering");

            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.ResourceId).HasColumnName("resource_id");
            entity.Property(e => e.GatheringTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("gathering_time");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Player).WithMany(p => p.ResourceGatherings)
                .HasForeignKey(d => d.PlayerId)
                .HasConstraintName("resource_gathering_player_id_fkey");

            entity.HasOne(d => d.Resource).WithMany(p => p.ResourceGatherings)
                .HasForeignKey(d => d.ResourceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("resource_gathering_resource_id_fkey");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("transaction_pkey");

            entity.ToTable("transaction");

            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.ItemSheetId).HasColumnName("item_sheet_id");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.TransactionTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("transaction_time");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(50)
                .HasColumnName("transaction_type");
            entity.Property(e => e.TransactionValue).HasColumnName("transaction_value");
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id");

            entity.HasOne(d => d.ItemSheet).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.ItemSheetId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("transaction_item_sheet_id_fkey");

            entity.HasOne(d => d.Player).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("transaction_player_id_fkey");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("transaction_vehicle_id_fkey");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("vehicle_pkey");

            entity.ToTable("vehicle");

            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PurchaseValue).HasColumnName("purchase_value");
            entity.Property(e => e.VehicleName)
                .HasMaxLength(50)
                .HasColumnName("vehicle_name");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_log_pkey");

            entity.ToTable("audit_log");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.EntityName).HasColumnName("entity_name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Timestamp)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("timestamp");
            entity.Property(e => e.PerformedBy).HasColumnName("performed_by");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
