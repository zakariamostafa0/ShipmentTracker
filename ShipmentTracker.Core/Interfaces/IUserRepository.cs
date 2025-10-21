using ShipmentTracker.Core.Entities;

namespace ShipmentTracker.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUserNameAsync(string userName);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetWithRolesAsync(long userId);
    Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName);
    Task<bool> IsEmailUniqueAsync(string email, long? excludeUserId = null);
    Task<bool> IsUserNameUniqueAsync(string userName, long? excludeUserId = null);
}
