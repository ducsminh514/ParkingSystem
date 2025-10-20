using System;
using System.Collections.Generic;

namespace ParkingSystem.Server.Models;

public partial class ReportComment
{
    public Guid CommentId { get; set; }

    public Guid ReportId { get; set; }

    public Guid UserId { get; set; }

    public string UserType { get; set; } = null!;

    public string Comment { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public bool IsInternal { get; set; }

    public virtual CustomerReport Report { get; set; } = null!;
}
