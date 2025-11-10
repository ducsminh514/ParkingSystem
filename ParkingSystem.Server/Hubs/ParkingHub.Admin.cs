using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;
using ParkingSystem.Shared.DTOs;

namespace ParkingSystem.Server.Hubs
{
    public partial class ParkingHub
    {
        // ============================
        // Get All Reports (Admin only)
        // ============================
        public async Task<List<CustomerReport>> GetAllReports()
        {
            try
            {
                return await _context.CustomerReports
                    .Include(r => r.Customer)
                    .Include(r => r.Category)
                    .Include(r => r.AssignedStaff)
                    .Include(r => r.ReportComments)
                    .OrderByDescending(r => r.CreatedDate)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new HubException($"Error getting all reports: {ex.Message}");
            }
        }

        // ============================
        // Get All Staff (For assignment dropdown)
        // ============================
        public async Task<List<Staff>> GetAllStaff()
        {
            try
            {
                return await _context.Staff
                    .OrderBy(s => s.FullName)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new HubException($"Error getting staff list: {ex.Message}");
            }
        }

        // ============================
        // Assign Report to Staff (Admin only)
        // ============================
        public async Task<bool> AssignReportToStaff(Guid reportId, Guid staffId)
        {
            try
            {
                var report = await _context.CustomerReports.FindAsync(reportId);
                if (report == null)
                    return false;

                var staff = await _context.Staff.FindAsync(staffId);
                if (staff == null)
                    throw new HubException("Staff not found");

                // Update report
                report.AssignedStaffId = staffId;
                report.Status = "InProgress"; // Auto change status
                report.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Get full report info
                var updatedReport = await _context.CustomerReports
                    .Include(r => r.Customer)
                    .Include(r => r.Category)
                    .Include(r => r.AssignedStaff)
                    .AsNoTracking()
                    .FirstAsync(r => r.ReportId == reportId);

                // ⭐ Broadcast to ALL
                await Clients.All.SendAsync("OnReportAssigned", updatedReport);

                // ⭐ Notify specific staff
                await Clients.User(staffId.ToString())
                    .SendAsync("OnReportAssignedToYou", updatedReport);

                return true;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error assigning report: {ex.Message}");
            }
        }

        // ============================
        // Change Report Priority (Admin only)
        // ============================
        public async Task<bool> ChangePriority(Guid reportId, string newPriority)
        {
            try
            {
                var report = await _context.CustomerReports.FindAsync(reportId);
                if (report == null)
                    return false;

                report.Priority = newPriority;
                report.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Broadcast
                await Clients.All.SendAsync("OnReportPriorityChanged", reportId, newPriority);

                return true;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error changing priority: {ex.Message}");
            }
        }

        // ============================
        // Reassign Report (Admin only)
        // ============================
        public async Task<bool> ReassignReport(Guid reportId, Guid newStaffId)
        {
            try
            {
                var report = await _context.CustomerReports.FindAsync(reportId);
                if (report == null)
                    return false;

                var oldStaffId = report.AssignedStaffId;
                report.AssignedStaffId = newStaffId;
                report.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Get full report info
                var updatedReport = await _context.CustomerReports
                    .Include(r => r.Customer)
                    .Include(r => r.Category)
                    .Include(r => r.AssignedStaff)
                    .AsNoTracking()
                    .FirstAsync(r => r.ReportId == reportId);

                // Notify old staff
                if (oldStaffId.HasValue)
                {
                    await Clients.User(oldStaffId.ToString())
                        .SendAsync("OnReportUnassigned", reportId);
                }

                // Notify new staff
                await Clients.User(newStaffId.ToString())
                    .SendAsync("OnReportAssignedToYou", updatedReport);

                // Broadcast to ALL
                await Clients.All.SendAsync("OnReportReassigned", updatedReport);

                return true;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error reassigning report: {ex.Message}");
            }
        }

