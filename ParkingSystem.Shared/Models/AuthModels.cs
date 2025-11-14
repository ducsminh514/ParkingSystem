namespace ParkingSystem.Shared.Models;
using System.ComponentModel.DataAnnotations;

// Register Request
public class RegisterRequest
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^\+?\d{9,15}$", ErrorMessage = "Invalid phone number")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }
}

// Login Request
public class LoginRequest
{
    public string UsernameOrEmail { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool IsStaff { get; set; } // true = Staff, false = Customer
}

// Response after login/register
public class AuthResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UserInfo? UserInfo { get; set; }
}

// User information
public class UserInfo
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string UserType { get; set; } = null!; // "Customer" or "Staff"
    public string? Email { get; set; }
    public string? Phone { get; set; }
}