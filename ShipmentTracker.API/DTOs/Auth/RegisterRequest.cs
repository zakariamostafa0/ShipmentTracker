using System.ComponentModel.DataAnnotations;
using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.API.DTOs.Auth;

public record RegisterRequest(
    [Required]
    [StringLength(50, MinimumLength = 3)]
    string UserName,
    
    [Required]
    [EmailAddress]
    [StringLength(100)]
    string Email,
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    string DisplayName,
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    string Password,
    
    [Required]
    [Phone]
    [StringLength(20)]
    string PhoneNumber,
    
    [Required]
    Gender Gender,
    
    List<string>? AdditionalPhoneNumbers
);