        // ============================
        // Get Admin Statistics
        // ============================
        public async Task<object> GetAdminStatistics()
        {
            try
            {
                var reports = await _context.CustomerReports
                    .AsNoTracking()
                    .ToListAsync();

                var resolvedReports = reports
                    .Where(r => r.Status == "Resolved" && r.ResolvedDate.HasValue)
                    .ToList();

                var staffPerformance = await _context.Staff
                    .Select(s => new
                    {
                        StaffId = s.StaffId,
                        StaffName = s.FullName,
                        TotalAssigned = _context.CustomerReports.Count(r => r.AssignedStaffId == s.StaffId),
                        Resolved = _context.CustomerReports.Count(r => r.AssignedStaffId == s.StaffId && r.Status == "Resolved"),
                        AverageRating = _context.CustomerReports
                            .Where(r => r.AssignedStaffId == s.StaffId && r.Rating.HasValue)
                            .Average(r => (double?)r.Rating) ?? 0
                    })
                    .AsNoTracking()
                    .ToListAsync();

                return new
                {
                    TotalReports = reports.Count,
                    UnassignedReports = reports.Count(r => r.AssignedStaffId == null),
                    PendingReports = reports.Count(r => r.Status == "Pending"),
                    InProgressReports = reports.Count(r => r.Status == "InProgress"),
                    ResolvedReports = reports.Count(r => r.Status == "Resolved"),
                    RejectedReports = reports.Count(r => r.Status == "Rejected"),
                    UrgentReports = reports.Count(r => r.Priority == "Urgent"),
                    HighReports = reports.Count(r => r.Priority == "High"),
                    AverageResolutionTime = resolvedReports.Any()
                        ? resolvedReports.Average(r => (r.ResolvedDate.Value - r.CreatedDate).TotalHours)
                        : 0,
                    AverageRating = reports.Where(r => r.Rating.HasValue).Any()
                        ? reports.Where(r => r.Rating.HasValue).Average(r => r.Rating.Value)
                        : 0,
                    StaffPerformance = staffPerformance
                };
            }
            catch (Exception ex)
            {
                throw new HubException($"Error getting admin statistics: {ex.Message}");
            }
        }

        // ============ ADMIN PRICING MANAGEMENT ============

