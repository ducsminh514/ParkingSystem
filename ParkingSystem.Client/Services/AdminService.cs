using Microsoft.AspNetCore.SignalR.Client;
using ParkingSystem.Shared.Models;
using ParkingSystem.Shared.DTOs;
using Microsoft.Extensions.Logging;
using ParkingSystem.Shared.DTOs;

namespace ParkingSystem.Client.Services
{
    public class AdminService : IAsyncDisposable
    {
        private readonly ILogger<AdminService> _logger;
        private readonly ISignalRConnectionService _signalRConnectionService;
        private HubConnection Connection => _signalRConnectionService.Connection;

        public bool IsConnected => Connection?.State == HubConnectionState.Connected;
        public event Action<Guid>? OnPriceDeleted;
        public event Action<ParkingPriceDto>? OnPriceUpdated;
        public AdminService(ISignalRConnectionService signalRConnectionService, ILogger<AdminService> logger)
        {
            _signalRConnectionService = signalRConnectionService;
            _logger = logger;

            // Register real-time event listeners
            RegisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            Connection.On<ParkingPriceDto>("OnPriceUpdated", (price) =>
            {
                _logger.LogInformation($"[Real-time] Slot updated");
                OnPriceUpdated?.Invoke(price);
            });

            Connection.On<Guid>("OnPriceDeleted", (priceId) =>
            {
                _logger.LogInformation($"[Real-time] Slot updated");
                OnPriceDeleted?.Invoke(priceId);
            });
        }

        // ============ HELPER METHODS ============

        private void EnsureConnected()
        {
            if (Connection?.State != HubConnectionState.Connected)
            {
                _logger.LogError("SignalR not connected. State: {State}", Connection?.State);
                throw new InvalidOperationException($"SignalR not connected. Current state: {Connection?.State}. Please check the server connection and try again.");
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

        // ============ ADMIN PRICING MANAGEMENT ============

        /// <summary>
        /// Get all prices (including inactive) - Admin only
        /// </summary>
        public async Task<List<ParkingPriceDetailDto>> GetAllPricesAdmin()
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<List<ParkingPriceDetailDto>>("GetAllPricesAdmin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all prices for admin");
                throw;
            }
        }

        /// <summary>
        /// Create or update price - Admin only
        /// </summary>
        public async Task<UpsertPriceResponse> UpsertPrice(UpsertPriceRequest request)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<UpsertPriceResponse>("UpsertPrice", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting price");
                return new UpsertPriceResponse
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Delete price (soft delete) - Admin only
        /// </summary>
        public async Task<DeletePriceResponse> DeletePrice(Guid priceId)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<DeletePriceResponse>("DeletePrice", priceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting price");
                return new DeletePriceResponse
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Toggle active/inactive status - Admin only
        /// </summary>
        public async Task<UpsertPriceResponse> TogglePriceStatus(Guid priceId)
        {
            EnsureConnected();
            try
            {
                return await Connection.InvokeAsync<UpsertPriceResponse>("TogglePriceStatus", priceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling price status");
                return new UpsertPriceResponse
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