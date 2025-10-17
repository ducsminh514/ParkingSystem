using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ParkingSystem.Client;
using ParkingSystem.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


// HttpClient cho API calls - trỏ đến Server
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7142/") });

// ParkingService
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
