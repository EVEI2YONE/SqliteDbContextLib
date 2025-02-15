using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using SqliteDbContextLibTests.Entities;

namespace SqliteDbContextLibTests.Context
{
    public partial class EntityProjectContext : DbContext
    {
        public EntityProjectContext()
        {
        }

        public EntityProjectContext(DbContextOptions<EntityProjectContext> options)
        : base(options)
        {
        }

        public virtual DbSet<Table1> Table1 { get; set; }
        public virtual DbSet<Table2> Table2 { get; set; }
        public virtual DbSet<Table3> Table3 { get; set; }
        public virtual DbSet<Table4> Table4 { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                //(localdb)\\mssqllocaldb
                optionsBuilder.UseSqlServer("Server=localhost;Database=EntityProject;Integrated Security=True;MultipleActiveResultSets=true;Trust Server Certificate=true");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Table1>()
                    .Property(e => e.Col4)
                    .IsUnicode(false);

            modelBuilder.Entity<Table1>().ToTable("Table_1", "dbo");
            modelBuilder.Entity<Table2>().ToTable("Table_2", "dbo");
            modelBuilder.Entity<Table3>().ToTable("Table_3", "dbo");
            modelBuilder.Entity<Table4>().ToTable("Table_4", "dbo");

            modelBuilder.Entity<Table1>()
                .HasMany(e => e.Table2)
                .WithOne(e => e.Table1).IsRequired()
                .HasForeignKey(e => e.Col2_FK)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Table1>()
                .HasMany(e => e.Table3)
                .WithOne(e => e.Table1).IsRequired()
                .HasForeignKey(e => e.Col1_PKFK)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Table1>()
                .HasMany(e => e.Table4)
                .WithOne(e => e.Table1).IsRequired()
                .HasForeignKey(e => e.Col1_T1PKFK)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Table2>()
                .HasMany(e => e.Table4)
                .WithOne(e => e.Table2).IsRequired()
                .HasForeignKey(e => e.Col2_T2PKFK)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Table2>()
                .HasMany(e => e.Table3)
                .WithOne(e => e.Table2).IsRequired()
                .HasForeignKey(e => e.Col2_FK)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Table3>()
                .Property(e => e.Col3_Value)
                .IsUnicode(false);

            modelBuilder.Entity<Table3>()
                .HasMany(e => e.Table4)
                .WithOne(e => e.Table3).IsRequired()
                .HasForeignKey(e => new { e.Col3_T3PKFK_PKFK, e.Col4_T3PKFK_FK })
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Table4>()
                .Property(e => e.Col5_Value)
                .IsUnicode(false);
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}