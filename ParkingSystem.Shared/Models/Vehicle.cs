using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace ParkingSystem.Shared.Models;
using System.ComponentModel.DataAnnotations;

public partial class Vehicle
{
    public Guid VehicleId { get; set; }


    [Required(ErrorMessage = "Biển số xe là bắt buộc.")]
    [RegularExpression(@"^\d{2}[A-Za-z]{1,2}[-\s]?(?:\d{5}|\d{3}(?:[.\s]?\d{2}))$",
        ErrorMessage = "Biển số không hợp lệ. Ví dụ hợp lệ: `30A-12345`, `30A-123.45`")]
    [StringLength(15, ErrorMessage = "Biển số quá dài.")]
    public string PlateNumber { get; set; } = null!;

    public string? VehicleType { get; set; }

    public Guid CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<ParkingRegistration> ParkingRegistrations { get; set; } = new List<ParkingRegistration>();
}
