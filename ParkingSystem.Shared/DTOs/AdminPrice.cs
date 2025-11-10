using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingSystem.Shared.DTOs
{
    public class UpsertPriceRequest
    {
        public Guid? PriceId { get; set; } // Null nếu tạo mới
        public string VehicleType { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Response sau khi tạo/update giá
    /// </summary>
    public class UpsertPriceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ParkingPriceDto? Price { get; set; }
    }

    /// <summary>
    /// Request xóa giá (soft delete)
    /// </summary>
    public class DeletePriceRequest
    {
        public Guid PriceId { get; set; }
    }

    /// <summary>
    /// Response sau khi xóa
    /// </summary>
    public class DeletePriceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Extended DTO với thêm thông tin
    /// </summary>
    public class ParkingPriceDetailDto : ParkingPriceDto
    {
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string StatusDisplay => IsActive ? "Đang áp dụng" : "Ngừng áp dụng";
        public string PriceDisplay => $"{PricePerHour:N0} VNĐ/giờ";
    }
}
