using Microsoft.AspNetCore.SignalR.Client;

namespace ParkingSystem.Client.Services
{
    public interface ISignalRConnectionService
    {
        HubConnection Connection { get; }
        Task StartAsync();
        Task StopAsync();
        bool IsConnected { get; }
        event Func<Exception?, Task>? Reconnecting;
        event Func<string?, Task>? Reconnected;
        event Func<Exception?, Task>? Closed;
    }

    public class SignalRConnectionService : ISignalRConnectionService
    {
        private readonly HubConnection _connection;

        public HubConnection Connection => _connection;
        public bool IsConnected => _connection.State == HubConnectionState.Connected;

        public event Func<Exception?, Task>? Reconnecting;
        public event Func<string?, Task>? Reconnected;
        public event Func<Exception?, Task>? Closed;

        public SignalRConnectionService()
        {
            // Configure connection
            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7142/parkinghub") // Thay bằng URL server của bạn
                .WithAutomaticReconnect() // Tự động reconnect khi mất kết nối
                .Build();

            // Setup event handlers
            _connection.Reconnecting += error =>
            {
                Console.WriteLine($"Connection lost. Reconnecting... Error: {error?.Message}");
                return Reconnecting?.Invoke(error) ?? Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                Console.WriteLine($"Reconnected. ConnectionId: {connectionId}");
                return Reconnected?.Invoke(connectionId) ?? Task.CompletedTask;
            };

            _connection.Closed += error =>
            {
                Console.WriteLine($"Connection closed. Error: {error?.Message}");
                return Closed?.Invoke(error) ?? Task.CompletedTask;
            };
        }

        public async Task StartAsync()
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _connection.StartAsync();
                    Console.WriteLine("SignalR Connected!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting to SignalR: {ex.Message}");
                    throw;
                }
            }
        }

        public async Task StopAsync()
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.StopAsync();
                Console.WriteLine("SignalR Disconnected!");
            }
        }
    }
}