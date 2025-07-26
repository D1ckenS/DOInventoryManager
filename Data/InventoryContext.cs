using Microsoft.EntityFrameworkCore;
using DOInventoryManager.Models;
using System.IO;

namespace DOInventoryManager.Data
{
    public class InventoryContext : DbContext
    {
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Vessel> Vessels { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Consumption> Consumptions { get; set; }
        public DbSet<Allocation> Allocations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Database file will be created in the application folder
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DOInventory.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.Vessel)
                .WithMany(v => v.Purchases)
                .HasForeignKey(p => p.VesselId);

            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Purchases)
                .HasForeignKey(p => p.SupplierId);

            modelBuilder.Entity<Consumption>()
                .HasOne(c => c.Vessel)
                .WithMany(v => v.Consumptions)
                .HasForeignKey(c => c.VesselId);

            modelBuilder.Entity<Allocation>()
                .HasOne(a => a.Purchase)
                .WithMany(p => p.Allocations)
                .HasForeignKey(a => a.PurchaseId);

            modelBuilder.Entity<Allocation>()
                .HasOne(a => a.Consumption)
                .WithMany(c => c.Allocations)
                .HasForeignKey(a => a.ConsumptionId);

            // Seed default data
            modelBuilder.Entity<Supplier>().HasData(
                new Supplier { Id = 1, Name = "GAC", Currency = "USD", ExchangeRate = 1.0m },
                new Supplier { Id = 2, Name = "Al Manaseer", Currency = "JOD", ExchangeRate = 1.4104372m },
                new Supplier { Id = 3, Name = "Abu Younis Sons", Currency = "USD", ExchangeRate = 1.0m }
            );

            // Add indexes for performance
            modelBuilder.Entity<Purchase>()
                .HasIndex(p => p.PurchaseDate);

            modelBuilder.Entity<Consumption>()
                .HasIndex(c => c.Month);

            modelBuilder.Entity<Allocation>()
                .HasIndex(a => a.Month);
        }
    }
}