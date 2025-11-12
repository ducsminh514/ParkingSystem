namespace ParkingSystem.Shared.DTOs
{
    /// <summary>
    /// DTO hiển thị thông tin Staff
    /// </summary>
    public class StaffDto
    {
        public Guid StaffId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Shift { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Thống kê
        public int TotalRegistrations { get; set; }
        public int ActiveRegistrations { get; set; }
    }

    /// <summary>
    /// Request tạo staff mới
    /// </summary>
    public class CreateStaffRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Shift { get; set; }
    }

    /// <summary>
    /// Request cập nhật staff
    /// </summary>
    public class UpdateStaffRequest
    {
        public Guid StaffId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Shift { get; set; }
        public string? NewPassword { get; set; } // Null nếu không đổi password
    }

    /// <summary>
    /// Response sau khi thao tác với staff
    /// </summary>
    public class StaffOperationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public StaffDto? Staff { get; set; }
    }

    /// <summary>
    /// Response danh sách staff
    /// </summary>
    public class StaffListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<StaffDto> Staffs { get; set; } = new();
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Request xóa staff
    /// </summary>
    public class DeleteStaffRequest
    {
        public Guid StaffId { get; set; }
    }
}