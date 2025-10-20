using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingSystem.Shared.DTOs
{
    public class CustomerReportDto
    {
        public Guid ReportID { get; set; }
        public Guid CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public Guid CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public string? AssignedStaffName { get; set; }
        public string? ResolutionNote { get; set; }
        public int? Rating { get; set; }
        public int CommentCount { get; set; }
        public int AttachmentCount { get; set; }
    }
}
