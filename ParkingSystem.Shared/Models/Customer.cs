using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Shared.Models;

public partial class Customer
{
    public Guid CustomerId { get; set; }

    public string FullName { get; set; } = null!;
    
    
    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(@"^0\d{8,9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 9–10 chữ số.")]
    public string Phone { get; set; } = null!;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(254, ErrorMessage = "Email quá dài.")]
    public string? Email { get; set; }

    public string PasswordHash { get; set; } = null!;

    public virtual ICollection<CustomerReport> CustomerReports { get; set; } = new List<CustomerReport>();
    public bool IsDeleted { get; set; } = false;
    public bool HasActiveVehicle => Vehicles.Any(v => v.HasActiveParking);
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
