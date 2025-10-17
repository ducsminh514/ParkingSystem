using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using ParkingSystem.Shared.Models;
using Microsoft.Extensions.Logging;
namespace ParkingSystem.Client.Services
{
    public class ParkingService : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ParkingService> _logger;

        // Events for real-time updates
        public event Func<Customer, Task>? OnCustomerAdded;
        public event Func<Customer, Task>? OnCustomerUpdated;
        public event Func<Guid, Task>? OnCustomerDeleted;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public ParkingService(HttpClient httpClient, ILogger<ParkingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // ============ INITIALIZE CONNECTION ============
        public async Task InitializeAsync()
        {
            try
            {
                var hubUrl = "https://localhost:7142/parkinghub";
                _logger.LogInformation($"Connecting to SignalR hub at: {hubUrl}");

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) })
                    .Build();

                _hubConnection.On<Customer>("CustomerAdded", async (customer) =>
                {
                    _logger.LogInformation($"Real-time: Customer added - {customer.FullName}");
                    if (OnCustomerAdded != null)
                        await OnCustomerAdded.Invoke(customer);
                });

                _hubConnection.On<Customer>("CustomerUpdated", async (customer) =>
                {
                    _logger.LogInformation($"Real-time: Customer updated - {customer.FullName}");
                    if (OnCustomerUpdated != null)
                        await OnCustomerUpdated.Invoke(customer);
                });

                _hubConnection.On<Guid>("CustomerDeleted", async (customerId) =>
                {
                    _logger.LogInformation($"Real-time: Customer deleted - {customerId}");
                    if (OnCustomerDeleted != null)
                        await OnCustomerDeleted.Invoke(customerId);
                });

                _hubConnection.Reconnecting += error =>
                {
                    _logger.LogWarning("SignalR reconnecting...");
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += connectionId =>
                {
                    _logger.LogInformation($"SignalR reconnected: {connectionId}");
                    return Task.CompletedTask;
                };

                _hubConnection.Closed += async error =>
                {
                    _logger.LogError(error, "SignalR connection closed");
                    await Task.Delay(Random.Shared.Next(0, 5) * 1000);

                    if (_hubConnection.State == HubConnectionState.Disconnected)
                    {
                        try
                        {
                            await _hubConnection.StartAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to reconnect");
                        }
                    }
                };

                await _hubConnection.StartAsync();
                _logger.LogInformation("SignalR connected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing SignalR connection");
                throw;
            }
        }

        // ============ CUSTOMER OPERATIONS ============

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            EnsureConnected();
            try
            {
                var customers = await _hubConnection!.InvokeAsync<List<Customer>>("GetAllCustomers");
                return customers ?? new List<Customer>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                throw;
            }
        }

        public async Task<Customer?> GetCustomerByIdAsync(Guid customerId)
        {
            EnsureConnected();
            try
            {
                return await _hubConnection!.InvokeAsync<Customer?>("GetCustomerById", customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting customer {customerId}");
                throw;
            }
        }

        public async Task<Customer> AddCustomerAsync(Customer customer)
        {
            EnsureConnected();
            try
            {
                return await _hubConnection!.InvokeAsync<Customer>("AddCustomer", customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding customer");
                throw;
            }
        }

        public async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            EnsureConnected();
            try
            {
                return await _hubConnection!.InvokeAsync<Customer>("UpdateCustomer", customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer");
                throw;
            }
        }

        public async Task DeleteCustomerAsync(Guid customerId)
        {
            EnsureConnected();
            try
            {
                await _hubConnection!.InvokeAsync("DeleteCustomer", customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer");
                throw;
            }
        }

        public async Task<List<Customer>> SearchCustomersAsync(string keyword)
        {
            EnsureConnected();
            try
            {
                var customers = await _hubConnection!.InvokeAsync<List<Customer>>("SearchCustomers", keyword);
                return customers ?? new List<Customer>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers");
                throw;
            }
        }

        public async Task<Customer?> GetCustomerWithFullDetailsAsync(Guid customerId)
        {
            EnsureConnected();
            try
            {
                return await _hubConnection!.InvokeAsync<Customer?>("GetCustomerWithFullDetails", customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer details");
                throw;
            }
        }

        // ============ PARKING SLOT OPERATIONS ============

        public async Task<List<ParkingSlotDto>> GetAllSlotsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ParkingSlotDto>>("api/ParkingSlot");
                return response ?? new List<ParkingSlotDto>();
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
                var response = await _httpClient.GetFromJsonAsync<List<ParkingAreaDto>>("api/ParkingSlot/by-area");
                return response ?? new List<ParkingAreaDto>();
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
                return await _httpClient.GetFromJsonAsync<ParkingOverviewDto>("api/ParkingSlot/overview");
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
                var response = await _httpClient.GetFromJsonAsync<List<ParkingSlotDto>>("api/ParkingSlot/available");
                return response ?? new List<ParkingSlotDto>();
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
                return await _httpClient.GetFromJsonAsync<ParkingSlotDto>($"api/ParkingSlot/{slotId}");
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
                var response = await _httpClient.PostAsJsonAsync("api/ParkingSlot/register", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RegisterParkingResponse>();
                    return result ?? new RegisterParkingResponse 
                    { 
                        Success = false, 
                        Message = "Không nhận được phản hồi từ server" 
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error registering parking: {response.StatusCode} - {errorContent}");
                    
                    return new RegisterParkingResponse 
                    { 
                        Success = false, 
                        Message = $"Lỗi: {response.StatusCode}" 
                    };
                }
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
                var response = await _httpClient.PostAsJsonAsync("api/ParkingSlot/checkout", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CheckOutResponse>();
                    return result ?? new CheckOutResponse 
                    { 
                        Success = false, 
                        Message = "Không nhận được phản hồi từ server" 
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error checking out: {response.StatusCode} - {errorContent}");
                    
                    return new CheckOutResponse 
                    { 
                        Success = false, 
                        Message = $"Lỗi: {response.StatusCode}" 
                    };
                }
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

        // ============ HELPER METHODS ============

        private void EnsureConnected()
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("SignalR not connected. Please try again.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}