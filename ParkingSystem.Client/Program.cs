using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ParkingSystem.Client;
using ParkingSystem.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ============================
// HTTP Client
// ============================
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// ============================
// LocalStorage
// ============================
builder.Services.AddBlazoredLocalStorage();

// ============================
// Authentication & Authorization
// ============================
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, SimpleAuthStateProvider>();
builder.Services.AddScoped<SimpleAuthStateProvider>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddSingleton<IStaffManagementService, StaffManagementService>(); 

// ============================
// SignalR Connection - SINGLETON
// ============================
builder.Services.AddSingleton<ISignalRConnectionService, SignalRConnectionService>();

// ============================
// Application Services - SCOPED
// ============================
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<SlotService>();
// ============================
// Build Host
// ============================
var host = builder.Build();

// ============================
// Initialize SignalR Connection
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

// ============================
// Run Application
// ============================
await host.RunAsync();
