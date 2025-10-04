using Microsoft.AspNetCore.SignalR;
using ParkingSystem.Server.Models;

namespace ParkingSystem.Server.Hubs;

public class ParkingHub : Hub
{
    public async Task UpdateParkingStatus(ParkingRegistration registration)
    {
        await Clients.All.SendAsync("ReceiveParkingUpdate", registration);
    }

    public async Task VehicleCheckin(Vehicle vehicle, string slotId)
    {
        await Clients.All.SendAsync("VehicleCheckedIn", vehicle, slotId);
    }

    public async Task VehicleCheckout(string registrationId)
    {
        await Clients.All.SendAsync("VehicleCheckedOut", registrationId);
    }
}
