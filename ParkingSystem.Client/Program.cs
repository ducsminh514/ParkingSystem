using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ParkingSystem.Client;
using ParkingSystem.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HTTP Client (if needed)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// ============================
// SignalR Connection - SINGLETON
// ============================
builder.Services.AddSingleton<ISignalRConnectionService, SignalRConnectionService>();

// ============================
// Application Services - SCOPED
// ============================
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<CustomerService>();
// Add more services here...
// builder.Services.AddScoped<IVehicleService, VehicleService>();
// builder.Services.AddScoped<IParkingService, ParkingService>();

var host = builder.Build();

// ============================
// Start SignalR Connection
// ============================
var connectionService = host.Services.GetRequiredService<ISignalRConnectionService>();
try
{
    await connectionService.StartAsync();
    Console.WriteLine("✅ SignalR connection established!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to connect to SignalR: {ex.Message}");
}

await host.RunAsync();