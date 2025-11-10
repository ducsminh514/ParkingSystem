
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;
using ParkingSystem.Shared.Models;

namespace ParkingSystem.Server.Hubs
{
    public partial class ParkingHub : Hub
    {
        private readonly ParkingManagementContext _context;
        private readonly ILogger<ParkingHub> _logger;

        public ParkingHub(ParkingManagementContext context, ILogger<ParkingHub> logger)
        {
            _context = context;
            _logger = logger;
        }
    }
}
