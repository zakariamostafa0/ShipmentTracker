using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentTracker.API.DTOs.Auth;
using ShipmentTracker.API.DTOs.Common;
using ShipmentTracker.API.DTOs.User;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Enums;
using ShipmentTracker.Core.Interfaces;
using BCrypt.Net;
using System.Security.Claims;

namespace ShipmentTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IEmailService emailService,
        IMapper mapper,
        ILogger<AuthController> logger)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Validate primary phone number format first
            if (!IsValidPhoneNumber(request.PhoneNumber))
            {
                return BadRequest(new { message = $"Invalid primary phone number format: {request.PhoneNumber}" });
            }

            // Validate additional phone numbers if provided
            if (request.AdditionalPhoneNumbers != null && request.AdditionalPhoneNumbers.Any())
            {
                foreach (var phoneNumber in request.AdditionalPhoneNumbers)
                {
                    if (!string.IsNullOrWhiteSpace(phoneNumber))
                    {
                        if (!IsValidPhoneNumber(phoneNumber))
                        {
                            return BadRequest(new { message = $"Invalid additional phone number format: {phoneNumber}" });
                        }
                    }
                }
            }

            // Check if username already exists
            if (!await _unitOfWork.Users.IsUserNameUniqueAsync(request.UserName))
            {
                return BadRequest(new { message = "Username already exists" });
            }

            // Check if email already exists
            if (!await _unitOfWork.Users.IsEmailUniqueAsync(request.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Create user
            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                DisplayName = request.DisplayName,
                Gender = request.Gender,
                PasswordHash = System.Text.Encoding.UTF8.GetBytes(BCrypt.Net.BCrypt.HashPassword(request.Password)),
                IsActive = false, // Will be activated after email verification
                EmailVerified = false
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Assign Client role by default
            var clientRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.RoleType == RoleType.Client);
            if (clientRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = clientRole.Id
                };
                await _unitOfWork.UserRoles.AddAsync(userRole);
            }

            // Create client record with primary phone
            var client = new Client
            {
                UserId = user.Id,
                PhoneNumber = request.PhoneNumber // Now required
            };
            await _unitOfWork.Clients.AddAsync(client);

            // Add primary phone to UserPhoneNumber table
            var primaryPhone = new UserPhoneNumber
            {
                UserId = user.Id,
                PhoneNumber = request.PhoneNumber,
                IsPrimary = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.UserPhoneNumbers.AddAsync(primaryPhone);

            // Add additional phone numbers if provided
            if (request.AdditionalPhoneNumbers != null && request.AdditionalPhoneNumbers.Any())
            {
                foreach (var phoneNumber in request.AdditionalPhoneNumbers)
                {
                    if (!string.IsNullOrWhiteSpace(phoneNumber))
                    {
                        var additionalPhone = new UserPhoneNumber
                        {
                            UserId = user.Id,
                            PhoneNumber = phoneNumber,
                            IsPrimary = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.UserPhoneNumbers.AddAsync(additionalPhone);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // Generate email verification token and send email
            var verificationToken = await _tokenService.CreateEmailVerificationTokenAsync(user.Id);
            await _emailService.SendEmailVerificationAsync(user.Email, user.DisplayName, verificationToken.Token);

            _logger.LogInformation("User {UserName} registered successfully", user.UserName);

            return Ok(new { message = "Registration successful. Please check your email to verify your account.", userId = user.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        try
        {
            // Validate token
            if (!await _tokenService.ValidateEmailVerificationTokenAsync(token))
            {
                return BadRequest(new { message = "Invalid or expired verification token" });
            }

            // Get the verification token record
            var verificationToken = await _unitOfWork.EmailVerificationTokens.FirstOrDefaultAsync(evt => evt.Token == token);
            if (verificationToken == null)
            {
                return BadRequest(new { message = "Invalid verification token" });
            }

            // Get user and activate account
            var user = await _unitOfWork.Users.GetByIdAsync(verificationToken.UserId);
            if (user == null)
            {
                return BadRequest(new { message = "User not found" });
            }

            user.EmailVerified = true;
            user.IsActive = true;
            await _unitOfWork.Users.UpdateAsync(user);

            // Mark token as used
            await _tokenService.MarkEmailVerificationTokenAsUsedAsync(token);

            await _unitOfWork.SaveChangesAsync();

            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(user.Email, user.DisplayName);

            _logger.LogInformation("User {UserName} email verified successfully", user.UserName);

            return Ok(new { message = "Email verified successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification");
            return StatusCode(500, new { message = "An error occurred during email verification" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Get user with roles
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
            if (user != null)
            {
                user = await _unitOfWork.Users.GetWithRolesAsync(user.Id);
            }
            
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            // Check if user is active and email is verified
            if (!user.IsActive || !user.EmailVerified)
            {
                return Unauthorized(new { message = "Account not activated. Please verify your email first." });
            }

            // Verify password
            var passwordHashString = System.Text.Encoding.UTF8.GetString(user.PasswordHash);
            if (!BCrypt.Net.BCrypt.Verify(request.Password, passwordHashString))
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            // Get user roles
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshTokenString = _tokenService.GenerateRefreshToken();
            var refreshToken = await _tokenService.CreateRefreshTokenAsync(user.Id, refreshTokenString);

            var response = new AuthResponse(
                accessToken,
                refreshTokenString,
                DateTime.UtcNow.AddMinutes(15),
                roles,
                user.Id,
                user.UserName,
                user.DisplayName,
                user.Email
            );

            _logger.LogInformation("User {UserName} logged in successfully", user.UserName);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            // Validate refresh token
            if (!await _tokenService.ValidateRefreshTokenAsync(request.RefreshToken))
            {
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            // Get refresh token record
            var refreshTokenRecord = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
            if (refreshTokenRecord == null)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            // Get user with roles
            var user = await _unitOfWork.Users.GetWithRolesAsync(refreshTokenRecord.UserId);
            if (user == null || !user.IsActive || !user.EmailVerified)
            {
                return Unauthorized(new { message = "User not found or inactive" });
            }

            // Revoke old refresh token
            await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken);

            // Generate new tokens
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var newRefreshTokenString = _tokenService.GenerateRefreshToken();
            var newRefreshToken = await _tokenService.CreateRefreshTokenAsync(user.Id, newRefreshTokenString);

            var response = new AuthResponse(
                accessToken,
                newRefreshTokenString,
                DateTime.UtcNow.AddMinutes(15),
                roles,
                user.Id,
                user.UserName,
                user.DisplayName,
                user.Email
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (user == null)
            {
                // Don't reveal if email exists or not
                return Ok(new { message = "If the email exists, a password reset link has been sent." });
            }

            // Generate password reset token
            var resetToken = await _tokenService.CreatePasswordResetTokenAsync(user.Id);
            await _emailService.SendPasswordResetAsync(user.Email, user.DisplayName, resetToken.Token);

            _logger.LogInformation("Password reset requested for user {UserName}", user.UserName);

            return Ok(new { message = "If the email exists, a password reset link has been sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password");
            return StatusCode(500, new { message = "An error occurred during password reset request" });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromQuery] string token, [FromBody] NewPasswordRequest request)
    {
        try
        {
            // Validate token
            if (!await _tokenService.ValidatePasswordResetTokenAsync(token))
            {
                return BadRequest(new { message = "Invalid or expired reset token" });
            }

            // Get the reset token record
            var resetToken = await _unitOfWork.PasswordResetTokens.FirstOrDefaultAsync(prt => prt.Token == token);
            if (resetToken == null)
            {
                return BadRequest(new { message = "Invalid reset token" });
            }

            // Get user and update password
            var user = await _unitOfWork.Users.GetByIdAsync(resetToken.UserId);
            if (user == null)
            {
                return BadRequest(new { message = "User not found" });
            }

            user.PasswordHash = System.Text.Encoding.UTF8.GetBytes(BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
            await _unitOfWork.Users.UpdateAsync(user);

            // Mark token as used
            await _tokenService.MarkPasswordResetTokenAsUsedAsync(token);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Password reset successfully for user {UserName}", user.UserName);

            return Ok(new { message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new { message = "An error occurred during password reset" });
        }
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Email not found" });
            }

            if (user.EmailVerified)
            {
                return BadRequest(new { message = "Email already verified" });
            }

            // Generate new verification token
            var verificationToken = await _tokenService.CreateEmailVerificationTokenAsync(user.Id);
            await _emailService.SendEmailVerificationAsync(user.Email, user.DisplayName, verificationToken.Token);

            _logger.LogInformation("Verification email resent for user {UserName}", user.UserName);

            return Ok(new { message = "Verification email sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resend verification");
            return StatusCode(500, new { message = "An error occurred while sending verification email" });
        }
    }

    // User Management Endpoints (Admin only)
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserResponse>>>> GetUsers()
    {
        try
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            var userResponses = _mapper.Map<List<UserResponse>>(users);
            return Ok(ApiResponse<List<UserResponse>>.SuccessResult(userResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, ApiResponse<List<UserResponse>>.ErrorResult("An error occurred while retrieving users"));
        }
    }

    [HttpGet("users/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetUser(long id)
    {
        try
        {
            var user = await _unitOfWork.Users.GetWithRolesAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<UserResponse>.ErrorResult("User not found"));
            }

            var userResponse = _mapper.Map<UserResponse>(user);
            return Ok(ApiResponse<UserResponse>.SuccessResult(userResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, ApiResponse<UserResponse>.ErrorResult("An error occurred while retrieving user"));
        }
    }

    [HttpPut("users/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateUser(long id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<UserResponse>.ErrorResult("User not found"));
            }

            // Check if email already exists (excluding current user)
            if (!await _unitOfWork.Users.IsEmailUniqueAsync(request.Email, id))
            {
                return BadRequest(ApiResponse<UserResponse>.ErrorResult("Email already exists"));
            }

            _mapper.Map(request, user);
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var userResponse = _mapper.Map<UserResponse>(user);
            return Ok(ApiResponse<UserResponse>.SuccessResult(userResponse, "User updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, ApiResponse<UserResponse>.ErrorResult("An error occurred while updating user"));
        }
    }

    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeactivateUser(long id)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse.ErrorResult("User not found"));
            }

            // Don't allow deactivating the last admin
            var adminRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.RoleType == RoleType.Admin);
            if (adminRole != null)
            {
                var adminUsers = await _unitOfWork.UserRoles.CountAsync(ur => ur.RoleId == adminRole.Id);
                var isCurrentUserAdmin = await _unitOfWork.UserRoles.ExistsAsync(ur => ur.UserId == id && ur.RoleId == adminRole.Id);
                
                if (isCurrentUserAdmin && adminUsers <= 1)
                {
                    return BadRequest(ApiResponse.ErrorResult("Cannot deactivate the last admin user"));
                }
            }

            user.IsActive = false;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deactivated", id);
            return Ok(ApiResponse.SuccessResult("User deactivated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deactivating user"));
        }
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var cleaned = phoneNumber.Trim();
        
        // Allow formats like: +1234567890, 1234567890, +1-234-567-890, (123) 456-7890
        // Remove common separators but keep + at the beginning
        var normalized = cleaned.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "");
        
        // Must start with + for international format or be all digits
        if (!normalized.StartsWith("+") && !normalized.All(c => char.IsDigit(c)))
            return false;

        // Remove + and check if remaining characters are digits
        var digitsOnly = normalized.StartsWith("+") ? normalized.Substring(1) : normalized;
        
        // Must have at least 7 digits and at most 15 digits (international standard)
        return digitsOnly.All(char.IsDigit) && digitsOnly.Length >= 7 && digitsOnly.Length <= 15;
    }
}
