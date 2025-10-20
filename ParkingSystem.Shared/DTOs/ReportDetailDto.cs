using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingSystem.Shared.DTOs
{
    public class ReportDetailDto
    {
        public CustomerReportDto Report { get; set; } = null!;
        public List<ReportCommentDto> Comments { get; set; } = new();
        public List<ReportAttachmentDto> Attachments { get; set; } = new();
    }
}
