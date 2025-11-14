// Thêm vào ParkingHub.cs (partial class)

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;
using ParkingSystem.Shared.DTOs;

namespace ParkingSystem.Server.Hubs
{
    public partial class ParkingHub
    {
        // ============ PRICING & CHECK-OUT METHODS ============

        /// <summary>
        /// Lấy danh sách bảng giá
        /// </summary>
        public async Task<List<ParkingPriceDto>> GetParkingPrices()
        {
            try
            {
                var prices = await _context.ParkingPrices
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.VehicleType)
                    .Select(p => new ParkingPriceDto
                    {
                        PriceId = p.PriceId,
                        VehicleType = p.VehicleType,
                        PricePerHour = p.PricePerHour,
                        IsActive = p.IsActive
                    })
                    .ToListAsync();

                return prices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parking prices");
                throw new HubException($"Error getting prices: {ex.Message}");
            }
        }

        /// <summary>
        /// Tính phí trước khi check-out
        /// </summary>
        public async Task<CalculateFeeResponse> CalculateParkingFee(Guid registrationId)
        {
            try
            {
                _logger.LogInformation($"Calculating fee for registration {registrationId}");

                var registration = await _context.ParkingRegistrations
                    .Include(r => r.Slot)
                    .Include(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                    .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

                if (registration == null)
                {
                    return new CalculateFeeResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin đăng ký"
                    };
                }

                if (registration.Status == "CheckedOut")
                {
                    return new CalculateFeeResponse
                    {
                        Success = false,
                        Message = "Đã check-out trước đó"
                    };
                }

                // Lấy giá theo loại xe
                var vehicleType = registration.Vehicle?.VehicleType ?? "Xe máy";
                var price = await _context.ParkingPrices
                    .FirstOrDefaultAsync(p => p.VehicleType == vehicleType );

                if (price == null)
                {
                    // Fallback: Giá mặc định nếu không tìm thấy
                    _logger.LogWarning($"Price not found for vehicle type: {vehicleType}, using default");
                    price = new ParkingPrice { PricePerHour = 10000 };
                }

                // Tính thời gian đỗ
                var checkInTime = registration.CheckInTime;
                var checkOutTime = DateTime.Now;
                var duration = checkOutTime - checkInTime;

                // Tính số giờ (chính xác, có thập phân)
                var totalHours = duration.TotalHours;

                // Tính tổng tiền
                var totalAmount = (decimal)totalHours * price.PricePerHour;

                // Format duration display
                var durationDisplay = duration.Days > 0
                    ? $"{duration.Days} ngày {duration.Hours} giờ {duration.Minutes} phút"
                    : duration.Hours > 0
                        ? $"{duration.Hours} giờ {duration.Minutes} phút"
                        : $"{duration.Minutes} phút";

                return new CalculateFeeResponse
                {
                    Success = true,
                    Message = "Tính phí thành công",
                    SlotCode = registration.Slot?.SlotCode ?? "N/A",
                    VehiclePlateNumber = registration.Vehicle?.PlateNumber ?? "N/A",
                    VehicleType = vehicleType,
                    CustomerName = registration.Vehicle?.Customer?.FullName ?? "N/A",
                    CustomerPhone = registration.Vehicle?.Customer?.Phone ?? "N/A",
                    CheckInTime = checkInTime,
                    CheckOutTime = checkOutTime,
                    Duration = duration,
                    DurationDisplay = durationDisplay,
                    PricePerHour = price.PricePerHour,
                    TotalHours = Math.Round(totalHours, 2),
                    TotalAmount = Math.Round(totalAmount, 0) // Làm tròn đến VNĐ
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating parking fee");
                return new CalculateFeeResponse
                {
                    Success = false,
                    Message = $"Lỗi tính phí: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Check-out với payment
        /// </summary>
        public async Task<CheckOutWithPaymentResponse> CheckOutWithPayment(CheckOutWithPaymentRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation($"Processing check-out for registration {request.RegistrationId}");

                // 1. Tìm registration
                var registration = await _context.ParkingRegistrations
                    .Include(r => r.Slot)
                    .Include(r => r.Vehicle)
                    .FirstOrDefaultAsync(r => r.RegistrationId == request.RegistrationId);

                if (registration == null)
                {
                    return new CheckOutWithPaymentResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin đăng ký"
                    };
                }

                if (registration.Status == "CheckedOut")
                {
                    return new CheckOutWithPaymentResponse
                    {
                        Success = false,
                        Message = "Đã check-out trước đó"
                    };
                }

                // 2. Cập nhật registration
                var checkOutTime = DateTime.Now;
                registration.CheckOutTime = checkOutTime;
                registration.Status = "CheckedOut";

                // 3. Cập nhật slot status
                if (registration.Slot != null)
                {
                    registration.Slot.Status = "Available";
                }

                // 4. Tạo payment record
                var payment = new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    RegistrationId = registration.RegistrationId,
                    Amount = request.PaymentAmount,
                    PaymentMethod = request.PaymentMethod,
                    PaymentDate = checkOutTime
                };

                _context.Payments.Add(payment);

                // 5. Lưu tất cả thay đổi
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var duration = checkOutTime - registration.CheckInTime;

                _logger.LogInformation($"Successfully checked out: Registration {request.RegistrationId}, Amount: {request.PaymentAmount}");

                var response = new CheckOutWithPaymentResponse
                {
                    Success = true,
                    Message = "Check-out thành công",
                    CheckOutTime = checkOutTime,
                    Duration = duration,
                    TotalAmount = request.PaymentAmount,
                    PaymentMethod = request.PaymentMethod,
                    PaymentId = payment.PaymentId
                };

                // 6. Broadcast to all clients
                await Clients.All.SendAsync("OnSlotCheckedOut", registration.SlotId);

                // 7. Broadcast updated slot
                var updatedSlot = new ParkingSlotDto
                {
                    SlotId = registration.Slot!.SlotId,
                    SlotCode = registration.Slot.SlotCode,
                    Status = registration.Slot.Status,
                    //IsAvailable = true 
                };
                await Clients.All.SendAsync("OnSlotUpdated", updatedSlot);

                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in check-out with payment");
                return new CheckOutWithPaymentResponse
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                };
            }
        }
    }
}