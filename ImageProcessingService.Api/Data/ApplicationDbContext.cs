using ImageProcessingService.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ImageProcessingService.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<ImageAsset> Images => Set<ImageAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(user => user.Username)
            .IsUnique();

        modelBuilder.Entity<ImageAsset>()
            .HasIndex(image => image.StorageKey)
            .IsUnique();

        modelBuilder.Entity<ImageAsset>()
            .HasOne(image => image.User)
            .WithMany(user => user.Images)
            .HasForeignKey(image => image.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ImageAsset>()
            .HasOne(image => image.OriginalImage)
            .WithMany()
            .HasForeignKey(image => image.OriginalImageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
