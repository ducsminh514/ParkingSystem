using System;
using System.Collections.Generic;

namespace ParkingSystem.Shared.Models;

public partial class ReportCategory
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public virtual ICollection<CustomerReport> CustomerReports { get; set; } = new List<CustomerReport>();
}
