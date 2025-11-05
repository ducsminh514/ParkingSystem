using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingSystem.Shared.DTOs
{
    public class ReportCommentDto
    {
        public Guid CommentID { get; set; }
        public Guid ReportID { get; set; }
        public Guid UserID { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsInternal { get; set; }
    }
}
