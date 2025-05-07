using Microsoft.EntityFrameworkCore;
using GameServer.Models;

namespace GameServer.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

        // DbSet for each entity
        public DbSet<UserAccount> UserAccount { get; set; }
        public DbSet<UserCharacterOverview> UserCharacterOverviews { get; set; }
        public DbSet<CharacterInventory> CharacterInventories { get; set; }
        public DbSet<UserCharacterStatus> UserCharacterStatuses { get; set; }
        public DbSet<PlayerLocation> PlayerLocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. UserAccount (PK: UserUniqueID)
            modelBuilder.Entity<UserAccount>()
                .HasKey(u => u.UserUniqueID);

            // 2. UserCharacterOverview (PK: CharacterUniqueID)
            modelBuilder.Entity<UserCharacterOverview>()
                .HasKey(c => c.CharacterUniqueID);

            // 3. CharacterInventory (PK: CharacterUniqueID + InventoryNumber)
            modelBuilder.Entity<CharacterInventory>()
                .HasKey(ci => new { ci.CharacterUniqueID, ci.InventoryNumber });

            // 4. UserCharacterStatus (PK: CharacterUniqueID)
            modelBuilder.Entity<UserCharacterStatus>()
                .HasKey(s => s.CharacterUniqueID);

            // 5. PlayerLocation (PK: UserUniqueID + CharacterUniqueID)
            modelBuilder.Entity<PlayerLocation>()
                .HasKey(pl => new { pl.UserUniqueID, pl.CharacterUniqueID });

            // 관계 설정
            modelBuilder.Entity<UserCharacterOverview>()
                .HasOne(c => c.UserAccount)
                .WithMany(u => u.Characters)
                .HasForeignKey(c => c.UserUniqueID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CharacterInventory>()
                .HasOne(i => i.UserAccount)
                .WithMany(u => u.Inventories)
                .HasForeignKey(i => i.UserUniqueID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CharacterInventory>()
                .HasOne(i => i.Character)
                .WithMany(c => c.Inventories)
                .HasForeignKey(i => i.CharacterUniqueID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCharacterStatus>()
                .HasOne(s => s.Character)
                .WithOne(c => c.CharacterStatus)
                .HasForeignKey<UserCharacterStatus>(s => s.CharacterUniqueID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCharacterStatus>()
                .HasOne(s => s.UserAccount)
                .WithMany(u => u.CharacterStatuses)
                .HasForeignKey(s => s.UserUniqueID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerLocation>()
                .HasOne(l => l.Character)
                .WithOne(c => c.PlayerLocation)
                .HasForeignKey<PlayerLocation>(l => l.CharacterUniqueID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerLocation>()
                .HasOne(l => l.UserAccount)
                .WithMany(u => u.PlayerLocations)
                .HasForeignKey(l => l.UserUniqueID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}