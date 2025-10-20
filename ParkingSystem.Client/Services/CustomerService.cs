using System.Data.SqlTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using ParkingSystem.Shared.Models;

namespace ParkingSystem.Client.Services
{
    public class CustomerService : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        private readonly HttpClient _httpClient;
        private readonly ILogger<CustomerService> _logger;

        // Events for real-time updates
        public event Func<Customer, Task>? OnCustomerAdded;
        public event Func<Customer, Task>? OnCustomerUpdated;
        public event Func<Guid, Task>? OnCustomerDeleted;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public CustomerService(HttpClient httpClient, ILogger<CustomerService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // ============ INITIALIZE CONNECTION ============
        public async Task InitializeAsync()
        {
            try
            {
                // Sử dụng HttpClient.BaseAddress thay vì NavigationManager
                var hubUrl = "https://localhost:7142/parkinghub";
                _logger.LogInformation($"Connecting to SignalR hub at: {hubUrl}");


                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) })
                    .Build();

                // Register event handlers for real-time updates
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

                // Reconnection events
                _hubConnection.Reconnecting += error =>
                {
                    _logger.LogWarning("SignalR đang kết nối lại...");
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += connectionId =>
                {
                    _logger.LogInformation($"SignalR đã kết nối lại: {connectionId}");
                    return Task.CompletedTask;
                };

                _hubConnection.Closed += async error =>
                {
                    _logger.LogError(error, "SignalR connection closed");
                    await Task.Delay(Random.Shared.Next(0, 5) * 1000);

                    // Kiểm tra trạng thái trước khi reconnect
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

        // ============ HELPER METHODS ============

        private void EnsureConnected()
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("SignalR chưa kết nối. Vui lòng thử lại.");
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