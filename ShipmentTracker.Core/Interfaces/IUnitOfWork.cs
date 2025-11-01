using ShipmentTracker.Core.Entities;

namespace ShipmentTracker.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRepository<Role> Roles { get; }
    IRepository<UserRole> UserRoles { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<EmailVerificationToken> EmailVerificationTokens { get; }
    IRepository<PasswordResetToken> PasswordResetTokens { get; }
    IClientRepository Clients { get; }
    IRepository<UserPhoneNumber> UserPhoneNumbers { get; }
    IRepository<Branch> Branches { get; }
    IRepository<Warehouse> Warehouses { get; }
    IRepository<Port> Ports { get; }
    IRepository<Carrier> Carriers { get; }
    IBatchRepository Batches { get; }
    IShipmentRepository Shipments { get; }
    IRepository<ShipmentEvent> ShipmentEvents { get; }
    IAnnouncementRepository Announcements { get; }
    IRepository<AnnouncementTarget> AnnouncementTargets { get; }
    IRepository<AuditHeader> AuditHeaders { get; }
    IRepository<AuditDetail> AuditDetails { get; }
    IRepository<OutboxEvent> OutboxEvents { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
