using System;

namespace ParkingSystem.Shared.DTOs;

/// <summary>
/// DTO để hiển thị thông tin slot trong client
/// </summary>
public class ParkingSlotDto
{
    public Guid SlotId { get; set; }
    public string SlotCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public bool IsAvailable => Status == "Available";
    
    // Thông tin đăng ký hiện tại (nếu slot đang được sử dụng)
    public Guid? CurrentRegistrationId { get; set; }
    public string? VehiclePlateNumber { get; set; }
    public string? VehicleType { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime? CheckInTime { get; set; }
    
    // Thời gian đỗ (tính từ check-in đến hiện tại)
    public string? ParkingDuration 
    {
        get
        {
            if (CheckInTime.HasValue && !IsAvailable)
            {
                var duration = DateTime.Now - CheckInTime.Value;
                if (duration.TotalDays >= 1)
                    return $"{(int)duration.TotalDays} ngày {duration.Hours} giờ";
                else if (duration.TotalHours >= 1)
                    return $"{(int)duration.TotalHours} giờ {duration.Minutes} phút";
                else
                    return $"{(int)duration.TotalMinutes} phút";
            }
            return null;
        }
    }
}

public class ParkingHistoryDto
{
    public Guid RegistrationID { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string SlotCode { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = string.Empty; // InUse, CheckedOut
    public string? StaffName { get; set; }

    // Computed properties
    public string Duration
    {
        get
        {
            if (CheckOutTime.HasValue)
            {
                var duration = CheckOutTime.Value - CheckInTime;
                if (duration.TotalDays >= 1)
                    return $"{(int)duration.TotalDays} ngày {duration.Hours}h {duration.Minutes}m";
                else if (duration.TotalHours >= 1)
                    return $"{(int)duration.TotalHours}h {duration.Minutes}m";
                else
                    return $"{duration.Minutes}m";
            }
            else
            {
                var duration = DateTime.Now - CheckInTime;
                if (duration.TotalDays >= 1)
                    return $"{(int)duration.TotalDays} ngày {duration.Hours}h {duration.Minutes}m";
                else if (duration.TotalHours >= 1)
                    return $"{(int)duration.TotalHours}h {duration.Minutes}m";
                else
                    return $"{duration.Minutes}m";
            }
        }
    }

    public string StatusText => Status switch
    {
        "Active" => "Đang đỗ",
        "CheckedOut" => "Đã hoàn thành",
        _ => "Không xác định"
    };

    public string StatusColor => Status switch
    {
        "Active" => "success",
        "CheckedOut" => "secondary",
        _ => "warning"
    };

    public bool IsActive => Status == "Active";
}

public class ParkingHistoryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ParkingHistoryDto> Histories { get; set; } = new();
    public int TotalRecords { get; set; }
    public int ActiveParking { get; set; } // Số xe đang đỗ
}

/// <summary>
/// DTO để nhóm các slot theo khu vực/bãi đỗ
/// </summary>
public class ParkingAreaDto
{
    public string AreaName { get; set; } = null!;
    public int TotalSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public List<ParkingSlotDto> Slots { get; set; } = new();
    
    public double OccupancyRate => TotalSlots > 0 
        ? Math.Round((double)OccupiedSlots / TotalSlots * 100, 1) 
        : 0;
}

/// <summary>
/// DTO tổng quan về trạng thái bãi đỗ
/// </summary>
public class ParkingOverviewDto
{
    public int TotalSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public double OccupancyRate { get; set; }
}

/// <summary>
/// DTO cho Customer - hiển thị thông tin chi tiết CHỈ KHI là slot của chính họ
/// </summary>
public class CustomerParkingSlotDto
{
    public Guid SlotId { get; set; }
    public string SlotCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public bool IsAvailable => Status == "Available";
    
    // Thông tin đăng ký (chỉ hiển thị nếu là slot của chính customer này)
    public bool IsMySlot { get; set; }  // Đánh dấu slot này có phải của customer hiện tại không
    public Guid? CurrentRegistrationId { get; set; }
    public string? VehiclePlateNumber { get; set; }
    public string? VehicleType { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime? CheckInTime { get; set; }
    
    // Thời gian đỗ (chỉ hiển thị cho slot của mình)
    public string? ParkingDuration 
    {
        get
        {
            if (CheckInTime.HasValue && !IsAvailable && IsMySlot)
            {
                var duration = DateTime.Now - CheckInTime.Value;
                if (duration.TotalDays >= 1)
                    return $"{(int)duration.TotalDays} ngày {duration.Hours} giờ";
                else if (duration.TotalHours >= 1)
                    return $"{(int)duration.TotalHours} giờ {duration.Minutes} phút";
                else
                    return $"{(int)duration.TotalMinutes} phút";
            }
            return null;
        }
    }
}

/// <summary>
/// DTO nhóm slot cho Customer view
/// </summary>
public class CustomerParkingAreaDto
{
    public string AreaName { get; set; } = null!;
    public int TotalSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public List<CustomerParkingSlotDto> Slots { get; set; } = new();
    
    public double OccupancyRate => TotalSlots > 0 
        ? Math.Round((double)OccupiedSlots / TotalSlots * 100, 1) 
        : 0;
}

/// <summary>
    /// Response khi Staff kiểm tra số điện thoại customer
    /// </summary>
    public class CustomerCheckResult
    {
        public bool Exists { get; set; }
        public Guid? CustomerId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string Phone { get; set; } = string.Empty;
        public List<VehicleSummaryDto> Vehicles { get; set; } = new();
    }

    /// <summary>
    /// Thông tin tóm tắt về xe của customer
    /// </summary>
    public class VehicleSummaryDto
    {
        public Guid VehicleId { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public bool IsCurrentlyParked { get; set; }
        public string? CurrentSlotCode { get; set; } // Slot đang đỗ (nếu có)
    }

    /// <summary>
    /// Request tạo customer mới từ Staff
    /// </summary>
    public class CreateCustomerRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    /// <summary>
    /// Response sau khi tạo customer
    /// </summary>
    public class CreateCustomerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? CustomerId { get; set; }
    }

    /// <summary>
    /// Request đăng ký parking cho Staff (bao gồm cả tạo customer/vehicle mới)
    /// </summary>
    public class StaffRegisterParkingRequest
    {
        // Thông tin slot
        public Guid SlotId { get; set; }
        public Guid StaffId { get; set; }

        // Thông tin customer
        public Guid? CustomerId { get; set; } // Nếu đã tồn tại
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerName { get; set; } // Nếu tạo mới
        public string? CustomerEmail { get; set; } // Nếu tạo mới

        // Thông tin vehicle
        public Guid? VehicleId { get; set; } // Nếu chọn xe có sẵn
        public string PlateNumber { get; set; } = string.Empty; // Nếu tạo xe mới
        public string VehicleType { get; set; } = string.Empty; // Nếu tạo xe mới
    }

    /// <summary>
    /// Dữ liệu xác nhận trước khi đăng ký cuối cùng
    /// </summary>
    public class RegistrationConfirmation
    {
        public string SlotCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string VehiclePlateNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public bool IsNewCustomer { get; set; }
        public bool IsNewVehicle { get; set; }
    }

