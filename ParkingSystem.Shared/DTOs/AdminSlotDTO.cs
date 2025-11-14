// Thêm vào ParkingSystem.Shared/DTOs/

namespace ParkingSystem.Shared.DTOs
{
    /// <summary>
    /// DTO cho Area (tự động group từ SlotCode)
    /// </summary>
    public class ParkingAreaGroupDto
    {
        public string AreaPrefix { get; set; } = string.Empty; // "A", "B", "VIP"...
        public string AreaName { get; set; } = string.Empty; // "Khu A", "Khu B"...
        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }
        public int InUseSlots { get; set; }
        public int MaintenanceSlots { get; set; }
        public int ReservedSlots { get; set; }
        public List<SlotManagementDto> Slots { get; set; } = new();
        
        public double OccupancyRate => TotalSlots > 0 
            ? Math.Round((double)(InUseSlots + ReservedSlots) / TotalSlots * 100, 1) 
            : 0;
    }

    /// <summary>
    /// DTO chi tiết slot cho admin management
    /// </summary>
    public class SlotManagementDto
    {
        public Guid SlotId { get; set; }
        public string SlotCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Available, InUse, Maintenance, Reserved
        public bool HasActiveRegistration { get; set; }
        
        // Thông tin registration hiện tại (nếu có)
        public Guid? CurrentRegistrationId { get; set; }
        public string? VehiclePlateNumber { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? CheckInTime { get; set; }

        // Computed
        public bool CanDelete => Status == "Available" && !HasActiveRegistration;
        public string StatusDisplay => Status switch
        {
            "Available" => "Sẵn sàng",
            "InUse" => "Đang sử dụng",
            "Maintenance" => "Bảo trì",
            _ => Status
        };
    }

    /// <summary>
    /// Request tạo slot mới
    /// </summary>
    public class CreateSlotRequest
    {
        public string SlotCode { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Default: Available
    }

    /// <summary>
    /// Request cập nhật slot
    /// </summary>
    public class UpdateSlotRequest
    {
        public Guid SlotId { get; set; }
        public string SlotCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request tạo nhiều slots cùng lúc (bulk)
    /// </summary>
    public class BulkCreateSlotsRequest
    {
        public string Prefix { get; set; } = string.Empty; // "A", "B", "VIP"...
        public int StartNumber { get; set; }
        public int EndNumber { get; set; }
        public string Status { get; set; } = "Available";
    }

    /// <summary>
    /// Response sau khi thao tác slot
    /// </summary>
    public class SlotOperationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public SlotManagementDto? Slot { get; set; }
        public List<SlotManagementDto>? Slots { get; set; } // For bulk operations
    }

    /// <summary>
    /// Thống kê tổng quan slots
    /// </summary>
    public class SlotsStatisticsDto
    {
        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }
        public int InUseSlots { get; set; }
        public int MaintenanceSlots { get; set; }
        public int ReservedSlots { get; set; }
        public int TotalAreas { get; set; }
        public double OccupancyRate { get; set; }
    }

    /// <summary>
    /// Request xóa slot
    /// </summary>
    public class DeleteSlotRequest
    {
        public Guid SlotId { get; set; }
        public bool ForceDelete { get; set; } = false; // Admin có thể force delete
    }

    /// <summary>
    /// Request xóa nhiều slots
    /// </summary>
    public class BulkDeleteSlotsRequest
    {
        public List<Guid> SlotIds { get; set; } = new();
        public bool ForceDelete { get; set; } = false;
    }

    /// <summary>
    /// Response xóa slots
    /// </summary>
    public class DeleteSlotResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DeletedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}