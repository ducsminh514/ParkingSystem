using Microsoft.AspNetCore.SignalR.Client;
using ParkingSystem.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace ParkingSystem.Client.Services
{
    public interface IReportService
    {
        // Customer methods
        Task<List<ReportCategory>> GetCategoriesAsync();
        Task<CustomerReport> CreateReportAsync(CustomerReport report);
        Task<List<CustomerReport>> GetMyReportsAsync(Guid customerId);
        Task<CustomerReport> GetReportDetailAsync(Guid reportId);
        Task<ReportComment> AddCommentAsync(ReportComment comment);
        Task<bool> RateReportAsync(Guid reportId, int rating);
        Task<object> GetStatisticsAsync(Guid customerId);
        
        // Admin methods
        Task<List<CustomerReport>> GetAllReportsAsync();
        Task<List<Staff>> GetAllStaffAsync();
        Task<bool> AssignReportToStaffAsync(Guid reportId, Guid staffId);
        Task<bool> ChangePriorityAsync(Guid reportId, string newPriority);
        Task<bool> ReassignReportAsync(Guid reportId, Guid newStaffId);
        Task<object> GetAdminStatisticsAsync();
        
        // Staff methods
        Task<List<CustomerReport>> GetMyAssignedReportsAsync(Guid staffId);
        Task<bool> UpdateReportStatusByStaffAsync(Guid reportId, string status, string resolutionNote = null);
        Task<ReportComment> AddInternalNoteAsync(ReportComment comment);
        Task<object> GetStaffStatisticsAsync(Guid staffId);
        
        // Real-time events
        event Action<Guid, string>? OnReportStatusUpdated;
        event Action<ReportComment>? OnNewCommentAdded;
        event Action<CustomerReport>? OnNewReportCreated;
        event Action<Guid, int>? OnReportRated;
        event Action<CustomerReport>? OnReportAssigned;
        event Action<Guid, string>? OnReportPriorityChanged;
    }

    public class ReportService : IReportService
    {
        private readonly ISignalRConnectionService _connectionService;
        private HubConnection Connection => _connectionService.Connection;

        public event Action<Guid, string>? OnReportStatusUpdated;
        public event Action<ReportComment>? OnNewCommentAdded;
        public event Action<CustomerReport>? OnNewReportCreated;
        public event Action<Guid, int>? OnReportRated;
        public event Action<CustomerReport>? OnReportAssigned;
        public event Action<Guid, string>? OnReportPriorityChanged;

        public ReportService(ISignalRConnectionService connectionService)
        {
            _connectionService = connectionService;
            
            // Register real-time event listeners ONE TIME
            RegisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            // Listen to report status updates
            Connection.On<Guid, string>("OnReportStatusUpdated", (reportId, status) =>
            {
                Console.WriteLine($"[Real-time] Report {reportId} status: {status}");
                OnReportStatusUpdated?.Invoke(reportId, status);
            });

            // Listen to new comments
            Connection.On<ReportComment>("OnNewCommentAdded", (comment) =>
            {
                Console.WriteLine($"[Real-time] New comment on report {comment.ReportId}");
                OnNewCommentAdded?.Invoke(comment);
            });

            // Listen to new reports (for staff)
            Connection.On<CustomerReport>("OnNewReportCreated", (report) =>
            {
                Console.WriteLine($"[Real-time] New report created: {report.Title}");
                OnNewReportCreated?.Invoke(report);
            });

            // Listen to report rating updates
            Connection.On<Guid, int>("OnReportRated", (reportId, rating) =>
            {
                Console.WriteLine($"[Real-time] Report {reportId} rated: {rating} stars");
                OnReportRated?.Invoke(reportId, rating);
            });

            // Listen to report assignment
            Connection.On<CustomerReport>("OnReportAssigned", (report) =>
            {
                Console.WriteLine($"[Real-time] Report {report.ReportId} assigned to staff {report.AssignedStaffId}");
                OnReportAssigned?.Invoke(report);
            });

            // Listen to priority changes
            Connection.On<Guid, string>("OnReportPriorityChanged", (reportId, newPriority) =>
            {
                Console.WriteLine($"[Real-time] Report {reportId} priority changed to {newPriority}");
                OnReportPriorityChanged?.Invoke(reportId, newPriority);
            });
        }

        public async Task<List<ReportCategory>> GetCategoriesAsync()
        {
            return await Connection.InvokeAsync<List<ReportCategory>>("GetReportCategories");
        }

        public async Task<CustomerReport> CreateReportAsync(CustomerReport report)
        {
            return await Connection.InvokeAsync<CustomerReport>("CreateReport", report);
        }

        public async Task<List<CustomerReport>> GetMyReportsAsync(Guid customerId)
        {
            return await Connection.InvokeAsync<List<CustomerReport>>("GetMyReports", customerId);
        }

        public async Task<CustomerReport> GetReportDetailAsync(Guid reportId)
        {
            return await Connection.InvokeAsync<CustomerReport>("GetReportDetail", reportId);
        }

        public async Task<ReportComment> AddCommentAsync(ReportComment comment)
        {
            var result = await Connection.InvokeAsync<ReportComment>("AddReportComment", comment);
            if (result != null)
            {
                OnNewCommentAdded?.Invoke(result);
            }
            return result;
        }

        public async Task<bool> RateReportAsync(Guid reportId, int rating)
        {
            var result = await Connection.InvokeAsync<bool>("RateReport", reportId, rating);
            if (result)
            {
                OnReportRated?.Invoke(reportId, rating);
            }
            return result;
        }

        public async Task<object> GetStatisticsAsync(Guid customerId)
        {
            return await Connection.InvokeAsync<object>("GetMyReportStatistics", customerId);
        }

        // Admin methods
        public async Task<List<CustomerReport>> GetAllReportsAsync()
        {
            return await Connection.InvokeAsync<List<CustomerReport>>("GetAllReports");
        }

        public async Task<List<Staff>> GetAllStaffAsync()
        {
            return await Connection.InvokeAsync<List<Staff>>("GetAllStaff");
        }

        public async Task<bool> AssignReportToStaffAsync(Guid reportId, Guid staffId)
        {
            var result = await Connection.InvokeAsync<bool>("AssignReportToStaff", reportId, staffId);
            if (result)
            {
                var report = await GetReportDetailAsync(reportId);
                OnReportAssigned?.Invoke(report);
            }
            return result;
        }

        public async Task<bool> ChangePriorityAsync(Guid reportId, string newPriority)
        {
            var result = await Connection.InvokeAsync<bool>("ChangeReportPriority", reportId, newPriority);
            if (result)
            {
                OnReportPriorityChanged?.Invoke(reportId, newPriority);
            }
            return result;
        }

        public async Task<bool> ReassignReportAsync(Guid reportId, Guid newStaffId)
        {
            return await Connection.InvokeAsync<bool>("ReassignReport", reportId, newStaffId);
        }

        public async Task<object> GetAdminStatisticsAsync()
        {
            return await Connection.InvokeAsync<object>("GetAdminReportStatistics");
        }

        // Staff methods
        public async Task<List<CustomerReport>> GetMyAssignedReportsAsync(Guid staffId)
        {
            return await Connection.InvokeAsync<List<CustomerReport>>("GetMyAssignedReports", staffId);
        }

        public async Task<bool> UpdateReportStatusByStaffAsync(Guid reportId, string status, string resolutionNote = null)
        {
            var result = await Connection.InvokeAsync<bool>("UpdateReportStatus", reportId, status, resolutionNote);
            if (result)
            {
                OnReportStatusUpdated?.Invoke(reportId, status);
            }
            return result;
        }

        public async Task<ReportComment> AddInternalNoteAsync(ReportComment comment)
        {
            var result = await Connection.InvokeAsync<ReportComment>("AddInternalNote", comment);
            if (result != null)
            {
                // Only notify about internal notes to staff/admin, not customers
                OnNewCommentAdded?.Invoke(result);
            }
            return result;
        }

        public async Task<object> GetStaffStatisticsAsync(Guid staffId)
        {
            return await Connection.InvokeAsync<object>("GetStaffReportStatistics", staffId);
        }
    }
}