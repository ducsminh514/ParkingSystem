using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;
using ParkingSystem.Shared.DTOs;

namespace ParkingSystem.Server.Hubs
{
    public partial class ParkingHub
    {
        public async Task<ParkingHistoryResponse> GetCustomerParkingHistory(Guid customerId)
        {
            try
            {
                var histories = await _context.ParkingRegistrations
                    .Include(pr => pr.Vehicle)
                    .Include(pr => pr.Slot)
                    .Include(pr => pr.Staff)
                    .Where(pr => pr.Vehicle.CustomerId == customerId)
                    .OrderByDescending(pr => pr.CheckInTime)
                    .Select(pr => new ParkingHistoryDto
                    {
                        RegistrationID = pr.RegistrationId,
                        PlateNumber = pr.Vehicle.PlateNumber,
                        VehicleType = pr.Vehicle.VehicleType ?? "N/A",
                        SlotCode = pr.Slot.SlotCode,
                        CheckInTime = pr.CheckInTime,
                        CheckOutTime = pr.CheckOutTime,
                        Status = pr.Status,
                        StaffName = pr.Staff != null ? pr.Staff.FullName : null
                    })
                    .ToListAsync();

                var activeCount = histories.Count(h => h.Status == "Active");

                return new ParkingHistoryResponse
                {
                    Success = true,
                    Message = "Successfully fetched parking history",
                    Histories = histories,
                    TotalRecords = histories.Count,
                    ActiveParking = activeCount
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetCustomerParkingHistory Error] {ex.Message}");
                return new ParkingHistoryResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Histories = new List<ParkingHistoryDto>(),
                    TotalRecords = 0,
                    ActiveParking = 0
                };
            }
        }

        /// <summary>
        /// Broadcast khi có check-in mới (để update real-time cho trang history)
        /// </summary>
        public async Task NotifyNewCheckIn(Guid customerId, ParkingHistoryDto newHistory)
        {
            await Clients.All.SendAsync("OnNewCheckIn", customerId, newHistory);
        }

        /// <summary>
        /// Broadcast khi có check-out (để update real-time cho trang history)
        /// </summary>
        public async Task NotifyCheckOut(Guid customerId, Guid registrationId)
        {
            await Clients.All.SendAsync("OnCheckOut", customerId, registrationId);
        }
        // ============ STAFF REGISTRATION WORKFLOW ============

