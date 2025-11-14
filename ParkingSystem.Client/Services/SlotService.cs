using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using ParkingSystem.Shared.DTOs;
using ParkingSystem.Shared.Models;

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
        public event Action<SlotManagementDto>? OnSlotCreated;
        public event Action<List<SlotManagementDto>>? OnSlotsBulkCreated;
        public event Action<SlotManagementDto>? OnAdminSlotUpdated;
        public event Action<Guid>? OnSlotDeleted;
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
            Connection.On<SlotManagementDto>("OnSlotCreated", (newSlot) =>
            {
                _logger.LogInformation($"[Real-time] Slot created: {newSlot.SlotCode}");
                OnSlotCreated?.Invoke(newSlot);
            });

            // Listen to bulk slot creation
            Connection.On<List<SlotManagementDto>>("OnSlotsBulkCreated", (newSlots) =>
            {
                _logger.LogInformation($"[Real-time] {newSlots.Count} slots bulk created.");
                OnSlotsBulkCreated?.Invoke(newSlots);
            });

            // Listen to admin slot updates
            Connection.On<SlotManagementDto>("OnAdminSlotUpdated", (updatedSlot) =>
            {
                _logger.LogInformation($"[Real-time] Admin slot updated: {updatedSlot.SlotCode}");
                OnAdminSlotUpdated?.Invoke(updatedSlot);
            });

            // Listen to slot deletion
            Connection.On<Guid>("OnSlotDeleted", (slotId) =>
            {
                _logger.LogInformation($"[Real-time] Slot deleted: {slotId}");
                OnSlotDeleted?.Invoke(slotId);
            });
        }

        // ============ HELPER METHODS ============

        private void EnsureConnected()
        {
            if (Connection?.State != HubConnectionState.Connected)
            {
                _logger.LogError("SignalR not connected. State: {State}", Connection?.State);
                throw new InvalidOperationException($"SignalR not connected. Current state: {Connection?.State}. Please check server connection and try again.");
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


        // ============ ADMIN SLOT MANAGEMENT ============

        /// <summary>
        /// Get slots grouped by area - Admin only
        /// </summary>
        public async Task<List<ParkingAreaGroupDto>> GetSlotsGroupedByArea()
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<List<ParkingAreaGroupDto>>("GetSlotsGroupedByArea");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slots grouped by area");
                throw;
            }
        }

        /// <summary>
        /// Get slots statistics - Admin only
        /// </summary>
        public async Task<SlotsStatisticsDto> GetSlotsStatistics()
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<SlotsStatisticsDto>("GetSlotsStatistics");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slots statistics");
                throw;
            }
        }

        /// <summary>
        /// Create new slot - Admin only
        /// </summary>
        public async Task<SlotOperationResponse> CreateSlot(CreateSlotRequest request)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<SlotOperationResponse>("CreateSlot", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating slot");
                return new SlotOperationResponse
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Bulk create slots - Admin only
        /// </summary>
        public async Task<SlotOperationResponse> BulkCreateSlots(BulkCreateSlotsRequest request)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<SlotOperationResponse>("BulkCreateSlots", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating slots");
                return new SlotOperationResponse
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Update slot - Admin only
        /// </summary>
        public async Task<SlotOperationResponse> UpdateSlot(UpdateSlotRequest request)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<SlotOperationResponse>("UpdateSlot", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating slot");
                return new SlotOperationResponse
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Delete slot - Admin only
        /// </summary>
        public async Task<DeleteSlotResponse> DeleteSlot(Guid slotId, bool forceDelete = false)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<DeleteSlotResponse>("DeleteSlot", slotId, forceDelete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting slot");
                return new DeleteSlotResponse
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Bulk delete slots - Admin only
        /// </summary>
        public async Task<DeleteSlotResponse> BulkDeleteSlots(BulkDeleteSlotsRequest request)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<DeleteSlotResponse>("BulkDeleteSlots", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting slots");
                return new DeleteSlotResponse
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}"
                };
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
                    Message = $"Error loading history: {ex.Message}",
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
        /// Get slots by area for STAFF - with full customer details
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
        /// Get slots by area for CUSTOMER - show details ONLY for the customer's slots
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
        /// Get the list of actively parked vehicles of a customer
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
        /// Check customer by phone number (Staff)
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
        /// Staff registers parking (can create new customer/vehicle)
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
                    Message = $"Connection error: {ex.Message}"
                };
            }
        }
        /// <summary>
        /// Get parking prices
        /// </summary>
        public async Task<List<ParkingPriceDto>> GetParkingPrices()
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<List<ParkingPriceDto>>("GetParkingPrices");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parking prices");
                throw;
            }
        }

        /// <summary>
        /// Calculate parking fee prior to check-out
        /// </summary>
        public async Task<CalculateFeeResponse> CalculateParkingFee(Guid registrationId)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<CalculateFeeResponse>("CalculateParkingFee", registrationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating fee");
                return new CalculateFeeResponse
                {
                    Success = false,
                    Message = $"Fee calculation error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Check-out with payment
        /// </summary>
        public async Task<CheckOutWithPaymentResponse> CheckOutWithPayment(CheckOutWithPaymentRequest request)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<CheckOutWithPaymentResponse>("CheckOutWithPayment", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in check-out with payment");
                return new CheckOutWithPaymentResponse
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}"
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
                    Message = $"Connection error: {ex.Message}"
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
                    Message = $"Connection error: {ex.Message}"
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