        /// <summary>
        /// Lấy tất cả giá (bao gồm inactive) - Admin only
        /// </summary>
        public async Task<List<ParkingPriceDetailDto>> GetAllPricesAdmin()
        {
            try
            {
                _logger.LogInformation("Admin getting all prices");

                var prices = await _context.ParkingPrices
                    .OrderByDescending(p => p.IsActive)
                    .ThenBy(p => p.VehicleType)
                    .Select(p => new ParkingPriceDetailDto
                    {
                        PriceId = p.PriceId,
                        VehicleType = p.VehicleType,
                        PricePerHour = p.PricePerHour,
                        IsActive = p.IsActive,
                        CreatedDate = p.CreatedDate,
                        UpdatedDate = p.UpdatedDate
                    })
                    .ToListAsync();

                return prices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all prices for admin");
                throw new HubException($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo hoặc cập nhật giá - Admin only
        /// </summary>
        public async Task<UpsertPriceResponse> UpsertPrice(UpsertPriceRequest request)
        {
            try
            {
                _logger.LogInformation($"Admin upserting price: {request.VehicleType}");

                // Validation
                if (string.IsNullOrWhiteSpace(request.VehicleType))
                {
                    return new UpsertPriceResponse
                    {
                        Success = false,
                        Message = "Vui lòng nhập loại xe"
                    };
                }

                if (request.PricePerHour <= 0)
                {
                    return new UpsertPriceResponse
                    {
                        Success = false,
                        Message = "Giá phải lớn hơn 0"
                    };
                }

                ParkingPrice price;

                if (request.PriceId.HasValue)
                {
                    // UPDATE
                    price = await _context.ParkingPrices.FindAsync(request.PriceId.Value);

                    if (price == null)
                    {
                        return new UpsertPriceResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy giá cần cập nhật"
                        };
                    }

                    // Kiểm tra trùng VehicleType (nếu thay đổi)
                    if (price.VehicleType != request.VehicleType)
                    {
                        var exists = await _context.ParkingPrices
                            .AnyAsync(p => p.VehicleType == request.VehicleType && p.PriceId != price.PriceId);

                        if (exists)
                        {
                            return new UpsertPriceResponse
                            {
                                Success = false,
                                Message = $"Loại xe '{request.VehicleType}' đã tồn tại"
                            };
                        }
                    }

                    price.VehicleType = request.VehicleType.Trim();
                    price.PricePerHour = request.PricePerHour;
                    price.IsActive = request.IsActive;
                    price.UpdatedDate = DateTime.Now;

                    _logger.LogInformation($"Updated price: {price.VehicleType} - {price.PricePerHour}");
                }
                else
                {
                    // CREATE
                    // Kiểm tra trùng VehicleType
                    var exists = await _context.ParkingPrices
                        .AnyAsync(p => p.VehicleType == request.VehicleType);

                    if (exists)
                    {
                        return new UpsertPriceResponse
                        {
                            Success = false,
                            Message = $"Loại xe '{request.VehicleType}' đã tồn tại"
                        };
                    }

                    price = new ParkingPrice
                    {
                        PriceId = Guid.NewGuid(),
                        VehicleType = request.VehicleType.Trim(),
                        PricePerHour = request.PricePerHour,
                        IsActive = request.IsActive,
                        CreatedDate = DateTime.Now
                    };

                    _context.ParkingPrices.Add(price);
                    _logger.LogInformation($"Created new price: {price.VehicleType} - {price.PricePerHour}");
                }

                await _context.SaveChangesAsync();

                var priceDto = new ParkingPriceDto
                {
                    PriceId = price.PriceId,
                    VehicleType = price.VehicleType,
                    PricePerHour = price.PricePerHour,
                    IsActive = price.IsActive
                };

                // Broadcast to all clients
                await Clients.All.SendAsync("OnPriceUpdated", priceDto);

                return new UpsertPriceResponse
                {
                    Success = true,
                    Message = request.PriceId.HasValue ? "Cập nhật giá thành công" : "Thêm giá mới thành công",
                    Price = priceDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting price");
                return new UpsertPriceResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Xóa giá (soft delete) - Admin only
        /// </summary>
        public async Task<DeletePriceResponse> DeletePrice(Guid priceId)
        {
            try
            {
                _logger.LogInformation($"Admin deleting price: {priceId}");

                var price = await _context.ParkingPrices.FindAsync(priceId);

                if (price == null)
                {
                    return new DeletePriceResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy giá cần xóa"
                    };
                }

                // Soft delete: Set IsActive = false
                price.IsActive = false;
                price.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Deleted price: {price.VehicleType}");

                // Broadcast to all clients
                await Clients.All.SendAsync("OnPriceDeleted", priceId);

                return new DeletePriceResponse
                {
                    Success = true,
                    Message = "Xóa giá thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting price");
                return new DeletePriceResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Toggle trạng thái active/inactive - Admin only
        /// </summary>
        public async Task<UpsertPriceResponse> TogglePriceStatus(Guid priceId)
        {
            try
            {
                _logger.LogInformation($"Admin toggling price status: {priceId}");

                var price = await _context.ParkingPrices.FindAsync(priceId);

                if (price == null)
                {
                    return new UpsertPriceResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy giá"
                    };
                }

                price.IsActive = !price.IsActive;
                price.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                var priceDto = new ParkingPriceDto
                {
                    PriceId = price.PriceId,
                    VehicleType = price.VehicleType,
                    PricePerHour = price.PricePerHour,
                    IsActive = price.IsActive
                };

                // Broadcast to all clients
                await Clients.All.SendAsync("OnPriceUpdated", priceDto);

                return new UpsertPriceResponse
                {
                    Success = true,
                    Message = price.IsActive ? "Đã kích hoạt giá" : "Đã tắt giá",
                    Price = priceDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling price status");
                return new UpsertPriceResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                };
            }
        }
    }
}