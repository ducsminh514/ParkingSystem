// Thêm vào ParkingHub.cs (partial class)

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;
using ParkingSystem.Shared.DTOs;
using System.Text.RegularExpressions;

namespace ParkingSystem.Server.Hubs
{
    public partial class ParkingHub
    {
        // ============ ADMIN SLOT MANAGEMENT ============

        /// <summary>
        /// Lấy danh sách slots grouped by area (prefix) - Admin only
        /// </summary>
        public async Task<List<ParkingAreaGroupDto>> GetSlotsGroupedByArea()
        {
            try
            {
                _logger.LogInformation("Admin getting slots grouped by area");

                var allSlots = await _context.ParkingSlots
                    .Include(s => s.ParkingRegistrations
                        .Where(r => r.Status == "Active" || r.Status == "CheckedIn"))
                    .ThenInclude(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                    .OrderBy(s => s.SlotCode)
                    .ToListAsync();

                // Group by prefix (extract từ SlotCode)
                var grouped = allSlots
                    .GroupBy(s => ExtractPrefix(s.SlotCode))
                    .Select(g => new ParkingAreaGroupDto
                    {
                        AreaPrefix = g.Key,
                        AreaName = $"Khu {g.Key}",
                        TotalSlots = g.Count(),
                        AvailableSlots = g.Count(s => s.Status == "Available"),
                        InUseSlots = g.Count(s => s.Status == "InUse"),
                        MaintenanceSlots = g.Count(s => s.Status == "Maintenance"),
                        Slots = g.Select(s =>
                        {
                            var currentReg = s.ParkingRegistrations
                                .FirstOrDefault(r => r.Status == "Active" || r.Status == "CheckedIn");

                            return new SlotManagementDto
                            {
                                SlotId = s.SlotId,
                                SlotCode = s.SlotCode,
                                Status = s.Status,
                                HasActiveRegistration = currentReg != null,
                                CurrentRegistrationId = currentReg?.RegistrationId,
                                VehiclePlateNumber = currentReg?.Vehicle?.PlateNumber,
                                CustomerName = currentReg?.Vehicle?.Customer?.FullName,
                                CheckInTime = currentReg?.CheckInTime
                            };
                        }).ToList()
                    })
                    .OrderBy(a => a.AreaPrefix)
                    .ToList();

                return grouped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slots grouped by area");
                throw new HubException($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract prefix từ SlotCode (VD: "A01" → "A", "VIP10" → "VIP")
        /// </summary>
        private string ExtractPrefix(string slotCode)
        {
            if (string.IsNullOrWhiteSpace(slotCode))
                return "Unknown";

            // Extract chữ cái đầu tiên hoặc nhóm chữ cái
            var match = Regex.Match(slotCode, @"^([A-Za-z]+)");
            return match.Success ? match.Groups[1].Value.ToUpper() : "Unknown";
        }

        /// <summary>
        /// Lấy thống kê slots - Admin only
        /// </summary>
        public async Task<SlotsStatisticsDto> GetSlotsStatistics()
        {
            try
            {
                var totalSlots = await _context.ParkingSlots.CountAsync();
                var availableSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Available");
                var inUseSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "InUse");
                var maintenanceSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Maintenance");
                var reservedSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Reserved");

                var allSlots = await _context.ParkingSlots.Select(s => s.SlotCode).ToListAsync();
                var totalAreas = allSlots.Select(code => ExtractPrefix(code)).Distinct().Count();

                var occupancyRate = totalSlots > 0
                    ? Math.Round((double)(inUseSlots + reservedSlots) / totalSlots * 100, 1)
                    : 0;

                return new SlotsStatisticsDto
                {
                    TotalSlots = totalSlots,
                    AvailableSlots = availableSlots,
                    InUseSlots = inUseSlots,
                    MaintenanceSlots = maintenanceSlots,
                    ReservedSlots = reservedSlots,
                    TotalAreas = totalAreas,
                    OccupancyRate = occupancyRate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slots statistics");
                throw new HubException($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo slot mới - Admin only
        /// </summary>
        public async Task<SlotOperationResponse> CreateSlot(CreateSlotRequest request)
        {
            try
            {
                _logger.LogInformation($"Admin creating slot: {request.SlotCode}");

                // Validation
                if (string.IsNullOrWhiteSpace(request.SlotCode))
                {
                    return new SlotOperationResponse
                    {
                        Success = false,
                        Message = "Vui lòng nhập mã slot"
                    };
                }

                // Check trùng SlotCode
                var exists = await _context.ParkingSlots.AnyAsync(s => s.SlotCode == request.SlotCode);
                if (exists)
                {
                    return new SlotOperationResponse
                    {
                        Success = false,
                        Message = $"Mã slot '{request.SlotCode}' đã tồn tại"
                    };
                }

                // Tạo slot mới
                var slot = new ParkingSlot
                {
                    SlotId = Guid.NewGuid(),
                    SlotCode = request.SlotCode.Trim().ToUpper(),
                    Status = request.Status
                };

                _context.ParkingSlots.Add(slot);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created slot: {slot.SlotCode}");

                var slotDto = new SlotManagementDto
                {
                    SlotId = slot.SlotId,
                    SlotCode = slot.SlotCode,
                    Status = slot.Status,
                    HasActiveRegistration = false
                };

                // Broadcast to all clients
                await Clients.All.SendAsync("OnSlotCreated", slotDto);

                return new SlotOperationResponse
                {
                    Success = true,
                    Message = "Thêm slot thành công",
                    Slot = slotDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating slot");
                return new SlotOperationResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Tạo nhiều slots cùng lúc (bulk) - Admin only
        /// </summary>
        public async Task<SlotOperationResponse> BulkCreateSlots(BulkCreateSlotsRequest request)
        {
            try
            {
                _logger.LogInformation($"Admin bulk creating slots: {request.Prefix}{request.StartNumber}-{request.EndNumber}");

                // Validation
                if (string.IsNullOrWhiteSpace(request.Prefix))
                {
                    return new SlotOperationResponse
                    {
                        Success = false,
                        Message = "Vui lòng nhập prefix (VD: A, B, VIP)"
                    };
                }

                if (request.StartNumber > request.EndNumber)
                {
                    return new SlotOperationResponse
                    {
                        Success = false,
                        Message = "Số bắt đầu phải nhỏ hơn số kết thúc"
                    };
                }

                if (request.EndNumber - request.StartNumber > 100)
                {
                    return new SlotOperationResponse
                    {
                        Success = false,
                        Message = "Chỉ được tạo tối đa 100 slots một lúc"
                    };
                }

                var prefix = request.Prefix.Trim().ToUpper();
                var createdSlots = new List<SlotManagementDto>();
                var errors = new List<string>();

                for (int i = request.StartNumber; i <= request.EndNumber; i++)
                {
                    var slotCode = $"{prefix}{i:D2}"; // VD: A01, A02...

                    // Check trùng
                    var exists = await _context.ParkingSlots.AnyAsync(s => s.SlotCode == slotCode);
                    if (exists)
                    {
                        errors.Add($"{slotCode} đã tồn tại");
                        continue;
                    }

                    var slot = new ParkingSlot
                    {
                        SlotId = Guid.NewGuid(),
                        SlotCode = slotCode,
                        Status = request.Status
                    };

                    _context.ParkingSlots.Add(slot);

                    createdSlots.Add(new SlotManagementDto
                    {
                        SlotId = slot.SlotId,
                        SlotCode = slot.SlotCode,
                        Status = slot.Status,
                        HasActiveRegistration = false
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bulk created {createdSlots.Count} slots");

                // Broadcast to all clients
                await Clients.All.SendAsync("OnSlotsBulkCreated", createdSlots);

                var message = $"Đã tạo {createdSlots.Count} slots thành công";
                if (errors.Count > 0)
                {
                    message += $". {errors.Count} slots bị trùng: {string.Join(", ", errors.Take(5))}";
                }

                return new SlotOperationResponse
                {
                    Success = true,
                    Message = message,
                    Slots = createdSlots
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating slots");
                return new SlotOperationResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Cập nhật slot - Admin only
        /// </summary>
        public async Task<SlotOperationResponse> UpdateSlot(UpdateSlotRequest request)
        {
            try
            {
                _logger.LogInformation($"Admin updating slot: {request.SlotId}");

                var slot = await _context.ParkingSlots.FindAsync(request.SlotId);
                if (slot == null)
                {
                    return new SlotOperationResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy slot"
                    };
                }

                // Validation
                if (string.IsNullOrWhiteSpace(request.SlotCode))
                {
                    return new SlotOperationResponse
                    {
                        Success = false,
                        Message = "Vui lòng nhập mã slot"
                    };
                }

                // Check trùng SlotCode (nếu thay đổi)
                if (slot.SlotCode != request.SlotCode)
                {
                    var exists = await _context.ParkingSlots
                        .AnyAsync(s => s.SlotCode == request.SlotCode && s.SlotId != slot.SlotId);

                    if (exists)
                    {
                        return new SlotOperationResponse
                        {
                            Success = false,
                            Message = $"Mã slot '{request.SlotCode}' đã tồn tại"
                        };
                    }
                }

                // Update
                slot.SlotCode = request.SlotCode.Trim().ToUpper();
                slot.Status = request.Status;

                await _context.SaveChangesAsync();

                var slotDto = new SlotManagementDto
                {
                    SlotId = slot.SlotId,
                    SlotCode = slot.SlotCode,
                    Status = slot.Status,
                    HasActiveRegistration = await _context.ParkingRegistrations
                        .AnyAsync(r => r.SlotId == slot.SlotId && (r.Status == "Active" || r.Status == "CheckedIn"))
                };

                _logger.LogInformation($"Updated slot: {slot.SlotCode}");

                // Broadcast to all clients
                await Clients.All.SendAsync("OnSlotUpdated", slotDto);

                return new SlotOperationResponse
                {
                    Success = true,
                    Message = "Cập nhật slot thành công",
                    Slot = slotDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating slot");
                return new SlotOperationResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Xóa slot - Admin only
        /// ⚠️ KHÔNG được xóa nếu có registration active
        /// </summary>
        public async Task<DeleteSlotResponse> DeleteSlot(Guid slotId, bool forceDelete = false)
        {
            try
            {
                _logger.LogInformation($"Admin deleting slot: {slotId}, force: {forceDelete}");

                var slot = await _context.ParkingSlots
                    .Include(s => s.ParkingRegistrations)
                    .FirstOrDefaultAsync(s => s.SlotId == slotId);

                if (slot == null)
                {
                    return new DeleteSlotResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy slot"
                    };
                }

                // ⚠️ VALIDATION: Check có registration active không
                var hasActiveRegistration = slot.ParkingRegistrations
                    .Any(r => r.Status == "Active" || r.Status == "CheckedIn");

                if (hasActiveRegistration && !forceDelete)
                {
                    var activeReg = slot.ParkingRegistrations
                        .First(r => r.Status == "Active" || r.Status == "CheckedIn");

                    return new DeleteSlotResponse
                    {
                        Success = false,
                        Message = $"Không thể xóa slot {slot.SlotCode}. Slot đang có xe đăng ký (Registration ID: {activeReg.RegistrationId.ToString().Substring(0, 8)}...)",
                        Errors = new List<string> { "Vui lòng check-out xe trước khi xóa slot" }
                    };
                }

                // Xóa slot
                _context.ParkingSlots.Remove(slot);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Deleted slot: {slot.SlotCode}");

                // Broadcast to all clients
                await Clients.All.SendAsync("OnSlotDeleted", slotId);

                return new DeleteSlotResponse
                {
                    Success = true,
                    Message = $"Đã xóa slot {slot.SlotCode}",
                    DeletedCount = 1,
                    FailedCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting slot");
                return new DeleteSlotResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Xóa nhiều slots cùng lúc - Admin only
        /// ⚠️ KHÔNG được xóa các slot có registration active
        /// </summary>
        public async Task<DeleteSlotResponse> BulkDeleteSlots(BulkDeleteSlotsRequest request)
        {
            try
            {
                _logger.LogInformation($"Admin bulk deleting {request.SlotIds.Count} slots");

                var slots = await _context.ParkingSlots
                    .Include(s => s.ParkingRegistrations)
                    .Where(s => request.SlotIds.Contains(s.SlotId))
                    .ToListAsync();

                if (slots.Count == 0)
                {
                    return new DeleteSlotResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy slots để xóa"
                    };
                }

                var deletedCount = 0;
                var failedCount = 0;
                var errors = new List<string>();

                foreach (var slot in slots)
                {
                    // Check có registration active không
                    var hasActiveRegistration = slot.ParkingRegistrations
                        .Any(r => r.Status == "Active" || r.Status == "CheckedIn");

                    if (hasActiveRegistration && !request.ForceDelete)
                    {
                        errors.Add($"{slot.SlotCode}: Đang có xe đăng ký");
                        failedCount++;
                        continue;
                    }

                    _context.ParkingSlots.Remove(slot);
                    deletedCount++;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bulk deleted {deletedCount} slots, {failedCount} failed");

                // Broadcast to all clients
                await Clients.All.SendAsync("OnSlotsBulkDeleted", request.SlotIds);

                var message = $"Đã xóa {deletedCount} slots";
                if (failedCount > 0)
                {
                    message += $". {failedCount} slots không thể xóa (đang có xe đăng ký)";
                }

                return new DeleteSlotResponse
                {
                    Success = deletedCount > 0,
                    Message = message,
                    DeletedCount = deletedCount,
                    FailedCount = failedCount,
                    Errors = errors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting slots");
                return new DeleteSlotResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                };
            }
        }
    }
}