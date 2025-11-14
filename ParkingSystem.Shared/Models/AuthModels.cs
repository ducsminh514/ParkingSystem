namespace ParkingSystem.Shared.Models;
using System.ComponentModel.DataAnnotations;

// Request đăng ký
public class RegisterRequest
{
    [Required(ErrorMessage = "Họ và tên là bắt buộc")]
    [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [RegularExpression(@"^\+?\d{9,15}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }
}

// Request đăng nhập
public class LoginRequest
{
    public string UsernameOrEmail { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool IsStaff { get; set; } // true = Staff, false = Customer
}

// Response sau khi đăng nhập/đăng ký
public class AuthResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UserInfo? UserInfo { get; set; }
}

// Thông tin user
public class UserInfo
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string UserType { get; set; } = null!; // "Customer" hoặc "Staff"
    public string? Email { get; set; }
    public string? Phone { get; set; }
}