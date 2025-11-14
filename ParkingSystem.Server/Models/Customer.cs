using System;
using System.Collections.Generic;

namespace ParkingSystem.Server.Models;

public partial class Customer
{
    public Guid CustomerId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string PasswordHash { get; set; } = null!;

    public virtual ICollection<CustomerReport> CustomerReports { get; set; } = new List<CustomerReport>();
    public bool IsDeleted { get; set; } = false;
    public bool HasActiveVehicle => Vehicles.Any(v => v.HasActiveParking);

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
