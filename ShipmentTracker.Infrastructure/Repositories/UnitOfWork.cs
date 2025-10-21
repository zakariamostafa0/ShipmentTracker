using Microsoft.EntityFrameworkCore.Storage;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Interfaces;
using ShipmentTracker.Infrastructure.Data;

namespace ShipmentTracker.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ShipmentTrackerDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ShipmentTrackerDbContext context)
    {
        _context = context;
        
        // Initialize repositories
        Users = new UserRepository(_context);
        Roles = new GenericRepository<Role>(_context);
        UserRoles = new GenericRepository<UserRole>(_context);
        RefreshTokens = new GenericRepository<RefreshToken>(_context);
        EmailVerificationTokens = new GenericRepository<EmailVerificationToken>(_context);
        PasswordResetTokens = new GenericRepository<PasswordResetToken>(_context);
        Clients = new ClientRepository(_context);
        Branches = new GenericRepository<Branch>(_context);
        Warehouses = new GenericRepository<Warehouse>(_context);
        Ports = new GenericRepository<Port>(_context);
        Carriers = new GenericRepository<Carrier>(_context);
        Batches = new BatchRepository(_context);
        Shipments = new ShipmentRepository(_context);
        ShipmentEvents = new GenericRepository<ShipmentEvent>(_context);
        Announcements = new AnnouncementRepository(_context);
        AnnouncementTargets = new GenericRepository<AnnouncementTarget>(_context);
        AuditHeaders = new GenericRepository<AuditHeader>(_context);
        AuditDetails = new GenericRepository<AuditDetail>(_context);
        OutboxEvents = new GenericRepository<OutboxEvent>(_context);
    }

    public IUserRepository Users { get; }
    public IRepository<Role> Roles { get; }
    public IRepository<UserRole> UserRoles { get; }
    public IRepository<RefreshToken> RefreshTokens { get; }
    public IRepository<EmailVerificationToken> EmailVerificationTokens { get; }
    public IRepository<PasswordResetToken> PasswordResetTokens { get; }
    public IClientRepository Clients { get; }
    public IRepository<Branch> Branches { get; }
    public IRepository<Warehouse> Warehouses { get; }
    public IRepository<Port> Ports { get; }
    public IRepository<Carrier> Carriers { get; }
    public IBatchRepository Batches { get; }
    public IShipmentRepository Shipments { get; }
    public IRepository<ShipmentEvent> ShipmentEvents { get; }
    public IAnnouncementRepository Announcements { get; }
    public IRepository<AnnouncementTarget> AnnouncementTargets { get; }
    public IRepository<AuditHeader> AuditHeaders { get; }
    public IRepository<AuditDetail> AuditDetails { get; }
    public IRepository<OutboxEvent> OutboxEvents { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
