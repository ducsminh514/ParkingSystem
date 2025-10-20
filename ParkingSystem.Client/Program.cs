using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ParkingSystem.Client;
using ParkingSystem.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// ✨ Thêm LocalStorage
builder.Services.AddBlazoredLocalStorage();

// ✨ Thêm Authorization
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, SimpleAuthStateProvider>();
builder.Services.AddScoped<SimpleAuthStateProvider>();
builder.Services.AddScoped<ParkingService>();
// Program.cs (Server)
//builder.Services.AddCors(options =>
//{
//    options.AddDefaultPolicy(policy =>
//    {
//        policy.SetIsOriginAllowed(origin => true) // CHỈ dùng cho development
//            .AllowAnyHeader()
//            .AllowAnyMethod()
//            .AllowCredentials();
//    });
//});
await builder.Build().RunAsync();
