using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace ParkingSystem.Shared.Models;

public partial class Vehicle
{
    public Guid VehicleId { get; set; }

    public string PlateNumber { get; set; } = null!;

    public string? VehicleType { get; set; }

    public Guid CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<ParkingRegistration> ParkingRegistrations { get; set; } = new List<ParkingRegistration>();
}
