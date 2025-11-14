using System.Data.SqlTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using ParkingSystem.Shared.Models;

namespace ParkingSystem.Client.Services
{
    public class CustomerService 
    {
        private readonly ISignalRConnectionService _connectionService;
        private HubConnection? HubConnection => _connectionService.Connection;
        private readonly ILogger<CustomerService> _logger;
        private Func<string, string, string, Task>? _notificationHandler;

        // Events for real-time updates
        public event Func<Customer, Task>? OnCustomerAdded;
        public event Func<Customer, Task>? OnCustomerUpdated;
        public event Func<Guid, Task>? OnCustomerDeleted;
        public event Func<ParkingSystem.Shared.Models.Vehicle, Task>? OnVehicleAdded;
        public event Func<ParkingSystem.Shared.Models.Vehicle, Task>? OnVehicleUpdated;
        public event Func<Guid, Task>? OnVehicleDeleted;

        public bool IsConnected => HubConnection?.State == HubConnectionState.Connected;

        public CustomerService(ISignalRConnectionService connectionService, ILogger<CustomerService> logger)
        {
            _connectionService = connectionService;
            RegisterEventHandlers();
            _logger = logger;
        }

        private void RegisterEventHandlers()
        {
            HubConnection.On<Customer>("CustomerAdded", async (customer) =>
            {
                _logger.LogInformation($"Real-time: Customer added - {customer.FullName}");
                if (OnCustomerAdded != null)
                    await OnCustomerAdded.Invoke(customer);
            });

            HubConnection.On<Customer>("CustomerUpdated", async (customer) =>
            {
                _logger.LogInformation($"Real-time: Customer updated - {customer.FullName}");
                if (OnCustomerUpdated != null)
                    await OnCustomerUpdated.Invoke(customer);
            });

            HubConnection.On<Guid>("CustomerDeleted", async (customerId) =>
            {
                _logger.LogInformation($"Real-time: Customer deleted - {customerId}");
                if (OnCustomerDeleted != null)
                    await OnCustomerDeleted.Invoke(customerId);
            });

            // Reconnection events
            HubConnection.Reconnecting += error =>
            {
                _logger.LogWarning("SignalR is reconnecting...");
                return Task.CompletedTask;
            };
            
            
            HubConnection.On<ParkingSystem.Shared.Models.Vehicle>("VehicleAdded", async (vehicle) =>
            {
                _logger.LogInformation($"Real-time: Vehicle added - {vehicle.PlateNumber}");
                if (OnVehicleAdded != null)
                    await OnVehicleAdded.Invoke(vehicle);
            });

            HubConnection.On<ParkingSystem.Shared.Models.Vehicle>("VehicleUpdated", async (vehicle) =>
            {
                _logger.LogInformation($"Real-time: Vehicle updated - {vehicle.PlateNumber}");
                if (OnVehicleUpdated != null)
                    await OnVehicleUpdated.Invoke(vehicle);
            });

            HubConnection.On<Guid>("VehicleDeleted", async (vehicleId) =>
            {
                _logger.LogInformation($"Real-time: Vehicle deleted - {vehicleId}");
                if (OnVehicleDeleted != null)
                    await OnVehicleDeleted.Invoke(vehicleId);
            }); 
        }

        // ============ CUSTOMER OPERATIONS ============

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            EnsureConnected();
            try
            {
                var customers = await HubConnection!.InvokeAsync<List<Customer>>("GetAllCustomers");
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
                return await HubConnection!.InvokeAsync<Customer?>("GetCustomerById", customerId);
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
                return await HubConnection!.InvokeAsync<Customer>("AddCustomer", customer);
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
                return await HubConnection!.InvokeAsync<Customer>("UpdateCustomer", customer);
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
                await HubConnection!.InvokeAsync("DeleteCustomer", customerId);
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
                var customers = await HubConnection!.InvokeAsync<List<Customer>>("SearchCustomers", keyword);
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
                return await HubConnection!.InvokeAsync<Customer?>("GetCustomerWithFullDetails", customerId);
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
            if (HubConnection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("SignalR is not connected. Please try again.");
            }
        }
        
        public async Task<AuthResult> RegisterCustomer(RegisterRequest request)
        {
            EnsureConnected();
            try
            {
                return await HubConnection!.InvokeAsync<AuthResult>("RegisterCustomer", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering customer");
                throw;
            }
        }

        public async Task<AuthResult> Login(LoginRequest request)
        {
            EnsureConnected();
            try
            {
                return await HubConnection!.InvokeAsync<AuthResult>("Login", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (HubConnection is not null)
            {
                await HubConnection.DisposeAsync();
            }
        }
        
        public async Task<List<ParkingSystem.Shared.Models.Vehicle>> GetMyVehicles(Guid customerId)
        {
            EnsureConnected();
            try
            {
                return await HubConnection!.InvokeAsync<List<ParkingSystem.Shared.Models.Vehicle>>("GetMyVehicles", customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicles");
                throw;
            }
        }

        // Add vehicle
        public async Task<ParkingSystem.Shared.Models.Vehicle> AddVehicle(Guid customerId, string plateNumber, string vehicleType)
        {
            EnsureConnected();
            try
            {
                return await HubConnection!.InvokeAsync<ParkingSystem.Shared.Models.Vehicle>("AddVehicle", customerId, plateNumber, vehicleType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding vehicle");
                throw;
            }
        }

        // Update vehicle
        public async Task<ParkingSystem.Shared.Models.Vehicle> UpdateVehicle(Guid vehicleId, string plateNumber, string vehicleType)
        {
            EnsureConnected();
            try
            {
                return await HubConnection!.InvokeAsync<ParkingSystem.Shared.Models.Vehicle>("UpdateVehicle", vehicleId, plateNumber, vehicleType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vehicle");
                throw;
            }
        }

        // Delete vehicle
        public async Task DeleteVehicle(Guid vehicleId)
        {
            EnsureConnected();
            try
            {
                await HubConnection!.InvokeAsync("DeleteVehicle", vehicleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vehicle");
                throw;
            }
        }

        // Get vehicle details
        public async Task<ParkingSystem.Shared.Models.Vehicle?> GetVehicleDetails(Guid vehicleId)
        {
            EnsureConnected();
            try
            {
                return await HubConnection!.InvokeAsync<ParkingSystem.Shared.Models.Vehicle?>("GetVehicleDetails", vehicleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle details");
                throw;
            }
        }
        public async Task RegisterNotificationHandler(Func<string, string, string, Task> handler)
        {
            _notificationHandler = handler;

            // Register SignalR event
            if (HubConnection != null)
            {
                HubConnection.On<dynamic>("ReceiveNotification", async (notification) =>
                {
                    string title = notification.GetProperty("Title").GetString() ?? "Notification";
                    string message = notification.GetProperty("Message").GetString() ?? "";
                    string type = notification.GetProperty("Type").GetString() ?? "info";

                    if (_notificationHandler != null)
                    {
                        await _notificationHandler.Invoke(title, message, type);
                    }
                });
            }
        }
        
        
    }
}