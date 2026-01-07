using ComplaintAnalysis.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplaintAnalysis.Infrastructure.Data;

public class LabelConfigurationDbContext : DbContext
{
    public LabelConfigurationDbContext(DbContextOptions<LabelConfigurationDbContext> options)
        : base(options)
    {
    }

    public DbSet<LabelConfigurationEntity> LabelConfigurations { get; set; }
    public DbSet<LabelRuleEntity> LabelRules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LabelConfigurationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Version).IsUnique();
            entity.HasMany(e => e.Labels)
                .WithOne(e => e.Configuration)
                .HasForeignKey(e => e.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LabelRuleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ConfigurationId, e.LabelId }).IsUnique();
        });
    }
}

