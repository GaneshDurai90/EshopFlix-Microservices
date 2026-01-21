using Microsoft.EntityFrameworkCore;
using StockService.Domain.Entities;

namespace StockService.Infrastructure.Persistence;

/// <summary>
/// Partial class extension for StockServiceDbContext to add IdempotentRequests support.
/// </summary>
public partial class StockServiceDbContext
{
    public virtual DbSet<IdempotentRequest> IdempotentRequests { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdempotentRequest>(entity =>
        {
            entity.ToTable("IdempotentRequests");
            
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => new { e.Key, e.UserId })
                .IsUnique()
                .HasDatabaseName("UQ_IdempotentRequests_Key_UserId");

            entity.Property(e => e.Key)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.RequestHash)
                .HasMaxLength(88);

            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("(sysutcdatetime())");
        });
    }
}
