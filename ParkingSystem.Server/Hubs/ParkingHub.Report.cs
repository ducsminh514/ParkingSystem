using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;

namespace ParkingSystem.Server.Hubs
{
    public partial class ParkingHub
    {
        // ============================
        // Get Report Categories
        // ============================
        public async Task<List<ReportCategory>> GetReportCategories()
        {
            try
            {
                return await _context.ReportCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new HubException($"Error getting categories: {ex.Message}");
            }
        }

        // ============================
        // Create Report
        // ============================
        public async Task<CustomerReport> CreateReport(CustomerReport report)
        {
            try
            {
                report.ReportId = Guid.NewGuid();
                report.Status = "Pending";
                report.CreatedDate = DateTime.Now;

                try
                {
                    _context.CustomerReports.Add(report);
                    await _context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Loiix choox nayf :   "+e);
                }
                // Load related data
                var createdReport = await _context.CustomerReports
                    .Include(r => r.Customer)
                    .Include(r => r.Category)
                    .FirstAsync(r => r.ReportId == report.ReportId);

                // Broadcast to staff
                await Clients.Group("Staff").SendAsync("OnNewReportCreated", createdReport);

                return createdReport;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error creating report: {ex.Message}");
            }
        }

        // ============================
        // Get Customer's Reports
        // ============================
        public async Task<List<CustomerReport>> GetMyReports(Guid customerId)
        {
            try
            {
                return await _context.CustomerReports
                    .Include(r => r.Customer)
                    .Include(r => r.Category)
                    .Include(r => r.AssignedStaff)
                    .Include(r => r.ReportComments)
                    .Where(r => r.CustomerId == customerId)
                    .OrderByDescending(r => r.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new HubException($"Error getting reports: {ex.Message}");
            }
        }

        // ============================
        // Get Report Detail
        // ============================
        public async Task<CustomerReport> GetReportDetail(Guid reportId)
        {
            try
            {
                var report = await _context.CustomerReports
                    .Include(r => r.Customer)
                    .Include(r => r.Category)
                    .Include(r => r.AssignedStaff)
                    .Include(r => r.ReportComments)
                    .Include(r => r.ReportAttachments)
                    .FirstOrDefaultAsync(r => r.ReportId == reportId);

                if (report == null)
                    throw new HubException("Report not found");

                return report;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error getting report detail: {ex.Message}");
            }
        }

        // ============================
        // Add Comment to Report
        // ============================
        public async Task<ReportComment> AddReportComment(ReportComment comment)
        {
            try
            {
                comment.CommentId = Guid.NewGuid();
                comment.CreatedDate = DateTime.Now;

                _context.ReportComments.Add(comment);

                // Update report's UpdatedDate
                var report = await _context.CustomerReports.FindAsync(comment.ReportId);
                if (report != null)
                {
                    report.UpdatedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // Real-time broadcast to report watchers
                await Clients.Group($"Report_{comment.ReportId}")
                    .SendAsync("OnNewCommentAdded", comment);

                return comment;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error adding comment: {ex.Message}");
            }
        }

        // ============================
        // Update Report Status (Staff only)
        // ============================
        public async Task<bool> UpdateReportStatus(Guid reportId, string status, Guid? staffId = null, string resolutionNote = null)
        {
            try
            {
                var report = await _context.CustomerReports.FindAsync(reportId);
                if (report == null)
                    return false;

                report.Status = status;
                report.UpdatedDate = DateTime.Now;

                if (staffId.HasValue)
                    report.AssignedStaffId = staffId;

                if (!string.IsNullOrEmpty(resolutionNote))
                    report.ResolutionNote = resolutionNote;

                if (status == "Resolved" || status == "Rejected")
                    report.ResolvedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Real-time notification to customer
                await Clients.User(report.CustomerId.ToString())
                    .SendAsync("OnReportStatusUpdated", reportId, status);

                return true;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error updating report status: {ex.Message}");
            }
        }

        // ============================
        // Rate Report (Customer only)
        // ============================
        public async Task<bool> RateReport(Guid reportId, int rating)
        {
            try
            {
                if (rating < 1 || rating > 5)
                    throw new HubException("Rating must be between 1 and 5");

                var report = await _context.CustomerReports.FindAsync(reportId);
                if (report == null || report.Status != "Resolved")
                    return false;

                report.Rating = rating;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error rating report: {ex.Message}");
            }
        }

        // ============================
        // Get Customer Statistics
        // ============================
        public async Task<object> GetMyReportStatistics(Guid customerId)
        {
            try
            {
                var reports = await _context.CustomerReports
                    .Where(r => r.CustomerId == customerId)
                    .ToListAsync();

                var resolvedReports = reports
                    .Where(r => r.Status == "Resolved" && r.ResolvedDate.HasValue)
                    .ToList();

                return new
                {
                    TotalReports = reports.Count,
                    PendingReports = reports.Count(r => r.Status == "Pending"),
                    InProgressReports = reports.Count(r => r.Status == "InProgress"),
                    ResolvedReports = reports.Count(r => r.Status == "Resolved"),
                    RejectedReports = reports.Count(r => r.Status == "Rejected"),
                    AverageResolutionTime = resolvedReports.Any()
                        ? resolvedReports.Average(r => (r.ResolvedDate.Value - r.CreatedDate).TotalHours)
                        : 0,
                    AverageRating = reports.Where(r => r.Rating.HasValue).Any()
                        ? reports.Where(r => r.Rating.HasValue).Average(r => r.Rating.Value)
                        : 0
                };
            }
            catch (Exception ex)
            {
                throw new HubException($"Error getting statistics: {ex.Message}");
            }
        }
    }
}