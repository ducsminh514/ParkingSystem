namespace ParkingSystem.Shared.Models;

// Request đăng ký
public class RegisterRequest
{
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string Password { get; set; } = null!;
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