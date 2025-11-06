using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Server.Models;

namespace ParkingSystem.Server.Hubs
{
    public partial class ParkingHub
    {
        // ============================
        // Get All Reports (Admin only)
        // ============================
        public async Task<List<CustomerReport>> GetAllReports()
        {
            try
            {
                return await _context.CustomerReports
                    .Include(r => r.Customer)
                    .Include(r => r.Category)
                    .Include(r => r.AssignedStaff)
                    .Include(r => r.ReportComments)
                    .OrderByDescending(r => r.CreatedDate)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new HubException($"Error getting all reports: {ex.Message}");
            }
        }

        // ============================
        // Get All Staff (For assignment dropdown)
        // ============================
        public async Task<List<Staff>> GetAllStaff()
        {
            try
            {
                return await _context.Staff
                    .OrderBy(s => s.FullName)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new HubException($"Error getting staff list: {ex.Message}");
            }
        }

        // ============================
        // Assign Report to Staff (Admin only)
        // ============================
        public async Task<bool> AssignReportToStaff(Guid reportId, Guid staffId)
        {
            try
            {
                var report = await _context.CustomerReports.FindAsync(reportId);
                if (report == null)
                    return false;

                var staff = await _context.Staff.FindAsync(staffId);
                if (staff == null)
                    throw new HubException("Staff not found");

                // Update report
                report.AssignedStaffId = staffId;
                report.Status = "InProgress"; // Auto change status
                report.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Get full report info
                var updatedReport = await _context.CustomerReports
                    .Include(r => r.Customer)
                    .Include(r => r.Category)
                    .Include(r => r.AssignedStaff)
                    .AsNoTracking()
                    .FirstAsync(r => r.ReportId == reportId);

                // ⭐ Broadcast to ALL
                await Clients.All.SendAsync("OnReportAssigned", updatedReport);

                // ⭐ Notify specific staff
                await Clients.User(staffId.ToString())
                    .SendAsync("OnReportAssignedToYou", updatedReport);

                return true;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error assigning report: {ex.Message}");
            }
        }

        // ============================
        // Change Report Priority (Admin only)
        // ============================
        public async Task<bool> ChangePriority(Guid reportId, string newPriority)
        {
            try
            {
                var report = await _context.CustomerReports.FindAsync(reportId);
                if (report == null)
                    return false;

                report.Priority = newPriority;
                report.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Broadcast
                await Clients.All.SendAsync("OnReportPriorityChanged", reportId, newPriority);

                return true;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error changing priority: {ex.Message}");
            }
        }

        // ============================
        // Reassign Report (Admin only)
        // ============================
        public async Task<bool> ReassignReport(Guid reportId, Guid newStaffId)
        {
            try
            {
                var report = await _context.CustomerReports.FindAsync(reportId);
                if (report == null)
                    return false;

                var oldStaffId = report.AssignedStaffId;
                report.AssignedStaffId = newStaffId;
                report.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Get full report info
                var updatedReport = await _context.CustomerReports
                    .Include(r => r.Customer)
                    .Include(r => r.Category)
                    .Include(r => r.AssignedStaff)
                    .AsNoTracking()
                    .FirstAsync(r => r.ReportId == reportId);

                // Notify old staff
                if (oldStaffId.HasValue)
                {
                    await Clients.User(oldStaffId.ToString())
                        .SendAsync("OnReportUnassigned", reportId);
                }

                // Notify new staff
                await Clients.User(newStaffId.ToString())
                    .SendAsync("OnReportAssignedToYou", updatedReport);

                // Broadcast to ALL
                await Clients.All.SendAsync("OnReportReassigned", updatedReport);

                return true;
            }
            catch (Exception ex)
            {
                throw new HubException($"Error reassigning report: {ex.Message}");
            }
        }

        // ============================
        // Get Admin Statistics
        // ============================
        public async Task<object> GetAdminStatistics()
        {
            try
            {
                var reports = await _context.CustomerReports
                    .AsNoTracking()
                    .ToListAsync();

                var resolvedReports = reports
                    .Where(r => r.Status == "Resolved" && r.ResolvedDate.HasValue)
                    .ToList();

                var staffPerformance = await _context.Staff
                    .Select(s => new
                    {
                        StaffId = s.StaffId,
                        StaffName = s.FullName,
                        TotalAssigned = _context.CustomerReports.Count(r => r.AssignedStaffId == s.StaffId),
                        Resolved = _context.CustomerReports.Count(r => r.AssignedStaffId == s.StaffId && r.Status == "Resolved"),
                        AverageRating = _context.CustomerReports
                            .Where(r => r.AssignedStaffId == s.StaffId && r.Rating.HasValue)
                            .Average(r => (double?)r.Rating) ?? 0
                    })
                    .AsNoTracking()
                    .ToListAsync();

                return new
                {
                    TotalReports = reports.Count,
                    UnassignedReports = reports.Count(r => r.AssignedStaffId == null),
                    PendingReports = reports.Count(r => r.Status == "Pending"),
                    InProgressReports = reports.Count(r => r.Status == "InProgress"),
                    ResolvedReports = reports.Count(r => r.Status == "Resolved"),
                    RejectedReports = reports.Count(r => r.Status == "Rejected"),
                    UrgentReports = reports.Count(r => r.Priority == "Urgent"),
                    HighReports = reports.Count(r => r.Priority == "High"),
                    AverageResolutionTime = resolvedReports.Any()
                        ? resolvedReports.Average(r => (r.ResolvedDate.Value - r.CreatedDate).TotalHours)
                        : 0,
                    AverageRating = reports.Where(r => r.Rating.HasValue).Any()
                        ? reports.Where(r => r.Rating.HasValue).Average(r => r.Rating.Value)
                        : 0,
                    StaffPerformance = staffPerformance
                };
            }
            catch (Exception ex)
            {
                throw new HubException($"Error getting admin statistics: {ex.Message}");
            }
        }
    }
}