using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingSystem.Shared.DTOs
{
    public class RateReportRequest
    {
        public Guid ReportID { get; set; }
        public int Rating { get; set; } // 1-5
    }
}
