using Microsoft.EntityFrameworkCore;

namespace WolverineSagas.ApiService;

public class KafkaSagaDbContext : DbContext
{
    public DbSet<KafkaSaga> Sagas { get; set; } = null!;

    public KafkaSagaDbContext(DbContextOptions<KafkaSagaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KafkaSaga>().ToTable("kafka_sagas");
        modelBuilder.Entity<KafkaSaga>().HasKey(s => s.Id);
        modelBuilder.Entity<KafkaSaga>().Property(s => s.Id).HasColumnName("id");
        modelBuilder.Entity<KafkaSaga>().Property(s => s.Content).IsRequired(false).HasColumnName("content");
        modelBuilder.Entity<KafkaSaga>().Property(s => s.Version).IsRequired().HasColumnName("version");
        modelBuilder.Entity<KafkaSaga>().Property(s => s.State).IsRequired().HasConversion<int>().HasColumnName("state");
        modelBuilder.Entity<KafkaSaga>().Property(s => s.Message).IsRequired(false).HasColumnName("message");

        base.OnModelCreating(modelBuilder);
    }
}