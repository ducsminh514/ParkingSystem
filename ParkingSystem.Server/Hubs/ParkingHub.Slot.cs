using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;
using ParkingSystem.Shared.DTOs;

namespace ParkingSystem.Server.Hubs
{
    public partial class ParkingHub
    {
        // ============ PARKING SLOT OPERATIONS ============

        public async Task<List<ParkingSlotDto>> GetAllSlots()
        {
            try
            {
                var slots = await _context.ParkingSlots
                    .OrderBy(s => s.SlotCode)
                    .Select(s => new ParkingSlotDto
                    {
                        SlotId = s.SlotId,
                        SlotCode = s.SlotCode,
                        Status = s.Status
                    })
                    .ToListAsync();

                return slots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all slots");
                throw new HubException($"Error getting slots: {ex.Message}");
            }
        }
        
        public async Task<List<ParkingAreaDto>> GetSlotsByArea()
        {
            try
            {
                _logger.LogInformation("Bắt đầu lấy slots theo khu vực...");
                
                // Kiểm tra kết nối database
                var totalCount = await _context.ParkingSlots.CountAsync();
                _logger.LogInformation($"Tổng số slots trong database: {totalCount}");
                
                if (totalCount == 0)
                {
                    _logger.LogWarning("Không có slots nào trong database!");
                    return new List<ParkingAreaDto>();
                }

                var allSlots = await _context.ParkingSlots
                    .Include(s => s.ParkingRegistrations
                        .Where(r => r.Status == "Active" || r.Status == "CheckedIn"))
                    .ThenInclude(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                    .OrderBy(s => s.SlotCode)
                    .ToListAsync();

                _logger.LogInformation($"Đã lấy được {allSlots.Count} slots từ database");

                // Nhóm slots theo ký tự đầu tiên (Zone/Area)
                var groupedSlots = allSlots
                    .GroupBy(s => s.SlotCode.Length > 0 ? s.SlotCode[0].ToString().ToUpper() : "Unknown")
                    .Select(g => new ParkingAreaDto
                    {
                        AreaName = $"Khu {g.Key}",
                        TotalSlots = g.Count(),
                        AvailableSlots = g.Count(s => s.Status == "Available"),
                        OccupiedSlots = g.Count(s => s.Status == "InUse" || s.Status == "Occupied"),
                        Slots = g.Select(s =>
                        {
                            var currentReg = s.ParkingRegistrations
                                .FirstOrDefault(r => r.Status == "Active" || r.Status == "CheckedIn");

                            return new ParkingSlotDto
                            {
                                SlotId = s.SlotId,
                                SlotCode = s.SlotCode,
                                Status = s.Status,
                                CurrentRegistrationId = currentReg?.RegistrationId,
                                VehiclePlateNumber = currentReg?.Vehicle?.PlateNumber,
                                VehicleType = currentReg?.Vehicle?.VehicleType,
                                CustomerId =currentReg?.Vehicle?.CustomerId,
                                CustomerName = currentReg?.Vehicle?.Customer?.FullName,
                                CustomerPhone = currentReg?.Vehicle?.Customer?.Phone,
                                CheckInTime = currentReg?.CheckInTime
                            };
                        }).ToList()
                    })
                    .OrderBy(a => a.AreaName)
                    .ToList();

                _logger.LogInformation($"Đã nhóm thành {groupedSlots.Count} khu vực");
                return groupedSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slots theo khu vực: {Message}", ex.Message);
                throw new HubException($"Error getting slots by area: {ex.Message}");
            }
        }

        public async Task<ParkingOverviewDto> GetParkingOverview()
        {
            try
            {
                _logger.LogInformation("Bắt đầu lấy tổng quan parking...");
                
                var totalSlots = await _context.ParkingSlots.CountAsync();
                _logger.LogInformation($"Tổng số slots: {totalSlots}");
                
                var availableSlots = await _context.ParkingSlots
                    .CountAsync(s => s.Status == "Available");
                _logger.LogInformation($"Số slots trống: {availableSlots}");
                
                var occupiedSlots = await _context.ParkingSlots
                    .CountAsync(s => s.Status == "InUse" || s.Status == "Occupied");
                _logger.LogInformation($"Số slots đã sử dụng: {occupiedSlots}");

                var overview = new ParkingOverviewDto
                {
                    TotalSlots = totalSlots,
                    AvailableSlots = availableSlots,
                    OccupiedSlots = occupiedSlots,
                    OccupancyRate = totalSlots > 0
                        ? Math.Round((double)occupiedSlots / totalSlots * 100, 1)
                        : 0
                };

                _logger.LogInformation($"Hoàn thành lấy tổng quan: Total={overview.TotalSlots}, Available={overview.AvailableSlots}, Occupied={overview.OccupiedSlots}");
                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tổng quan parking: {Message}", ex.Message);
                throw new HubException($"Error getting parking overview: {ex.Message}");
            }
        }

        public async Task<ParkingSlotDto> GetSlotById(Guid id)
        {
            try
            {
                var slot = await _context.ParkingSlots
                    .Include(s => s.ParkingRegistrations
                        .Where(r => r.Status == "Active" || r.Status == "CheckedIn"))
                    .ThenInclude(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                    .Where(s => s.SlotId == id)
                    .FirstOrDefaultAsync();

                if (slot == null)
                {
                    throw new HubException($"Không tìm thấy slot với ID: {id}");
                }

                var currentReg = slot.ParkingRegistrations
                    .FirstOrDefault(r => r.Status == "Active" || r.Status == "CheckedIn");

                var slotDto = new ParkingSlotDto
                {
                    SlotId = slot.SlotId,
                    SlotCode = slot.SlotCode,
                    Status = slot.Status,
                    CurrentRegistrationId = currentReg?.RegistrationId,
                    VehiclePlateNumber = currentReg?.Vehicle?.PlateNumber,
                    VehicleType = currentReg?.Vehicle?.VehicleType,
                    CustomerId = currentReg?.Vehicle?.CustomerId,
                    CustomerName = currentReg?.Vehicle?.Customer?.FullName,
                    CustomerPhone = currentReg?.Vehicle?.Customer?.Phone,
                    CheckInTime = currentReg?.CheckInTime
                };

                return slotDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy slot {id}");
                throw new HubException($"Error getting slot: {ex.Message}");
            }
        }

        public async Task<List<ParkingSlotDto>> GetAvailableSlots()
        {
            try
            {
                var availableSlots = await _context.ParkingSlots
                    .Where(s => s.Status == "Available")
                    .OrderBy(s => s.SlotCode)
                    .Select(s => new ParkingSlotDto
                    {
                        SlotId = s.SlotId,
                        SlotCode = s.SlotCode,
                        Status = s.Status
                    })
                    .ToListAsync();

                return availableSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slots trống");
                throw new HubException($"Error getting available slots: {ex.Message}");
            }
        }

        public async Task<ParkingSlotDto> UpdateSlotStatus(Guid id, string status)
        {
            try
            {
                var slot = await _context.ParkingSlots.FindAsync(id);

                if (slot == null)
                {
                    throw new HubException($"Không tìm thấy slot với ID: {id}");
                }

                slot.Status = status;
                await _context.SaveChangesAsync();

                var slotDto = new ParkingSlotDto
                {
                    SlotId = slot.SlotId,
                    SlotCode = slot.SlotCode,
                    Status = slot.Status
                };

                // Broadcast to all clients
                await Clients.All.SendAsync("OnSlotUpdated", slotDto);

                return slotDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật trạng thái slot {id}");
                throw new HubException($"Error updating slot status: {ex.Message}");
            }
        }

        public async Task<RegisterParkingResponse> RegisterParking(RegisterParkingRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Kiểm tra slot có tồn tại và available không
                var slot = await _context.ParkingSlots.FindAsync(request.SlotId);
                if (slot == null)
                {
                    return new RegisterParkingResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy chỗ đỗ"
                    };
                }

                if (slot.Status != "Available")
                {
                    return new RegisterParkingResponse
                    {
                        Success = false,
                        Message = "Chỗ đỗ này đã được sử dụng"
                    };
                }

                // 2. Tìm hoặc tạo khách hàng
                Customer? customer;
                if (request.CustomerId.HasValue)
                {
                    customer = await _context.Customers.FindAsync(request.CustomerId.Value);
                    if (customer == null)
                    {
                        return new RegisterParkingResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy khách hàng"
                        };
                    }
                }
                else
                {
                    // Tạo khách hàng mới
                    customer = new Customer
                    {
                        CustomerId = Guid.NewGuid(),
                        FullName = request.CustomerName,
                        Phone = request.CustomerPhone,
                        Email = request.CustomerEmail,
                        PasswordHash = "default_hash" // TODO: Generate proper hash
                    };
                    _context.Customers.Add(customer);
                }

                // 3. Tìm hoặc tạo xe
                var vehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.PlateNumber == request.PlateNumber);

