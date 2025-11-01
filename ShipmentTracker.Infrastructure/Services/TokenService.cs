using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ShipmentTracker.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpiryExpirydaysInt;

    public TokenService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _jwtSecret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        _jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        _jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
        _jwtExpiryExpirydaysInt = int.Parse(_configuration["Jwt:Expirydays"] ?? "1");
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("displayName", user.DisplayName)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(_jwtExpiryExpirydaysInt), // 1 Day
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(long userId, string token)
    {
        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days
            IsRevoked = false
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();
        return refreshToken;
    }

    public async Task<EmailVerificationToken> CreateEmailVerificationTokenAsync(long userId)
    {
        var token = Guid.NewGuid().ToString();
        var verificationToken = new EmailVerificationToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddHours(24), // 24 hours
            IsUsed = false
        };

        await _unitOfWork.EmailVerificationTokens.AddAsync(verificationToken);
        await _unitOfWork.SaveChangesAsync();
        return verificationToken;
    }

    public async Task<PasswordResetToken> CreatePasswordResetTokenAsync(long userId)
    {
        var token = Guid.NewGuid().ToString();
        var resetToken = new PasswordResetToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour
            IsUsed = false
        };

        await _unitOfWork.PasswordResetTokens.AddAsync(resetToken);
        await _unitOfWork.SaveChangesAsync();
        return resetToken;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token)
    {
        var refreshToken = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(rt => 
            rt.Token == token && 
            !rt.IsRevoked && 
            rt.ExpiresAt > DateTime.UtcNow);

        return refreshToken != null;
    }

    public async Task<bool> ValidateEmailVerificationTokenAsync(string token)
    {
        var verificationToken = await _unitOfWork.EmailVerificationTokens.FirstOrDefaultAsync(evt => 
            evt.Token == token && 
            !evt.IsUsed && 
            evt.ExpiresAt > DateTime.UtcNow);

        return verificationToken != null;
    }

    public async Task<bool> ValidatePasswordResetTokenAsync(string token)
    {
        var resetToken = await _unitOfWork.PasswordResetTokens.FirstOrDefaultAsync(prt => 
            prt.Token == token && 
            !prt.IsUsed && 
            prt.ExpiresAt > DateTime.UtcNow);

        return resetToken != null;
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var refreshToken = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        if (refreshToken != null)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.RefreshTokens.UpdateAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task MarkEmailVerificationTokenAsUsedAsync(string token)
    {
        var verificationToken = await _unitOfWork.EmailVerificationTokens.FirstOrDefaultAsync(evt => evt.Token == token);
        if (verificationToken != null)
        {
            verificationToken.IsUsed = true;
            verificationToken.UsedAt = DateTime.UtcNow;
            await _unitOfWork.EmailVerificationTokens.UpdateAsync(verificationToken);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task MarkPasswordResetTokenAsUsedAsync(string token)
    {
        var resetToken = await _unitOfWork.PasswordResetTokens.FirstOrDefaultAsync(prt => prt.Token == token);
        if (resetToken != null)
        {
            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.UtcNow;
            await _unitOfWork.PasswordResetTokens.UpdateAsync(resetToken);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
