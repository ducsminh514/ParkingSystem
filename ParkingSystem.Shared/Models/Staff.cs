using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace ParkingSystem.Shared.Models;

public partial class Staff
{
    public Guid StaffId { get; set; }

    public string FullName { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Shift { get; set; }

    public virtual ICollection<CustomerReport> CustomerReports { get; set; } = new List<CustomerReport>();

    public virtual ICollection<ParkingRegistration> ParkingRegistrations { get; set; } = new List<ParkingRegistration>();
}
