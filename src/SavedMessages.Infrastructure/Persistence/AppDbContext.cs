using Microsoft.EntityFrameworkCore;
using SavedMessages.Domain.Entities;

namespace SavedMessages.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<UserE2eeSettings> UserE2eeSettings => Set<UserE2eeSettings>();
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();
    public DbSet<TransferSession> TransferSessions => Set<TransferSession>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
            e.Property(u => u.AvatarUrl).HasMaxLength(2048);
        });

        // ── ExternalLogin ────────────────────────────────────────────────────
        modelBuilder.Entity<ExternalLogin>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Provider, x.ProviderKey }).IsUnique();
            e.Property(x => x.Provider).IsRequired().HasMaxLength(50);
            e.Property(x => x.ProviderKey).IsRequired().HasMaxLength(256);

            e.HasOne(x => x.User)
                .WithMany(u => u.ExternalLogins)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Device ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Device>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Name).IsRequired().HasMaxLength(100);
            e.Property(d => d.Platform)
                .HasConversion<string>()
                .HasMaxLength(20);
            e.Property(d => d.PushToken).HasMaxLength(512);

            e.HasOne(d => d.User)
                .WithMany(u => u.Devices)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Message ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Message>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Kind)
                .HasConversion<string>()
                .HasMaxLength(10);
            e.Property(m => m.EncryptionIV).HasMaxLength(24);
            e.HasIndex(m => new { m.UserId, m.IsDeleted, m.CreatedAt });

            e.HasOne(m => m.User)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.FileAttachment)
                .WithMany()
                .HasForeignKey(m => m.FileId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── UserE2eeSettings ─────────────────────────────────────────────────
        modelBuilder.Entity<UserE2eeSettings>(e =>
        {
            e.HasKey(s => s.UserId);
            e.Property(s => s.KdfAlgorithm).IsRequired().HasMaxLength(50);
            e.Property(s => s.KdfSalt).IsRequired().HasMaxLength(256);
            e.Property(s => s.KdfParams).IsRequired();
            e.Property(s => s.KeyVerifier).IsRequired().HasMaxLength(512);

            e.HasOne(s => s.User)
                .WithOne(u => u.E2eeSettings)
                .HasForeignKey<UserE2eeSettings>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── FileAttachment ───────────────────────────────────────────────────
        modelBuilder.Entity<FileAttachment>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.OriginalName).IsRequired().HasMaxLength(256);
            e.Property(f => f.ContentType).IsRequired().HasMaxLength(100);
            e.Property(f => f.BlobPath).IsRequired().HasMaxLength(1024);
            e.Property(f => f.PreviewBlobPath).HasMaxLength(1024);

            e.HasOne(f => f.User)
                .WithMany(u => u.FileAttachments)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TransferSession ──────────────────────────────────────────────────
        modelBuilder.Entity<TransferSession>(e =>
        {
            e.HasKey(t => t.Id);
        });

        // ── RefreshToken ──────────────────────────────────────────────────────
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Token).IsUnique();
            e.Property(r => r.Token).IsRequired().HasMaxLength(256);

            e.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Ignore(r => r.IsRevoked);
            e.Ignore(r => r.IsExpired);
            e.Ignore(r => r.IsActive);
        });

        // ── ShareLink ────────────────────────────────────────────────────────
        modelBuilder.Entity<ShareLink>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Slug).IsUnique();
            e.Property(s => s.Slug).IsRequired().HasMaxLength(20);

            e.HasOne(s => s.Message)
                .WithMany(m => m.ShareLinks)
                .HasForeignKey(s => s.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.User)
                .WithMany(u => u.ShareLinks)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
