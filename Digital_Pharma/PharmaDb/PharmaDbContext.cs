using Microsoft.EntityFrameworkCore;
using Digital_Pharma.Models; // هذا namespace اللي فيه User, Doctor, Search

namespace DB.DBB
    {
        public class PharmaDbContext : DbContext
        {
            // Constructor
            public PharmaDbContext(DbContextOptions<PharmaDbContext> options)
                : base(options)
            {
            }

            // DbSets لكل جدول
            public DbSet<User> Users { get; set; }
            public DbSet<Doctor> Doctors { get; set; }
            public DbSet<Search> Searches { get; set; }

            // تحديد علاقات الجداول
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                // علاقة User ↔ Search (One-to-Many)
                modelBuilder.Entity<User>()
                    .HasMany(u => u.Searches)
                    .WithOne(s => s.User)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // علاقة Doctor ↔ User (One-to-One)
                modelBuilder.Entity<Doctor>()
                    .HasOne(d => d.User)
                    .WithOne()
                    .HasForeignKey<Doctor>(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
        }
    }

