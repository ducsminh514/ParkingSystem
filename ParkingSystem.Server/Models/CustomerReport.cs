using System;
using System.Collections.Generic;

namespace ParkingSystem.Server.Models;

public partial class CustomerReport
{
    public Guid ReportId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? ResolvedDate { get; set; }

    public Guid? AssignedStaffId { get; set; }

    public string? ResolutionNote { get; set; }

    public int? Rating { get; set; }

    public virtual Staff? AssignedStaff { get; set; }

    public virtual ReportCategory Category { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<ReportAttachment> ReportAttachments { get; set; } = new List<ReportAttachment>();

    public virtual ICollection<ReportComment> ReportComments { get; set; } = new List<ReportComment>();
}
