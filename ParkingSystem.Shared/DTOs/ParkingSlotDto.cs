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

