using System;

namespace ParkingSystem.Shared.DTOs;

/// <summary>
/// DTO cho request đăng ký parking
/// </summary>
public class RegisterParkingRequest
{
    public Guid SlotId { get; set; }
    
    // Thông tin khách hàng (có thể là khách hàng mới hoặc existing)
    public Guid? CustomerId { get; set; }  // Null nếu là khách hàng mới
    public string CustomerName { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;
    public string? CustomerEmail { get; set; }
    public Guid? VehicleId { get; set; }
    // Thông tin xe
    public string PlateNumber { get; set; } = null!;
    public string VehicleType { get; set; } = null!;
    
    // Thông tin đăng ký
    public Guid? StaffId { get; set; }  // Optional - nhân viên xử lý
}

/// <summary>
/// DTO cho response sau khi đăng ký
/// </summary>
public class RegisterParkingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public Guid? RegistrationId { get; set; }
    public ParkingSlotDto? UpdatedSlot { get; set; }
    
    // Thông tin chi tiết registration
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public DateTime? CheckInTime { get; set; }
}

/// <summary>
/// DTO cho request checkout
/// </summary>
public class CheckOutRequest
{
    public Guid RegistrationId { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string? PaymentMethod { get; set; }
}

/// <summary>
/// DTO cho response checkout
/// </summary>
public class CheckOutResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public DateTime? CheckOutTime { get; set; }
    public decimal? TotalAmount { get; set; }
    public TimeSpan? Duration { get; set; }
}

