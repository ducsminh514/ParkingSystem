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
        }

        // ============ PARKING SLOT OPERATIONS ============

        public async Task<List<ParkingSlotDto>> GetAllSlotsAsync()
        {
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

        public async Task<List<ParkingAreaDto>> GetSlotsByAreaAsync()
        {
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

        public async Task<List<ParkingSlotDto>> GetAvailableSlotsAsync()
        {
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

        // ============ PARKING REGISTRATION OPERATIONS ============

        public async Task<RegisterParkingResponse> RegisterParkingAsync(RegisterParkingRequest request)
        {
            try
            {
                return await Connection.InvokeAsync<RegisterParkingResponse>("RegisterParking", request);
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