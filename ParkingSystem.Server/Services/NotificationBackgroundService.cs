using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ParkingSystem.Server.Hubs;
using ParkingSystem.Server.Models;

namespace ParkingSystem.Server.Services;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly IHubContext<ParkingHub> _hubContext;

    public NotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NotificationBackgroundService> logger,
        IHubContext<ParkingHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Background Service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckExpiringRegistrations();
                
                    // Wait for 30 minutes or until cancellation is requested
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown
                    _logger.LogInformation("Notification Background Service is stopping due to application shutdown");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Notification Background Service");
                
                    // Wait before retrying, but respect cancellation
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                    catch (TaskCanceledException)
                    {
                        // Exit the loop if cancellation is requested during the delay
                        break;
                    }
                }
            }
        }
        finally
        {
            _logger.LogInformation("Notification Background Service has stopped");
        }
    }

    private async Task CheckExpiringRegistrations()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ParkingManagementContext>();

        var now = DateTime.Now;
        var nearExpiry = now.AddHours(2); // Thông báo trước 2 tiếng

        // Tìm xe sắp hết hạn (còn dưới 2h)
        var expiringRegistrations = await context.ParkingRegistrations
            .Include(pr => pr.Vehicle)
                .ThenInclude(v => v.Customer)
            .Include(pr => pr.Slot)
            .Where(pr => pr.Status == "InUse" 
                      && pr.CheckOutTime == null
                      && pr.CheckInTime.AddDays(1) <= nearExpiry
                      && pr.CheckInTime.AddDays(1) >= now)
            .ToListAsync();

        foreach (var registration in expiringRegistrations)
        {
            var timeRemaining = registration.CheckInTime.AddDays(1) - now;
            var hours = (int)timeRemaining.TotalHours;
            var minutes = (int)timeRemaining.TotalMinutes % 60;

            var message = new
            {
                Title = "⏰ Sắp hết thời gian đỗ xe",
                Message = $"Xe {registration.Vehicle.PlateNumber} đang đỗ tại chỗ {registration.Slot.SlotCode} còn {hours}h {minutes}ph. Vui lòng di chuyển xe hoặc gia hạn.",
                Type = "warning",
                Timestamp = DateTime.Now
            };

            // Gửi thông báo real-time tới customer
            await _hubContext.Clients
                .User(registration.Vehicle.CustomerId.ToString())
                .SendAsync("ReceiveNotification", message);

            _logger.LogInformation("Sent expiry notification to customer {CustomerId} for vehicle {PlateNumber}",
                registration.Vehicle.CustomerId, registration.Vehicle.PlateNumber);
        }

        if (expiringRegistrations.Any())
        {
            _logger.LogInformation("Sent {Count} expiry notifications", expiringRegistrations.Count);
        }
    }
}