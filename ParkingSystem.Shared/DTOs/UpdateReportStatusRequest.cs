using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingSystem.Shared.DTOs
{
    public class UpdateReportStatusRequest
    {
        public Guid ReportID { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? AssignedStaffID { get; set; }
        public string? ResolutionNote { get; set; }
    }
}
