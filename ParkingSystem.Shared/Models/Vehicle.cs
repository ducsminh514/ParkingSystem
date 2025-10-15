using System;
using System.Collections.Generic;

namespace ParkingSystem.Shared.Models;

public partial class Vehicle
{
    public Guid VehicleId { get; set; }

    public string PlateNumber { get; set; } = null!;

    public string? VehicleType { get; set; }

    public Guid CustomerId { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<ParkingRegistration> ParkingRegistrations { get; set; } = new List<ParkingRegistration>();
}
