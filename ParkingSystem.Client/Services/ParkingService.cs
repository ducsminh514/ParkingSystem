using Microsoft.AspNetCore.SignalR.Client;
using ParkingSystem.Shared.Models;

namespace ParkingSystem.Client.Services;

public class ParkingService : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;

    public ParkingService(NavigationManager navigationManager)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/parkinghub"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<ParkingRegistration>("ReceiveParkingUpdate", (registration) =>
        {
            OnParkingStatusUpdated?.Invoke(registration);
        });

        _hubConnection.On<Vehicle, string>("VehicleCheckedIn", (vehicle, slotId) =>
        {
            OnVehicleCheckedIn?.Invoke(vehicle, slotId);
        });

        _hubConnection.On<string>("VehicleCheckedOut", (registrationId) =>
        {
            OnVehicleCheckedOut?.Invoke(registrationId);
        });
    }

    public event Action<ParkingRegistration>? OnParkingStatusUpdated;
    public event Action<Vehicle, string>? OnVehicleCheckedIn;
    public event Action<string>? OnVehicleCheckedOut;

    public async Task StartConnection()
    {
        await _hubConnection.StartAsync();
    }

    public async Task CheckinVehicle(Vehicle vehicle, string slotId)
    {
        await _hubConnection.SendAsync("VehicleCheckin", vehicle, slotId);
    }

    public async Task CheckoutVehicle(string registrationId)
    {
        await _hubConnection.SendAsync("VehicleCheckout", registrationId);
    }

    public async Task UpdateParking(ParkingRegistration registration)
    {
        await _hubConnection.SendAsync("UpdateParkingStatus", registration);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
