using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence
{
    public partial class CatalogServiceDbContext
    {
        public virtual DbSet<IdempotentRequest> IdempotentRequests { get; set; }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IdempotentRequest>(entity =>
            {
                entity.ToTable("IdempotentRequests");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.Key, e.UserId })
                    .HasDatabaseName("UQ_IdempotentRequests_KeyUser")
                    .IsUnique();

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.RequestHash)
                    .HasMaxLength(256);

                entity.Property(e => e.ResponseBody)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.CreatedOn)
                    .HasDefaultValueSql("(sysutcdatetime())");

                entity.Property(e => e.LockedUntil)
                    .HasColumnType("datetime2");

                entity.Property(e => e.ExpiresOn)
                    .HasColumnType("datetime2");
            });
        }
    }
}