        /// <summary>
        /// Bước 1: Kiểm tra customer theo số điện thoại
        /// </summary>
        public async Task<CustomerCheckResult> CheckCustomerByPhone(string phoneNumber)
        {
            try
            {
                _logger.LogInformation($"Checking customer with phone: {phoneNumber}");

                var customer = await _context.Customers
                    .Include(c => c.Vehicles)
                    .FirstOrDefaultAsync(c => c.Phone == phoneNumber);

                if (customer == null)
                {
                    return new CustomerCheckResult
                    {
                        Exists = false,
                        Phone = phoneNumber,
                        Vehicles = new List<VehicleSummaryDto>()
                    };
                }

                // Lấy danh sách xe và kiểm tra xe nào đang đỗ
                var vehicles = new List<VehicleSummaryDto>();
                
                foreach (var vehicle in customer.Vehicles)
                {
                    var activeReg = await _context.ParkingRegistrations
                        .Include(r => r.Slot)
                        .FirstOrDefaultAsync(r => 
                            r.VehicleId == vehicle.VehicleId && 
                            (r.Status == "Active" || r.Status == "CheckedIn"));

                    vehicles.Add(new VehicleSummaryDto
                    {
                        VehicleId = vehicle.VehicleId,
                        PlateNumber = vehicle.PlateNumber,
                        VehicleType = vehicle.VehicleType ?? "N/A",
                        IsCurrentlyParked = activeReg != null,
                        CurrentSlotCode = activeReg?.Slot?.SlotCode
                    });
                }

                return new CustomerCheckResult
                {
                    Exists = true,
                    CustomerId = customer.CustomerId,
                    FullName = customer.FullName,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Vehicles = vehicles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking customer by phone");
                throw new HubException($"Error checking customer: {ex.Message}");
            }
        }

        /// <summary>
        /// Đăng ký parking cho Staff - Xử lý toàn bộ flow
        /// </summary>
        public async Task<RegisterParkingResponse> StaffRegisterParking(StaffRegisterParkingRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation($"Staff registering parking for slot {request.SlotId}");

                // 1. Kiểm tra slot
                var slot = await _context.ParkingSlots.FindAsync(request.SlotId);
                if (slot == null)
                {
                    return new RegisterParkingResponse
                    {
                        Success = false,
                        Message = "Slot not found"
                    };
                }

                if (slot.Status != "Available")
                {
                    return new RegisterParkingResponse
                    {
                        Success = false,
                        Message = "This slot is already in use"
                    };
                }

                // 2. Xử lý Customer
                Customer customer;

                if (request.CustomerId.HasValue)
                {
                    // Customer đã tồn tại
                    customer = await _context.Customers.FindAsync(request.CustomerId.Value);
                    if (customer == null)
                    {
                        return new RegisterParkingResponse
                        {
                            Success = false,
                            Message = "Customer not found"
                        };
                    }
                }
                else
                {
                    // Tạo customer mới
                    if (string.IsNullOrWhiteSpace(request.CustomerName))
                    {
                        return new RegisterParkingResponse
                        {
                            Success = false,
                            Message = "Please enter customer name"
                        };
                    }

                    // Kiểm tra số điện thoại đã tồn tại chưa
                    var existingCustomer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.Phone == request.CustomerPhone);

                    if (existingCustomer != null)
                    {
                        return new RegisterParkingResponse
                        {
                            Success = false,
                            Message = "This phone number is already registered. Please check again."
                        };
                    }

                    customer = new Customer
                    {
                        CustomerId = Guid.NewGuid(),
                        FullName = request.CustomerName,
                        Phone = request.CustomerPhone,
                        Email = request.CustomerEmail,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword($"customer_{request.CustomerPhone}") // Default password
                    };

                    _context.Customers.Add(customer);
                    _logger.LogInformation($"Created new customer: {customer.FullName} - {customer.Phone}");
                }

                // 3. Xử lý Vehicle
                Vehicle vehicle;

                if (request.VehicleId.HasValue)
                {
                    // Xe đã tồn tại - Kiểm tra xe có đang đỗ không
                    vehicle = await _context.Vehicles.FindAsync(request.VehicleId.Value);
                    if (vehicle == null)
                    {
                        return new RegisterParkingResponse
                        {
                            Success = false,
                            Message = "Vehicle not found"
                        };
                    }

                    // Kiểm tra xe có đang đỗ ở slot khác không
                    var existingActiveReg = await _context.ParkingRegistrations
                        .Include(r => r.Slot)
                        .FirstOrDefaultAsync(r => 
                            r.VehicleId == vehicle.VehicleId && 
                            (r.Status == "Active" || r.Status == "CheckedIn"));

                    if (existingActiveReg != null)
                    {
                        return new RegisterParkingResponse
                        {
                            Success = false,
                            Message = $"Vehicle {vehicle.PlateNumber} is parked at slot {existingActiveReg.Slot?.SlotCode}. Please check out first."
                        };
                    }
                }
                else
                {
                    // Tạo xe mới
                    if (string.IsNullOrWhiteSpace(request.PlateNumber))
                    {
                        return new RegisterParkingResponse
                        {
                            Success = false,
                            Message = "Please enter vehicle plate number"
                        };
                    }

                    // Kiểm tra biển số đã tồn tại chưa
                    var existingVehicle = await _context.Vehicles
                        .FirstOrDefaultAsync(v => v.PlateNumber == request.PlateNumber);

                    if (existingVehicle != null)
                    {
                        // Nếu xe đã tồn tại nhưng thuộc về customer này
                        if (existingVehicle.CustomerId == customer.CustomerId)
                        {
                            vehicle = existingVehicle;
                            _logger.LogInformation($"Using existing vehicle: {vehicle.PlateNumber}");
                        }
                        else
                        {
                            return new RegisterParkingResponse
                            {
                                Success = false,
                                Message = $"Plate number {request.PlateNumber} has already been registered by another customer"
                            };
                        }
                    }
                    else
                    {
                        vehicle = new Vehicle
                        {
                            VehicleId = Guid.NewGuid(),
                            PlateNumber = request.PlateNumber,
                            VehicleType = request.VehicleType,
                            CustomerId = customer.CustomerId
                        };
                        
                        _context.Vehicles.Add(vehicle);
                        _logger.LogInformation($"Created new vehicle: {vehicle.PlateNumber}");
                    }
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
                    CustomerName = currentReg?.Vehicle?.Customer?.FullName,
                    CustomerPhone = currentReg?.Vehicle?.Customer?.Phone,
                    CheckInTime = currentReg?.CheckInTime
                };

                _logger.LogInformation($"Successfully registered parking: Slot {slot.SlotCode}, Vehicle {vehicle.PlateNumber}");

                var response = new RegisterParkingResponse
                {
                    Success = true,
                    Message = "Parking slot registered successfully",
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
                _logger.LogError(ex, "Error in StaffRegisterParking");
                return new RegisterParkingResponse
                {
                    Success = false,
                    Message = $"Server error: {ex.Message}"
                };
            }
        }
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
                _logger.LogInformation("Start fetching slots by area...");

