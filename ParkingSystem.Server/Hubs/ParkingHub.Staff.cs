using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;

namespace ParkingSystem.Server.Hubs
{
    public partial class ParkingHub
    {
        // ============================
        // Get My Assigned Reports (Staff)
        // ============================
        public async Task<List<CustomerReport>> GetMyAssignedReports(Guid staffId)
        {
            try
            {
                return await _context.CustomerReports
                    .Include(r => r.Customer)
                    .Include(r => r.Category)
                    .Include(r => r.AssignedStaff)
                    .Include(r => r.ReportComments)
                    .Where(r => r.AssignedStaffId == staffId)
                    .OrderByDescending(r => r.CreatedDate)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new HubException($"Error getting assigned reports: {ex.Message}");
            }
        }

        // ============================
        // Update Report Status (Staff)
        // ============================
        public async Task<bool> UpdateReportStatusByStaff(Guid reportId, string status, string resolutionNote = null)
        {
            try
            {
                var report = await _context.CustomerReports.FindAsync(reportId);
                if (report == null)
                    return false;

                // Validate status change
                if (status == "Resolved" && string.IsNullOrEmpty(resolutionNote))
                {
                    throw new HubException("Resolution note is required when resolving a report");
                }

                report.Status = status;
                report.UpdatedDate = DateTime.Now;

                if (!string.IsNullOrEmpty(resolutionNote))
                {
                    report.ResolutionNote = resolutionNote;
                }

                if (status == "Resolved" || status == "Rejected")
                {
                    report.ResolvedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // Broadcast to ALL
                await Clients.All.SendAsync("OnReportStatusUpdated", reportId, status);

                return true;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error updating report status: {ex.Message}");
            }
        }

        // ============================
        // Add Internal Note (Staff/Admin only)
        // ============================
        public async Task<ReportComment> AddInternalNote(ReportComment comment)
        {
            try
            {
                comment.CommentId = Guid.NewGuid();
                comment.CreatedDate = DateTime.Now;
                comment.IsInternal = true; // Force internal
                comment.Report = null;

                _context.ReportComments.Add(comment);

                var report = await _context.CustomerReports.FindAsync(comment.ReportId);
                if (report != null)
                {
                    report.UpdatedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // Only broadcast to Staff/Admin (not to customer)
                await Clients.Group("Staff").SendAsync("OnInternalNoteAdded", comment);
                await Clients.Group("Admin").SendAsync("OnInternalNoteAdded", comment);

                return comment;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error adding internal note: {ex.Message}");
            }
        }

        // ============================
        // Get Staff Statistics
        // ============================
        public async Task<object> GetStaffStatistics(Guid staffId)
        {
            try
            {
                var reports = await _context.CustomerReports
                    .Where(r => r.AssignedStaffId == staffId)
                    .AsNoTracking()
                    .ToListAsync();

                var resolvedReports = reports
                    .Where(r => r.Status == "Resolved" && r.ResolvedDate.HasValue)
                    .ToList();

                return new
                {
                    TotalAssigned = reports.Count,
                    PendingReports = reports.Count(r => r.Status == "Pending"),
                    InProgressReports = reports.Count(r => r.Status == "InProgress"),
                    ResolvedReports = reports.Count(r => r.Status == "Resolved"),
                    RejectedReports = reports.Count(r => r.Status == "Rejected"),
                    AverageResolutionTime = resolvedReports.Any()
                        ? resolvedReports.Average(r => (r.ResolvedDate.Value - r.CreatedDate).TotalHours)
                        : 0,
                    AverageRating = reports.Where(r => r.Rating.HasValue).Any()
                        ? reports.Where(r => r.Rating.HasValue).Average(r => r.Rating.Value)
                        : 0,
                    RatingCount = reports.Count(r => r.Rating.HasValue)
                };
            }
            catch (Exception ex)
            {
                throw new HubException($"Error getting staff statistics: {ex.Message}");
            }
        }
    }
}