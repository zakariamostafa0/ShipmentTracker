using Microsoft.EntityFrameworkCore;
using ShipmentTracker.Core.Entities;

namespace ShipmentTracker.Infrastructure.Data;

public class ShipmentTrackerDbContext : DbContext
{
    public ShipmentTrackerDbContext(DbContextOptions<ShipmentTrackerDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<Port> Ports { get; set; }
    public DbSet<Carrier> Carriers { get; set; }
    public DbSet<Batch> Batches { get; set; }
    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<ShipmentEvent> ShipmentEvents { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<AnnouncementTarget> AnnouncementTargets { get; set; }
    public DbSet<AuditHeader> AuditHeaders { get; set; }
    public DbSet<AuditDetail> AuditDetails { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShipmentTrackerDbContext).Assembly);

        // Configure UserRole composite key
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        // Configure UserRole relationships
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        // Configure User-Client relationship (1:1)
        modelBuilder.Entity<Client>()
            .HasOne(c => c.User)
            .WithOne(u => u.Client)
            .HasForeignKey<Client>(c => c.UserId);

        // Configure Batch relationships
        modelBuilder.Entity<Batch>()
            .HasOne(b => b.Branch)
            .WithMany(br => br.Batches)
            .HasForeignKey(b => b.BranchId);

        modelBuilder.Entity<Batch>()
            .HasOne(b => b.SourceWarehouse)
            .WithMany()
            .HasForeignKey(b => b.SourceWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Batch>()
            .HasOne(b => b.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(b => b.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Batch>()
            .HasOne(b => b.SourcePort)
            .WithMany(p => p.SourceBatches)
            .HasForeignKey(b => b.SourcePortId);

        modelBuilder.Entity<Batch>()
            .HasOne(b => b.DestinationPort)
            .WithMany(p => p.DestinationBatches)
            .HasForeignKey(b => b.DestinationPortId);

        // Configure Shipment relationships
        modelBuilder.Entity<Shipment>()
            .HasOne(s => s.Client)
            .WithMany(c => c.Shipments)
            .HasForeignKey(s => s.ClientId);

        modelBuilder.Entity<Shipment>()
            .HasOne(s => s.Batch)
            .WithMany(b => b.Shipments)
            .HasForeignKey(s => s.BatchId);

        modelBuilder.Entity<Shipment>()
            .HasOne(s => s.Carrier)
            .WithMany(c => c.Shipments)
            .HasForeignKey(s => s.CarrierId);

        // Configure ShipmentEvent relationships
        modelBuilder.Entity<ShipmentEvent>()
            .HasOne(se => se.Shipment)
            .WithMany(s => s.Events)
            .HasForeignKey(se => se.ShipmentId);

        modelBuilder.Entity<ShipmentEvent>()
            .HasOne(se => se.ActorUser)
            .WithMany(u => u.ShipmentEvents)
            .HasForeignKey(se => se.ActorUserId);

        // Configure Announcement relationships
        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.CreatedByUser)
            .WithMany(u => u.CreatedAnnouncements)
            .HasForeignKey(a => a.CreatedByUserId);

        // Configure AnnouncementTarget relationships
        modelBuilder.Entity<AnnouncementTarget>()
            .HasOne(at => at.Announcement)
            .WithMany(a => a.Targets)
            .HasForeignKey(at => at.AnnouncementId);

        modelBuilder.Entity<AnnouncementTarget>()
            .HasOne(at => at.Branch)
            .WithMany(b => b.AnnouncementTargets)
            .HasForeignKey(at => at.BranchId);

        modelBuilder.Entity<AnnouncementTarget>()
            .HasOne(at => at.Client)
            .WithMany()
            .HasForeignKey(at => at.ClientId);

        // Configure Audit relationships
        modelBuilder.Entity<AuditDetail>()
            .HasOne(ad => ad.AuditHeader)
            .WithMany(ah => ah.Details)
            .HasForeignKey(ad => ad.AuditHeaderId);

        modelBuilder.Entity<AuditHeader>()
            .HasOne(ah => ah.User)
            .WithMany()
            .HasForeignKey(ah => ah.UserId);

        // Configure token relationships
        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId);

        modelBuilder.Entity<EmailVerificationToken>()
            .HasOne(evt => evt.User)
            .WithMany(u => u.EmailVerificationTokens)
            .HasForeignKey(evt => evt.UserId);

        modelBuilder.Entity<PasswordResetToken>()
            .HasOne(prt => prt.User)
            .WithMany(u => u.PasswordResetTokens)
            .HasForeignKey(prt => prt.UserId);
    }
}