                // Check DB connection
                var totalCount = await _context.ParkingSlots.CountAsync();
                _logger.LogInformation($"Total slots in database: {totalCount}");

                if (totalCount == 0)
                {
                    _logger.LogWarning("No slots found in database!");
                    return new List<ParkingAreaDto>();
                }

                var allSlots = await _context.ParkingSlots
                    .Include(s => s.ParkingRegistrations
                        .Where(r => r.Status == "Active" || r.Status == "CheckedIn"))
                    .ThenInclude(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                    .OrderBy(s => s.SlotCode)
                    .ToListAsync();

                _logger.LogInformation($"Fetched {allSlots.Count} slots from database");

                // Nhóm slots theo ký tự đầu tiên (Zone/Area)
                var groupedSlots = allSlots
                    .GroupBy(s => s.SlotCode.Length > 0 ? s.SlotCode[0].ToString().ToUpper() : "Unknown")
                    .Select(g => new ParkingAreaDto
                    {
                        AreaName = $"Zone {g.Key}",
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

                _logger.LogInformation($"Grouped into {groupedSlots.Count} areas");
                return groupedSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slots by area: {Message}", ex.Message);
                throw new HubException($"Error getting slots by area: {ex.Message}");
            }
        }

        public async Task<ParkingOverviewDto> GetParkingOverview()
        {
            try
            {
                _logger.LogInformation("Start fetching parking overview...");

                var totalSlots = await _context.ParkingSlots.CountAsync();
                _logger.LogInformation($"Total slots: {totalSlots}");

                var availableSlots = await _context.ParkingSlots
                    .CountAsync(s => s.Status == "Available");
                _logger.LogInformation($"Available slots: {availableSlots}");

                var occupiedSlots = await _context.ParkingSlots
                    .CountAsync(s => s.Status == "InUse" || s.Status == "Occupied");
                _logger.LogInformation($"Occupied slots: {occupiedSlots}");

                var overview = new ParkingOverviewDto
                {
                    TotalSlots = totalSlots,
                    AvailableSlots = availableSlots,
                    OccupiedSlots = occupiedSlots,
                    OccupancyRate = totalSlots > 0
                        ? Math.Round((double)occupiedSlots / totalSlots * 100, 1)
                        : 0
                };

                _logger.LogInformation($"Overview complete: Total={overview.TotalSlots}, Available={overview.AvailableSlots}, Occupied={overview.OccupiedSlots}");
                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching parking overview: {Message}", ex.Message);
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
                _logger.LogError(ex, $"Error fetching slot {id}");
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
                _logger.LogError(ex, "Error getting available slots");
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
                _logger.LogError(ex, $"Error updating slot status {id}");
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
                        Message = "Slot not found"
                    };
                }

                if (slot.Status != "Available")
                {
                    return new RegisterParkingResponse
                    {
                        Success = false,
                        Message = "This slot is already in use"
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
                            Message = "Customer not found"
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
                    Message = "Parking slot registered successfully",
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
                _logger.LogError(ex, "Error registering parking");
                return new RegisterParkingResponse
                {
                    Success = false,
                    Message = $"Server error: {ex.Message}"
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
                        Message = "Slot not found"
                    };
                }

                if (slot.Status != "Available")
                {
                    return new RegisterParkingResponse
                    {
                        Success = false,
                        Message = "This slot is already in use"
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
                            Message = "Customer not found"
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
                        Message = $"This vehicle is currently parked at slot {existingActiveRegistration.Slot?.SlotCode}. Please check out before registering a new slot."
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
                        Message = "Vehicle not found"
                    };
                }

                if (vehicle.CustomerId != request.CustomerId.Value)
                {
                    return new RegisterParkingResponse
                    {
                        Success = false,
                        Message = "This vehicle does not belong to you"
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
                    Message = "Parking slot registered successfully",
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
                    Message = $"Server error: {ex.Message}"
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
                        Message = "Registration not found"
                    };
                }

                if (registration.Status == "CheckedOut")
                {
                    return new CheckOutResponse
                    {
                        Success = false,
                        Message = "Already checked out"
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
                    Message = "Check out completed",
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
                    Message = $"Server error: {ex.Message}"
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
                _logger.LogInformation($"Customer {customerId} is fetching slots by area...");

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
                        AreaName = $"Zone {g.Key}",
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

                _logger.LogInformation($"Returned {groupedSlots.Count} areas for customer (privacy protected)");
                return groupedSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slots for customer");
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
