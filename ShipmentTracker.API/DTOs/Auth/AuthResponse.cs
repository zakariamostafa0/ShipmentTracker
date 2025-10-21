namespace ShipmentTracker.API.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    IEnumerable<string> Roles,
    long UserId,
    string UserName,
    string DisplayName,
    string Email
);
