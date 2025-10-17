using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Components;
using ParkingSystem.Server.Hubs;
using ParkingSystem.Server.Models;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Controllers for API
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = false;
    });

builder.Services.AddDbContext<ParkingManagementContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("ParkingManagementContext"));
});
// Add SignalR
builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = true;
    })
    .AddJsonProtocol(options =>
    {
        // Xử lý circular reference
        options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        // Hoặc dùng: ReferenceHandler.Preserve (giữ lại references)

        options.PayloadSerializerOptions.WriteIndented = false;
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("https://localhost:7068","http://localhost:5185") // URL của Blazor client
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Quan trọng cho SignalR
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// CORS must be before UseRouting
app.UseCors("AllowBlazor");

app.UseStaticFiles();

app.UseRouting();

// API Controllers - no antiforgery required
app.MapControllers();

// SignalR hub
app.MapHub<ParkingHub>("/parkinghub");

// Antiforgery for Razor Components only
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();