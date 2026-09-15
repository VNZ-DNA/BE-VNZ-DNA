using Microsoft.EntityFrameworkCore;

namespace VNZ.Repository;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Chưa khai báo DbSet vì database/entity chưa được chốt.
    // Khi ERD hoàn tất, thêm DbSet<TEntity> tại đây.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Khi chốt entity, đặt Fluent API/configuration ở đây
        // hoặc tách sang các IEntityTypeConfiguration<TEntity>.
    }
}
