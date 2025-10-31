using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace ParkingSystem.Shared.Models;

public partial class Payment
{
    public Guid PaymentId { get; set; }

    public Guid RegistrationId { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime PaymentDate { get; set; }

    [ForeignKey(nameof(RegistrationId))]
    public virtual ParkingRegistration Registration { get; set; } = null!;
}
