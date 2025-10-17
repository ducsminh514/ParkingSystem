using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;
using ParkingSystem.Shared.Models;

namespace ParkingSystem.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class ParkingSlotController : ControllerBase
    {
        private readonly ParkingManagementContext _context;
        private readonly ILogger<ParkingSlotController> _logger;

        public ParkingSlotController(
            ParkingManagementContext context, 
            ILogger<ParkingSlotController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy tất cả các slot
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ParkingSlotDto>>> GetAllSlots()
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

                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách slots");
                return StatusCode(500, "Lỗi server khi lấy dữ liệu");
            }
        }

        /// <summary>
        /// Lấy các slot theo khu vực/bãi đỗ
        /// Nhóm theo ký tự đầu tiên của SlotCode (A, B, C, ...)
        /// </summary>
        [HttpGet("by-area")]
        public async Task<ActionResult<List<ParkingAreaDto>>> GetSlotsByArea()
        {
            try
            {
                var allSlots = await _context.ParkingSlots
                    .Include(s => s.ParkingRegistrations
                        .Where(r => r.Status == "Active" || r.Status == "CheckedIn"))
                    .ThenInclude(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                    .OrderBy(s => s.SlotCode)
                    .ToListAsync();

                // Nhóm slots theo ký tự đầu tiên (Zone/Area)
                var groupedSlots = allSlots
                    .GroupBy(s => s.SlotCode.Length > 0 ? s.SlotCode[0].ToString().ToUpper() : "Unknown")
                    .Select(g => new ParkingAreaDto
                    {
                        AreaName = $"Khu {g.Key}",
                        TotalSlots = g.Count(),
                        AvailableSlots = g.Count(s => s.Status == "Available"),
                        OccupiedSlots = g.Count(s => s.Status != "Available"),
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
                                CustomerName = currentReg?.Vehicle?.Customer?.FullName,
                                CustomerPhone = currentReg?.Vehicle?.Customer?.Phone,
                                CheckInTime = currentReg?.CheckInTime
                            };
                        }).ToList()
                    })
                    .OrderBy(a => a.AreaName)
                    .ToList();

                return Ok(groupedSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slots theo khu vực");
                return StatusCode(500, "Lỗi server khi lấy dữ liệu");
            }
        }

        /// <summary>
        /// Lấy tổng quan về trạng thái các slot
        /// </summary>
        [HttpGet("overview")]
        public async Task<ActionResult<object>> GetParkingOverview()
        {
            try
            {
                var totalSlots = await _context.ParkingSlots.CountAsync();
                var availableSlots = await _context.ParkingSlots
                    .CountAsync(s => s.Status == "Available");
                var occupiedSlots = totalSlots - availableSlots;

                var overview = new
                {
                    TotalSlots = totalSlots,
                    AvailableSlots = availableSlots,
                    OccupiedSlots = occupiedSlots,
                    OccupancyRate = totalSlots > 0 
                        ? Math.Round((double)occupiedSlots / totalSlots * 100, 1) 
                        : 0
                };

                return Ok(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tổng quan parking");
                return StatusCode(500, "Lỗi server khi lấy dữ liệu");
            }
        }

        /// <summary>
        /// Lấy chi tiết một slot theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ParkingSlotDto>> GetSlotById(Guid id)
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
                    return NotFound($"Không tìm thấy slot với ID: {id}");
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
                    CustomerName = currentReg?.Vehicle?.Customer?.FullName,
                    CustomerPhone = currentReg?.Vehicle?.Customer?.Phone,
                    CheckInTime = currentReg?.CheckInTime
                };

                return Ok(slotDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy slot {id}");
                return StatusCode(500, "Lỗi server khi lấy dữ liệu");
            }
        }

        /// <summary>
        /// Lấy các slot trống
        /// </summary>
        [HttpGet("available")]
        public async Task<ActionResult<List<ParkingSlotDto>>> GetAvailableSlots()
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

                return Ok(availableSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slots trống");
                return StatusCode(500, "Lỗi server khi lấy dữ liệu");
            }
        }

        /// <summary>
        /// Cập nhật trạng thái slot
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<ParkingSlotDto>> UpdateSlotStatus(
            Guid id, 
            [FromBody] UpdateSlotStatusRequest request)
        {
            try
            {
                var slot = await _context.ParkingSlots.FindAsync(id);
                
                if (slot == null)
                {
                    return NotFound($"Không tìm thấy slot với ID: {id}");
                }

                slot.Status = request.Status;
                await _context.SaveChangesAsync();

                var slotDto = new ParkingSlotDto
                {
                    SlotId = slot.SlotId,
                    SlotCode = slot.SlotCode,
                    Status = slot.Status
                };

                return Ok(slotDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật trạng thái slot {id}");
                return StatusCode(500, "Lỗi server khi cập nhật dữ liệu");
            }
        }

        /// <summary>
        /// Đăng ký parking cho khách hàng
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<RegisterParkingResponse>> RegisterParking(
            [FromBody] RegisterParkingRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Kiểm tra slot có tồn tại và available không
                var slot = await _context.ParkingSlots.FindAsync(request.SlotId);
                if (slot == null)
                {
                    return NotFound(new RegisterParkingResponse 
                    { 
                        Success = false, 
                        Message = "Không tìm thấy chỗ đỗ" 
                    });
                }

                if (slot.Status != "Available")
                {
                    return BadRequest(new RegisterParkingResponse 
                    { 
                        Success = false, 
                        Message = "Chỗ đỗ này đã được sử dụng" 
                    });
                }

                // 2. Tìm hoặc tạo khách hàng
                Customer customer;
                if (request.CustomerId.HasValue)
                {
                    customer = await _context.Customers.FindAsync(request.CustomerId.Value);
                    if (customer == null)
                    {
                        return NotFound(new RegisterParkingResponse 
                        { 
                            Success = false, 
                            Message = "Không tìm thấy khách hàng" 
                        });
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
                slot.Status = "Occupied";

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

                _logger.LogInformation($"Successfully registered parking for slot {slot.SlotCode}");

                return Ok(new RegisterParkingResponse
                {
                    Success = true,
                    Message = "Đăng ký chỗ đỗ thành công",
                    RegistrationId = registration.RegistrationId,
                    CustomerId = customer.CustomerId,
                    VehicleId = vehicle.VehicleId,
                    CheckInTime = registration.CheckInTime,
                    UpdatedSlot = slotDto
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi đăng ký parking");
                return StatusCode(500, new RegisterParkingResponse 
                { 
                    Success = false, 
                    Message = $"Lỗi server: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Check out và kết thúc parking registration
        /// </summary>
        [HttpPost("checkout")]
        public async Task<ActionResult<CheckOutResponse>> CheckOut([FromBody] CheckOutRequest request)
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
                    return NotFound(new CheckOutResponse 
                    { 
                        Success = false, 
                        Message = "Không tìm thấy thông tin đăng ký" 
                    });
                }

                if (registration.Status == "CheckedOut")
                {
                    return BadRequest(new CheckOutResponse 
                    { 
                        Success = false, 
                        Message = "Đã check out trước đó" 
                    });
                }

                // 2. Cập nhật registration
                registration.CheckOutTime = DateTime.Now;
                registration.Status = "CheckedOut";

                // 3. Cập nhật slot status
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

                return Ok(new CheckOutResponse
                {
                    Success = true,
                    Message = "Check out thành công",
                    CheckOutTime = registration.CheckOutTime,
                    TotalAmount = request.PaymentAmount,
                    Duration = duration
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi check out");
                return StatusCode(500, new CheckOutResponse 
                { 
                    Success = false, 
                    Message = $"Lỗi server: {ex.Message}" 
                });
            }
        }
    }

    public class UpdateSlotStatusRequest
    {
        public string Status { get; set; } = null!;
    }
}

