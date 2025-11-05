using System;
using System.Collections.Generic;

namespace ParkingSystem.Shared.Models;

public partial class ReportAttachment
{
    public Guid AttachmentId { get; set; }

    public Guid ReportId { get; set; }

    public string FileName { get; set; } = null!;

    public string FileUrl { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public long FileSize { get; set; }

    public DateTime UploadedDate { get; set; }

    public virtual CustomerReport Report { get; set; } = null!;
}
