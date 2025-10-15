﻿using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Components;
using ParkingSystem.Server.Hubs;
using ParkingSystem.Server.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContext<ParkingManagementContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyParkingManagementContext"));
});
// Add SignalR
builder.Services.AddSignalR();
// Program.cs
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
// Sau khi build
app.UseCors("AllowBlazor");
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles(); // Remove the duplicate later

app.UseRouting(); // Must come before UseAntiforgery

app.UseAntiforgery(); // Must come after UseRouting

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub
app.MapHub<ParkingHub>("/parkinghub");


app.Run();