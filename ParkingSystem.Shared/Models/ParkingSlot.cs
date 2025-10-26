using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace ParkingSystem.Shared.Models;

public partial class ParkingSlot
{
    public Guid SlotId { get; set; }

    public string SlotCode { get; set; } = null!;

    public string Status { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<ParkingRegistration> ParkingRegistrations { get; set; } = new List<ParkingRegistration>();
}
