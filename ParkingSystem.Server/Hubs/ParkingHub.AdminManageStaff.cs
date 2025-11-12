// Thêm vào ParkingHub.cs

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Shared.DTOs;
using ParkingSystem.Server.Models;
using BCrypt.Net;

namespace ParkingSystem.Server.Hubs
{
    public partial class ParkingHub
    {
        /// <summary>
        /// Lấy danh sách tất cả staff
        /// </summary>
        public async Task<StaffListResponse> GetAllStaffs()
        {
            try
            {
                var staffs = await _context.Staff
                    .Select(s => new StaffDto
                    {
                        StaffId = s.StaffId ,
                        FullName = s.FullName,
                        Username = s.Username,
                        Shift = s.Shift,
                        // Đếm số lượng registration
                        TotalRegistrations = _context.ParkingRegistrations
                            .Count(pr => pr.StaffId == s.StaffId),
                        ActiveRegistrations = _context.ParkingRegistrations
                            .Count(pr => pr.StaffId == s.StaffId && pr.Status == "Active")
                    })
                    .OrderBy(s => s.FullName)
                    .ToListAsync();

                return new StaffListResponse
                {
                    Success = true,
                    Message = "Lấy danh sách staff thành công",
                    Staffs = staffs,
                    TotalCount = staffs.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all staffs");
                return new StaffListResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}",
                    Staffs = new List<StaffDto>(),
                    TotalCount = 0
                };
            }
        }

        /// <summary>
        /// Tạo staff mới
        /// </summary>
        public async Task<StaffOperationResponse> CreateStaff(CreateStaffRequest request)
        {
            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Vui lòng nhập họ tên"
                    };
                }

                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Vui lòng nhập tên đăng nhập"
                    };
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Vui lòng nhập mật khẩu"
                    };
                }

                // Check username đã tồn tại
                var existingStaff = await _context.Staff
                    .FirstOrDefaultAsync(s => s.Username == request.Username);

                if (existingStaff != null)
                {
                    return new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Tên đăng nhập đã tồn tại"
                    };
                }

                // Hash password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Tạo staff mới
                var newStaff = new Staff
                {
                    StaffId = Guid.NewGuid(),
                    FullName = request.FullName.Trim(),
                    Username = request.Username.Trim(),
                    PasswordHash = passwordHash,
                    Shift = request.Shift?.Trim()
                };

                _context.Staff.Add(newStaff);
                await _context.SaveChangesAsync();

                var staffDto = new StaffDto
                {
                    StaffId = newStaff.StaffId,
                    FullName = newStaff.FullName,
                    Username = newStaff.Username,
                    Shift = newStaff.Shift,
                    TotalRegistrations = 0,
                    ActiveRegistrations = 0
                };

                // Broadcast to all clients
                await Clients.All.SendAsync("OnStaffCreated", staffDto);

                return new StaffOperationResponse
                {
                    Success = true,
                    Message = "Tạo nhân viên thành công",
                    Staff = staffDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff");
                return new StaffOperationResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Cập nhật thông tin staff
        /// </summary>
        public async Task<StaffOperationResponse> UpdateStaff(UpdateStaffRequest request)
        {
            try
            {
                var staff = await _context.Staff
                    .FirstOrDefaultAsync(s => s.StaffId == request.StaffId);

                if (staff == null)
                {
                    return new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy nhân viên"
                    };
                }

                // Cập nhật thông tin
                staff.FullName = request.FullName.Trim();
                staff.Shift = request.Shift?.Trim();

                // Cập nhật password nếu có
                if (!string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    staff.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                }

                await _context.SaveChangesAsync();

                var staffDto = new StaffDto
                {
                    StaffId = staff.StaffId,
                    FullName = staff.FullName,
                    Username = staff.Username,
                    Shift = staff.Shift,
                    TotalRegistrations = await _context.ParkingRegistrations
                        .CountAsync(pr => pr.StaffId == staff.StaffId),
                    ActiveRegistrations = await _context.ParkingRegistrations
                        .CountAsync(pr => pr.StaffId == staff.StaffId && pr.Status == "Active")
                };

                // Broadcast to all clients
                await Clients.All.SendAsync("OnStaffUpdated", staffDto);

                return new StaffOperationResponse
                {
                    Success = true,
                    Message = "Cập nhật thành công",
                    Staff = staffDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff");
                return new StaffOperationResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Xóa staff
        /// </summary>
        public async Task<StaffOperationResponse> DeleteStaff(DeleteStaffRequest request)
        {
            try
            {
                var staff = await _context.Staff
                    .FirstOrDefaultAsync(s => s.StaffId == request.StaffId);

                if (staff == null)
                {
                    return new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy nhân viên"
                    };
                }

                // Check xem staff có registration nào không
                var hasRegistrations = await _context.ParkingRegistrations
                    .AnyAsync(pr => pr.StaffId == request.StaffId);

                if (hasRegistrations)
                {
                    return new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Không thể xóa nhân viên đã có lịch sử đăng ký. Vui lòng vô hiệu hóa thay vì xóa."
                    };
                }

                _context.Staff.Remove(staff);
                await _context.SaveChangesAsync();

                // Broadcast to all clients
                await Clients.All.SendAsync("OnStaffDeleted", request.StaffId);

                return new StaffOperationResponse
                {
                    Success = true,
                    Message = "Xóa nhân viên thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staff");
                return new StaffOperationResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }
    }
}