namespace ParkingSystem.Shared.DTOs;


     public class CalculateFeeRequest
    {
        public Guid RegistrationId { get; set; }
    }

    /// <summary>
    /// Response trả về chi tiết tính phí
    /// </summary>
    public class CalculateFeeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        // Thông tin slot & xe
        public string SlotCode { get; set; } = string.Empty;
        public string VehiclePlateNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;

        // Thông tin khách hàng
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        // Thông tin thời gian
        public DateTime CheckInTime { get; set; }
        public DateTime CheckOutTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string DurationDisplay { get; set; } = string.Empty; // "3 giờ 25 phút"

        // Thông tin tính phí
        public decimal PricePerHour { get; set; }
        public double TotalHours { get; set; } // Số giờ (VD: 3.42)
        public decimal TotalAmount { get; set; } // Tổng tiền phải trả
    }

    /// <summary>
    /// Request check-out với payment
    /// </summary>
    public class CheckOutWithPaymentRequest
    {
        public Guid RegistrationId { get; set; }
        public decimal PaymentAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // "Cash", "Transfer", "Card"
        public string? Note { get; set; }
    }

    /// <summary>
    /// Response sau khi check-out thành công
    /// </summary>
    public class CheckOutWithPaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime? CheckOutTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public Guid? PaymentId { get; set; }
    }

    /// <summary>
    /// DTO cho bảng giá
    /// </summary>
    public class ParkingPriceDto
    {
        public Guid PriceId { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public bool IsActive { get; set; }
    }
