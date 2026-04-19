using ImageProcessingService.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ImageProcessingService.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<ImageAsset> Images => Set<ImageAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var utcDateTimeConverter = new ValueConverter<DateTimeOffset, DateTime>(
            value => value.UtcDateTime,
            value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(user => user.CreatedAtUtc)
            .HasConversion(utcDateTimeConverter);

        modelBuilder.Entity<ImageAsset>()
            .HasIndex(image => image.StorageKey)
            .IsUnique();

        modelBuilder.Entity<ImageAsset>()
            .Property(image => image.CreatedAtUtc)
            .HasConversion(utcDateTimeConverter);

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
