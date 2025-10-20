using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingSystem.Shared.DTOs
{
    public class ReportStatisticsDto
    {
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int InProgressReports { get; set; }
        public int ResolvedReports { get; set; }
        public int RejectedReports { get; set; }
        public double AverageResolutionTime { get; set; } // in hours
        public double AverageRating { get; set; }
    }
}