                if (vehicle == null)
                {
                    vehicle = new Vehicle
                    {
                        VehicleId = Guid.NewGuid(),
                        PlateNumber = request.PlateNumber,
                        VehicleType = request.VehicleType,
                        CustomerId = customer.CustomerId
                    };
                    _context.Vehicles.Add(vehicle);
                }
                else
                {
                    // Update vehicle info if needed
                    vehicle.VehicleType = request.VehicleType;
                    vehicle.CustomerId = customer.CustomerId;
                }

                // 4. Tạo parking registration
                var registration = new ParkingRegistration
                {
                    RegistrationId = Guid.NewGuid(),
                    VehicleId = vehicle.VehicleId,
                    SlotId = request.SlotId,
                    StaffId = request.StaffId,
                    CheckInTime = DateTime.Now,
                    Status = "Active"
                };
                _context.ParkingRegistrations.Add(registration);

                // 5. Cập nhật trạng thái slot
                slot.Status = "InUse";

                // 6. Lưu tất cả thay đổi
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 7. Load lại thông tin slot để trả về
                var updatedSlot = await _context.ParkingSlots
                    .Include(s => s.ParkingRegistrations.Where(r => r.Status == "Active"))
                    .ThenInclude(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                    .FirstOrDefaultAsync(s => s.SlotId == request.SlotId);

                var currentReg = updatedSlot?.ParkingRegistrations.FirstOrDefault();

                var slotDto = new ParkingSlotDto
                {
                    SlotId = updatedSlot!.SlotId,
                    SlotCode = updatedSlot.SlotCode,
                    Status = updatedSlot.Status,
                    CurrentRegistrationId = currentReg?.RegistrationId,
                    VehiclePlateNumber = currentReg?.Vehicle?.PlateNumber,
                    VehicleType = currentReg?.Vehicle?.VehicleType,
                    CustomerId = currentReg?.Vehicle?.CustomerId,
                    CustomerName = currentReg?.Vehicle?.Customer?.FullName,
                    CustomerPhone = currentReg?.Vehicle?.Customer?.Phone,
                    CheckInTime = currentReg?.CheckInTime
                };

                _logger.LogInformation($"Successfully registered parking for slot {slot.SlotCode}");

                var response = new RegisterParkingResponse
                {
                    Success = true,
                    Message = "Đăng ký chỗ đỗ thành công",
                    RegistrationId = registration.RegistrationId,
                    CustomerId = customer.CustomerId,
                    VehicleId = vehicle.VehicleId,
                    CheckInTime = registration.CheckInTime,
                    UpdatedSlot = slotDto
                };

                // Broadcast to all clients
                await Clients.All.SendAsync("OnParkingRegistered", response);
                await Clients.All.SendAsync("OnSlotUpdated", slotDto);

                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi đăng ký parking");
                return new RegisterParkingResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                };
            }
        }


        public async Task<RegisterParkingResponse> RegisterParkingCustomer(RegisterParkingRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Kiểm tra slot có tồn tại và available không
                var slot = await _context.ParkingSlots.FindAsync(request.SlotId);
                if (slot == null)
                {
                    return new RegisterParkingResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy chỗ đỗ"
                    };
                }

                if (slot.Status != "Available")
                {
                    return new RegisterParkingResponse
                    {
                        Success = false,
                        Message = "Chỗ đỗ này đã được sử dụng"
                    };
                }

                // 2. Tìm hoặc tạo khách hàng
                Customer? customer;
                if (request.CustomerId.HasValue)
                {
                    customer = await _context.Customers.FindAsync(request.CustomerId.Value);
                    if (customer == null)
                    {
                        return new RegisterParkingResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy khách hàng"
                        };
                    }
                }
                // 3. ⭐ KIỂM TRA XE ĐÃ ĐANG ĐỖ Ở SLOT KHÁC CHƯA
                var existingActiveRegistration = await _context.ParkingRegistrations
                    .Include(r => r.Slot)
                    .FirstOrDefaultAsync(r =>
                        r.VehicleId == request.VehicleId &&
                        (r.Status == "Active" || r.Status == "CheckedIn"));

                if (existingActiveRegistration != null)
                {
                    return new RegisterParkingResponse
                    {
                        Success = false,
                        Message = $"Xe này đang đỗ ở chỗ {existingActiveRegistration.Slot?.SlotCode}. Vui lòng check-out trước khi đăng ký chỗ mới."
                    };
                }

                // 4. Kiểm tra vehicle có tồn tại và thuộc về customer này không
                var vehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.VehicleId == request.VehicleId);

                if (vehicle == null)
                {
                    return new RegisterParkingResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy xe"
                    };
                }

                if (vehicle.CustomerId != request.CustomerId.Value)
                {
                    return new RegisterParkingResponse
                    {
                        Success = false,
                        Message = "Xe này không thuộc về bạn"
                    };
                }

                // 4. Tạo parking registration
                var registration = new ParkingRegistration
                {
                    RegistrationId = Guid.NewGuid(),
                    VehicleId = request.VehicleId,
                    SlotId = request.SlotId,
                    CheckInTime = DateTime.Now,
                    Status = "Active"
                };

                _context.ParkingRegistrations.Add(registration);

                // 5. Cập nhật trạng thái slot
                slot.Status = "InUse";

                // 6. Lưu tất cả thay đổi
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 7. Load lại thông tin slot để trả về
                var updatedSlot = await _context.ParkingSlots
                    .Include(s => s.ParkingRegistrations.Where(r => r.Status == "Active"))
                    .ThenInclude(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                    .FirstOrDefaultAsync(s => s.SlotId == request.SlotId);

                var currentReg = updatedSlot?.ParkingRegistrations.FirstOrDefault();

                var slotDto = new ParkingSlotDto
                {
                    SlotId = updatedSlot!.SlotId,
                    SlotCode = updatedSlot.SlotCode,
                    Status = updatedSlot.Status,
                    CurrentRegistrationId = currentReg?.RegistrationId,
                    VehiclePlateNumber = currentReg?.Vehicle?.PlateNumber,
                    VehicleType = currentReg?.Vehicle?.VehicleType,
                    CustomerId = currentReg?.Vehicle?.CustomerId,
                    CustomerName = currentReg?.Vehicle?.Customer?.FullName,
                    CustomerPhone = currentReg?.Vehicle?.Customer?.Phone,
                    CheckInTime = currentReg?.CheckInTime
                };

                _logger.LogInformation($"Successfully registered parking for slot {slot.SlotCode}");

                var response = new RegisterParkingResponse
                {
                    Success = true,
                    Message = "Đăng ký chỗ đỗ thành công",
                    RegistrationId = registration.RegistrationId,
                    CustomerId = request.CustomerId,
                    VehicleId = request.VehicleId,
                    CheckInTime = registration.CheckInTime,
                    UpdatedSlot = slotDto
                };

                // Broadcast to all clients
                await Clients.All.SendAsync("OnParkingRegistered", response);
                await Clients.All.SendAsync("OnSlotUpdated", slotDto);

                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi đăng ký parking");
                return new RegisterParkingResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                };
            }
        }

        public async Task<CheckOutResponse> CheckOut(CheckOutRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tìm registration
                var registration = await _context.ParkingRegistrations
                    .Include(r => r.Slot)
                    .FirstOrDefaultAsync(r => r.RegistrationId == request.RegistrationId);

                if (registration == null)
                {
                    return new CheckOutResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin đăng ký"
                    };
                }

                if (registration.Status == "CheckedOut")
                {
                    return new CheckOutResponse
                    {
                        Success = false,
                        Message = "Đã check out trước đó"
                    };
                }

                // 2. Cập nhật registration
                registration.CheckOutTime = DateTime.Now;
                registration.Status = "CheckedOut";

                // 3. Cập nhật slot status
                var slotId = registration.SlotId;
                if (registration.Slot != null)
                {
                    registration.Slot.Status = "Available";
                }

                // 4. Tạo payment nếu có
                if (request.PaymentAmount.HasValue && request.PaymentAmount.Value > 0)
                {
                    var payment = new Payment
                    {
                        PaymentId = Guid.NewGuid(),
                        RegistrationId = registration.RegistrationId,
                        Amount = request.PaymentAmount.Value,
                        PaymentMethod = request.PaymentMethod ?? "Cash",
                        PaymentDate = DateTime.Now
                    };
                    _context.Payments.Add(payment);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var duration = registration.CheckOutTime.Value - registration.CheckInTime;

                _logger.LogInformation($"Successfully checked out registration {registration.RegistrationId}");

                var response = new CheckOutResponse
                {
                    Success = true,
                    Message = "Check out thành công",
                    CheckOutTime = registration.CheckOutTime,
                    TotalAmount = request.PaymentAmount,
                    Duration = duration
                };

                // Broadcast to all clients
                await Clients.All.SendAsync("OnSlotCheckedOut", slotId);

                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi check out");
                return new CheckOutResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                };
            }
        }

        // ============ CUSTOMER VIEW OPERATIONS (Privacy Protected) ============

        /// <summary>
        /// Lấy slots theo khu vực cho Customer - Hiển thị thông tin chi tiết CHỈ cho slot của chính họ
        /// </summary>
        public async Task<List<CustomerParkingAreaDto>> GetSlotsByAreaForCustomer(Guid customerId)
        {
            try
            {
                _logger.LogInformation($"Customer {customerId} đang lấy slots theo khu vực...");
                
                var allSlots = await _context.ParkingSlots
                    .Include(s => s.ParkingRegistrations
                        .Where(r => r.Status == "Active" || r.Status == "CheckedIn"))
                    .ThenInclude(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                    .OrderBy(s => s.SlotCode)
                    .ToListAsync();

                // Nhóm slots theo khu vực
                var groupedSlots = allSlots
                    .GroupBy(s => s.SlotCode.Length > 0 ? s.SlotCode[0].ToString().ToUpper() : "Unknown")
                    .Select(g => new CustomerParkingAreaDto
                    {
                        AreaName = $"Khu {g.Key}",
                        TotalSlots = g.Count(),
                        AvailableSlots = g.Count(s => s.Status == "Available"),
                        OccupiedSlots = g.Count(s => s.Status == "InUse" || s.Status == "Occupied"),
                        Slots = g.Select(s =>
                        {
                            var currentReg = s.ParkingRegistrations
                                .FirstOrDefault(r => r.Status == "Active" || r.Status == "CheckedIn");
                            
                            // Kiểm tra xem slot này có phải của customer hiện tại không
                            bool isMySlot = currentReg != null && 
                                           currentReg.Vehicle != null && 
                                           currentReg.Vehicle.CustomerId == customerId;

                            return new CustomerParkingSlotDto
                            {
                                SlotId = s.SlotId,
                                SlotCode = s.SlotCode,
                                Status = s.Status,
                                IsMySlot = isMySlot,
                                // Chỉ hiển thị thông tin chi tiết nếu là slot của customer này
                                CurrentRegistrationId = isMySlot ? currentReg?.RegistrationId : null,
                                VehiclePlateNumber = isMySlot ? currentReg?.Vehicle?.PlateNumber : null,
                                VehicleType = isMySlot ? currentReg?.Vehicle?.VehicleType : null,
                                CustomerId = isMySlot ? currentReg?.Vehicle?.CustomerId : null,
                                CustomerPhone = isMySlot ? currentReg?.Vehicle?.Customer?.Phone : null,
                                CheckInTime = isMySlot ? currentReg?.CheckInTime : null
                            };
                        }).ToList()
                    })
                    .OrderBy(a => a.AreaName)
                    .ToList();

                _logger.LogInformation($"Đã trả về {groupedSlots.Count} khu vực cho Customer (privacy protected)");
                return groupedSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slots cho Customer");
                throw new HubException($"Error getting slots: {ex.Message}");
            }
            }

        /// <summary>
        /// Lấy tổng quan parking cho Customer - Chỉ số liệu thống kê
        /// </summary>
        public async Task<ParkingOverviewDto> GetParkingOverviewForCustomer()
        {
            // Tổng quan thì cả Customer và Staff đều xem được số liệu như nhau
            return await GetParkingOverview();
        }
    }
}
