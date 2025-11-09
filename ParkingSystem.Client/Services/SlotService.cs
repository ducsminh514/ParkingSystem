using Microsoft.AspNetCore.SignalR.Client;
using ParkingSystem.Shared.Models;
using ParkingSystem.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace ParkingSystem.Client.Services
{
    public class SlotService : IAsyncDisposable
    {
        private readonly ILogger<SlotService> _logger;
        private readonly ISignalRConnectionService _signalRConnectionService;
        private HubConnection Connection => _signalRConnectionService.Connection;

        // Events for real-time updates
        public event Action<ParkingSlotDto>? OnSlotUpdated;
        public event Action<RegisterParkingResponse>? OnParkingRegistered;
        public event Action<Guid>? OnSlotCheckedOut;
        public event Action<Guid, ParkingHistoryDto>? OnNewCheckIn;
        public event Action<Guid, Guid>? OnCheckOut;

        public bool IsConnected => Connection?.State == HubConnectionState.Connected;

        public SlotService(ISignalRConnectionService signalRConnectionService, ILogger<SlotService> logger)
        {
            _signalRConnectionService = signalRConnectionService;
            _logger = logger;
            
            // Register real-time event listeners
            RegisterEventHandlers();
        }


        private void RegisterEventHandlers()
        {
            // Listen to slot updates
            Connection.On<ParkingSlotDto>("OnSlotUpdated", (slot) =>
            {
                _logger.LogInformation($"[Real-time] Slot updated: {slot.SlotCode}");
                OnSlotUpdated?.Invoke(slot);
            });

            // Listen to parking registrations
            Connection.On<RegisterParkingResponse>("OnParkingRegistered", (response) =>
            {
                _logger.LogInformation($"[Real-time] Parking registered");
                OnParkingRegistered?.Invoke(response);
            });

            // Listen to check-outs
            Connection.On<Guid>("OnSlotCheckedOut", (slotId) =>
            {
                _logger.LogInformation($"[Real-time] Slot checked out: {slotId}");
                OnSlotCheckedOut?.Invoke(slotId);
            });

            Connection.On<Guid, ParkingHistoryDto>("OnNewCheckIn", (customerId, history) =>
            {
                Console.WriteLine($"[SignalR Event] New check-in for customer {customerId}");
                OnNewCheckIn?.Invoke(customerId, history);
            });

            // Listen to check-out
            Connection.On<Guid, Guid>("OnCheckOut", (customerId, registrationId) =>
            {
                Console.WriteLine($"[SignalR Event] Check-out for registration {registrationId}");
                OnCheckOut?.Invoke(customerId, registrationId);
            });
        }

        // ============ HELPER METHODS ============

        private void EnsureConnected()
        {
            if (Connection?.State != HubConnectionState.Connected)
            {
                _logger.LogError("SignalR chưa kết nối. State: {State}", Connection?.State);
                throw new InvalidOperationException($"SignalR chưa kết nối. Trạng thái hiện tại: {Connection?.State}. Vui lòng kiểm tra kết nối server và thử lại.");
            }
        }

        public async Task<bool> TryReconnectAsync()
        {
            try
            {
                if (Connection.State == HubConnectionState.Disconnected)
                {
                    _logger.LogInformation("Attempting to reconnect SignalR...");
                    await _signalRConnectionService.StartAsync();
                    return true;
                }
                return Connection.State == HubConnectionState.Connected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect SignalR");
                return false;
            }
        }
        public async Task<ParkingHistoryResponse> GetMyHistoryAsync(Guid customerId)
        {
            try
            {
                EnsureConnected();

                Console.WriteLine($"[GetMyHistoryAsync] Requesting history for customer {customerId}");

                var response = await Connection.InvokeAsync<ParkingHistoryResponse>(
                    "GetCustomerParkingHistory",
                    customerId
                );

                Console.WriteLine($"[GetMyHistoryAsync] Received {response.TotalRecords} records");

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetMyHistoryAsync Error] {ex.Message}");
                return new ParkingHistoryResponse
                {
                    Success = false,
                    Message = $"Lỗi khi tải lịch sử: {ex.Message}",
                    Histories = new List<ParkingHistoryDto>(),
                    TotalRecords = 0,
                    ActiveParking = 0
                };
            }
        }
        // ============ PARKING SLOT OPERATIONS (STAFF) ============

        public async Task<List<ParkingSlotDto>> GetAllSlotsAsync()
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<List<ParkingSlotDto>>("GetAllSlots");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all slots");
                throw;
            }
        }

        /// <summary>
        /// Lấy slots theo khu vực cho STAFF - Có đầy đủ thông tin cá nhân
        /// </summary>
        public async Task<List<ParkingAreaDto>> GetSlotsByAreaAsync()
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<List<ParkingAreaDto>>("GetSlotsByArea");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slots by area");
                throw;
            }
        }

        public async Task<ParkingOverviewDto?> GetParkingOverviewAsync()
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<ParkingOverviewDto>("GetParkingOverview");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parking overview");
                throw;
            }
        }

        // ============ PARKING SLOT OPERATIONS (CUSTOMER - Privacy Protected) ============

        /// <summary>
        /// Lấy slots theo khu vực cho CUSTOMER - Hiển thị thông tin chi tiết CHỈ cho slot của chính customer đó
        /// </summary>
        public async Task<List<CustomerParkingAreaDto>> GetSlotsByAreaForCustomerAsync(Guid customerId)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<List<CustomerParkingAreaDto>>("GetSlotsByAreaForCustomer", customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slots by area for customer");
                throw;
            }
        }

        public async Task<ParkingOverviewDto?> GetParkingOverviewForCustomerAsync()
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<ParkingOverviewDto>("GetParkingOverviewForCustomer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parking overview for customer");
                throw;
            }
        }

        public async Task<List<ParkingSlotDto>> GetAvailableSlotsAsync()
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<List<ParkingSlotDto>>("GetAvailableSlots");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available slots");
                throw;
            }
        }

        public async Task<ParkingSlotDto?> GetSlotByIdAsync(Guid slotId)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<ParkingSlotDto>("GetSlotById", slotId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting slot {slotId}");
                throw;
            }
        }

        // ============ NEW: Get Active Registrations ============
        
        /// <summary>
        /// Lấy danh sách xe đang đỗ của customer
        /// </summary>
        //public async Task<List<ActiveRegistrationDto>> GetMyActiveRegistrations(Guid customerId)
        //{
        //    EnsureConnected();
        //    try
        //    {
        //        return await Connection.InvokeAsync<List<ActiveRegistrationDto>>("GetMyActiveRegistrations", customerId);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting active registrations");
        //        throw;
        //    }
        //}

        // ============ STAFF REGISTRATION WORKFLOW ============

        /// <summary>
        /// Kiểm tra customer theo số điện thoại (Staff)
        /// </summary>
        public async Task<CustomerCheckResult> CheckCustomerByPhone(string phoneNumber)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<CustomerCheckResult>("CheckCustomerByPhone", phoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking customer by phone");
                throw;
            }
        }

        /// <summary>
        /// Đăng ký parking cho Staff (bao gồm tạo customer/vehicle mới)
        /// </summary>
        public async Task<RegisterParkingResponse> StaffRegisterParking(StaffRegisterParkingRequest request)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<RegisterParkingResponse>("StaffRegisterParking", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in staff registration");
                return new RegisterParkingResponse 
                { 
                    Success = false, 
                    Message = $"Lỗi kết nối: {ex.Message}" 
                };
            }
        }

        // ============ PARKING REGISTRATION OPERATIONS ============

        public async Task<RegisterParkingResponse> RegisterParkingAsync(RegisterParkingRequest request)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<RegisterParkingResponse>("RegisterParkingCustomer", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering parking");
                return new RegisterParkingResponse 
                { 
                    Success = false, 
                    Message = $"Lỗi kết nối: {ex.Message}" 
                };
            }
        }

        public async Task<CheckOutResponse> CheckOutAsync(CheckOutRequest request)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<CheckOutResponse>("CheckOut", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking out");
                return new CheckOutResponse 
                { 
                    Success = false, 
                    Message = $"Lỗi kết nối: {ex.Message}" 
                };
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Connection is managed by SignalRConnectionService
            await Task.CompletedTask;
        }
    }
}