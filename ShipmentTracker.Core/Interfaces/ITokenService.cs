using ShipmentTracker.Core.Entities;

namespace ShipmentTracker.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    Task<RefreshToken> CreateRefreshTokenAsync(long userId, string token);
    Task<EmailVerificationToken> CreateEmailVerificationTokenAsync(long userId);
    Task<PasswordResetToken> CreatePasswordResetTokenAsync(long userId);
    Task<bool> ValidateRefreshTokenAsync(string token);
    Task<bool> ValidateEmailVerificationTokenAsync(string token);
    Task<bool> ValidatePasswordResetTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
    Task MarkEmailVerificationTokenAsUsedAsync(string token);
    Task MarkPasswordResetTokenAsUsedAsync(string token);
}
