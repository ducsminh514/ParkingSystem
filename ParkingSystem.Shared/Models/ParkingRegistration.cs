using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace ParkingSystem.Shared.Models;

public partial class ParkingRegistration
{
    public Guid RegistrationId { get; set; }

    public Guid VehicleId { get; set; }

    public Guid SlotId { get; set; }

    public Guid? StaffId { get; set; }

    public DateTime CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [ForeignKey(nameof(SlotId))]
    public virtual ParkingSlot? Slot { get; set; } = null!;

    [ForeignKey(nameof(StaffId))]
    public virtual Staff? Staff { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public virtual Vehicle Vehicle { get; set; } = null!;
}
