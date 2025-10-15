using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace ParkingSystem.Shared.Models;

public partial class Customer
{
    public Guid CustomerId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string PasswordHash { get; set; } = null!;

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    
}
